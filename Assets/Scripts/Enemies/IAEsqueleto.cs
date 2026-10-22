using UnityEngine;

/// <summary>
/// IA básica del minero esqueleto: patrulla alrededor de donde aparece, persigue a Mateo cuando lo ve,
/// se le lanza encima a corta distancia (el daño lo hace DanoPorContacto) y regresa a su zona si lo pierde.
/// No cae de las orillas ni atraviesa paredes, huacales u hornos (todo lo que esté en capaSuelo).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class IAEsqueleto : MonoBehaviour
{
    public enum Estado { Patrullar, Perseguir, Atacar, Regresar, Aturdido }
    enum Paso { Libre, Bajo, Bloqueado }   // Qué hay enfrente: nada, algo que se puede brincar, o pared/orilla

    [Header("Patrulla")]
    [SerializeField] float radioPatrulla = 2.5f;      // Camina de un lado a otro hasta esta distancia de su origen
    [SerializeField] float velocidadPatrulla = 1.2f;
    [SerializeField] float pausaEnGiro = 0.6f;

    [Header("Persecución")]
    [SerializeField] float rangoVision = 6f;          // Hacia donde mira
    [SerializeField] float rangoAlerta = 1.8f;        // A esta distancia lo nota aunque esté de espaldas
    [SerializeField] float alturaVision = 2.5f;       // Diferencia de altura máxima para verlo
    [SerializeField] float velocidadPersecucion = 2.8f;
    [SerializeField] float distanciaMaxima = 9f;      // No se aleja más que esto de su origen
    [SerializeField] float tiempoOlvido = 1.5f;       // Segundos sin verlo antes de regresar
    [SerializeField] float separacionMinima = 0.5f;   // Si queda encimado en Mateo retrocede un paso para volver a atacar
    [SerializeField] float fuerzaBrinco = 9f;         // Para saltar huacales y escombros bajos
    [SerializeField] float alturaBrincable = 1.3f;
    [SerializeField] float tiempoTrabadoMaximo = 1.2f; // Si no puede pasar, se rinde y regresa a su zona

    [Header("Ataque")]
    [SerializeField] float distanciaAtaque = 1.4f;
    [SerializeField] float preparacionAtaque = 0.35f; // Se echa para atrás: aviso para que el jugador lo esquive
    [SerializeField] float velocidadEmbestida = 6f;
    [SerializeField] float duracionEmbestida = 0.2f;
    [SerializeField] float enfriamientoAtaque = 0.9f;

    [Header("Golpe recibido")]
    [SerializeField] float retroceso = 3.5f;
    [SerializeField] float duracionAturdido = 0.25f;

    [Header("Referencias")]
    [SerializeField] LayerMask capaSuelo;
    [SerializeField] Transform visual;                // Hijo con el sprite: se balancea al caminar sin mover el collider
    [SerializeField] Transform objetivo;              // Si está vacío, busca a Mateo al iniciar

    public Estado EstadoActual { get; private set; } = Estado.Patrullar;
    public int Direccion { get; private set; } = 1;   // 1 = derecha, -1 = izquierda
    public Vector2 Origen { get; private set; }

    Rigidbody2D rb;
    Collider2D col;
    Health salud;
    Health saludObjetivo;

    float temporizadorEstado;
    float ultimaVezVisto = -999f;
    float siguienteAtaque;
    bool embistiendo;
    float tiempoTrabado;
    float faseCaminar;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        salud = GetComponent<Health>();
        rb.freezeRotation = true;
        Direccion = transform.localScale.x < 0f ? -1 : 1;
    }

    void Start()
    {
        Origen = transform.position;
        if (objetivo == null)
        {
            var jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null) objetivo = jugador.transform;
        }
        if (objetivo != null) saludObjetivo = objetivo.GetComponent<Health>();
    }

    void OnEnable() { salud.OnDano += AlRecibirDano; }
    void OnDisable() { salud.OnDano -= AlRecibirDano; }

    void FixedUpdate()
    {
        if (salud.EstaMuerto) return;
        temporizadorEstado -= Time.fixedDeltaTime;

        bool loVe = PuedeVerAlObjetivo();
        if (loVe) ultimaVezVisto = Time.time;

        switch (EstadoActual)
        {
            case Estado.Patrullar: Patrullar(loVe); break;
            case Estado.Perseguir: Perseguir(loVe); break;
            case Estado.Atacar: Atacar(); break;
            case Estado.Regresar: Regresar(loVe); break;
            case Estado.Aturdido:
                if (temporizadorEstado <= 0f) CambiarEstado(Estado.Perseguir);   // Tras un golpe sabe dónde está Mateo
                break;
        }
    }

    // ------------------------------------------------------------------ Estados
    void Patrullar(bool loVe)
    {
        if (loVe) { CambiarEstado(Estado.Perseguir); return; }

        if (temporizadorEstado > 0f) { Mover(0f); return; }   // Pausa al dar la vuelta

        float limite = Origen.x + Direccion * radioPatrulla;
        bool llego = Direccion > 0 ? transform.position.x >= limite : transform.position.x <= limite;
        if (llego || QueHayEnfrente(Direccion) != Paso.Libre)
        {
            Girar(-Direccion);
            temporizadorEstado = pausaEnGiro;
            Mover(0f);
            return;
        }
        Mover(Direccion * velocidadPatrulla);
    }

    void Perseguir(bool loVe)
    {
        if (!ObjetivoDisponible() || Time.time - ultimaVezVisto > tiempoOlvido)
        {
            CambiarEstado(Estado.Regresar);
            return;
        }

        Vector2 haciaObjetivo = (Vector2)objetivo.position - (Vector2)transform.position;
        int lado = haciaObjetivo.x >= 0f ? 1 : -1;
        Girar(lado);

        if (Mathf.Abs(haciaObjetivo.x) <= distanciaAtaque && Mathf.Abs(haciaObjetivo.y) < 1.5f && Time.time >= siguienteAtaque)
        {
            CambiarEstado(Estado.Atacar);
            temporizadorEstado = preparacionAtaque;
            embistiendo = false;
            return;
        }

        // Encimado en Mateo (p. ej. mientras él es invulnerable tras un golpe): retrocede para tomar distancia
        if (Mathf.Abs(haciaObjetivo.x) < separacionMinima)
        {
            Mover(-lado * velocidadPatrulla);
            return;
        }

        // Se queda en la orilla de su zona mirando a Mateo en lugar de seguirlo para siempre
        if (Mathf.Abs(transform.position.x + lado * 0.1f - Origen.x) > distanciaMaxima) { Mover(0f); return; }

        if (!Avanzar(lado, velocidadPersecucion))
        {
            // Pared, horno alto u orilla: no puede llegar. Tras un momento lo da por perdido y regresa
            tiempoTrabado += Time.fixedDeltaTime;
            if (tiempoTrabado > tiempoTrabadoMaximo)
            {
                ultimaVezVisto = -999f;
                CambiarEstado(Estado.Regresar);
            }
        }
    }

    /// <summary>Camina hacia un lado brincando lo bajito. Devuelve false si algo le cierra el paso.</summary>
    bool Avanzar(int lado, float velocidad)
    {
        switch (QueHayEnfrente(lado))
        {
            case Paso.Libre:
                tiempoTrabado = 0f;
                Mover(lado * velocidad);
                return true;
            case Paso.Bajo:
                if (EnSuelo()) rb.linearVelocity = new Vector2(lado * velocidad, fuerzaBrinco);
                return true;
            default:
                Mover(0f);
                return false;
        }
    }

    void Atacar()
    {
        if (!embistiendo)
        {
            Mover(0f);
            if (temporizadorEstado <= 0f)
            {
                embistiendo = true;
                temporizadorEstado = duracionEmbestida;
            }
            return;
        }

        Mover(QueHayEnfrente(Direccion) == Paso.Libre ? Direccion * velocidadEmbestida : 0f);
        if (temporizadorEstado <= 0f)
        {
            embistiendo = false;
            siguienteAtaque = Time.time + enfriamientoAtaque;
            CambiarEstado(Estado.Perseguir);
        }
    }

    void Regresar(bool loVe)
    {
        if (loVe) { CambiarEstado(Estado.Perseguir); return; }

        float dx = Origen.x - transform.position.x;
        if (Mathf.Abs(dx) < 0.2f)
        {
            CambiarEstado(Estado.Patrullar);
            return;
        }
        int lado = dx > 0f ? 1 : -1;
        Girar(lado);
        if (!Avanzar(lado, velocidadPatrulla))
        {
            Origen = transform.position;   // Algo le cerró el paso: patrulla desde aquí
            CambiarEstado(Estado.Patrullar);
        }
    }

    void AlRecibirDano(int cantidad)
    {
        if (salud.EstaMuerto) return;

        int lado = objetivo != null ? (transform.position.x >= objetivo.position.x ? 1 : -1) : -Direccion;
        rb.linearVelocity = new Vector2(lado * retroceso, 2f);
        if (objetivo != null) Girar(-lado);
        ultimaVezVisto = Time.time;
        embistiendo = false;
        CambiarEstado(Estado.Aturdido);
        temporizadorEstado = duracionAturdido;
    }

    void CambiarEstado(Estado nuevo)
    {
        EstadoActual = nuevo;
        temporizadorEstado = 0f;
        tiempoTrabado = 0f;
    }

    // ------------------------------------------------------------------ Sentidos
    bool ObjetivoDisponible()
    {
        if (objetivo == null || (saludObjetivo != null && saludObjetivo.EstaMuerto)) return false;
        var gestor = GestorNivel.Actual;
        return gestor == null || gestor.EstadoActual == GestorNivel.Estado.Jugando;
    }

    bool PuedeVerAlObjetivo()
    {
        if (!ObjetivoDisponible()) return false;

        Vector2 ojos = new Vector2(col.bounds.center.x, col.bounds.center.y + col.bounds.extents.y * 0.5f);
        Vector2 destino = objetivo.position + Vector3.up * 0.8f;
        Vector2 delta = destino - ojos;
        if (Mathf.Abs(delta.y) > alturaVision) return false;

        float distancia = Mathf.Abs(delta.x);
        bool enfrente = Mathf.Sign(delta.x) == Direccion;
        if (distancia > (enfrente ? rangoVision : rangoAlerta)) return false;

        return !Physics2D.Linecast(ojos, destino, capaSuelo);   // Paredes, huacales y hornos tapan la vista
    }

    bool EnSuelo()
    {
        Bounds b = col.bounds;
        return Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.3f, capaSuelo);
    }

    /// <summary>Revisa pared enfrente (a la altura de las rodillas, del pecho y del brinco) y suelo adelante.</summary>
    Paso QueHayEnfrente(int lado)
    {
        if (!EnSuelo()) return Paso.Libre;   // En el aire (aparece, brinca o lo empujaron): que siga su arco

        Bounds b = col.bounds;
        float alcance = b.extents.x + 0.15f;
        Vector2 dir = Vector2.right * lado;
        bool rodillas = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.3f), dir, alcance, capaSuelo);
        bool pecho = Physics2D.Raycast(b.center, dir, alcance, capaSuelo);
        bool arriba = Physics2D.Raycast(new Vector2(b.center.x, b.min.y + alturaBrincable), dir, alcance + 0.6f, capaSuelo);

        if (rodillas || pecho) return arriba || pecho && b.center.y - b.min.y > alturaBrincable ? Paso.Bloqueado : Paso.Bajo;

        Vector2 orilla = new Vector2(b.center.x + lado * alcance, b.min.y + 0.1f);
        return Physics2D.Raycast(orilla, Vector2.down, 0.6f, capaSuelo) ? Paso.Libre : Paso.Bloqueado;
    }

    // ------------------------------------------------------------------ Cuerpo
    void Mover(float velocidadX)
    {
        rb.linearVelocity = new Vector2(velocidadX, rb.linearVelocity.y);
    }

    void Girar(int lado)
    {
        if (lado == 0 || lado == Direccion) return;
        Direccion = lado;
        Vector3 e = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(e.x) * lado, e.y, e.z);
    }

    void Update()
    {
        if (visual == null) return;

        // Balanceo al caminar (el sprite del minero es un solo cuadro) e inclinación hacia atrás al preparar el ataque
        float rapidez = Mathf.Abs(rb.linearVelocity.x);
        float altura = 0f, angulo = 0f;
        if (EstadoActual == Estado.Atacar && !embistiendo)
        {
            angulo = 12f + Mathf.Sin(Time.time * 60f) * 2f;
        }
        else if (rapidez > 0.1f)
        {
            faseCaminar += Time.deltaTime * (6f + rapidez * 2.5f);
            altura = Mathf.Abs(Mathf.Sin(faseCaminar)) * 0.12f;
            angulo = Mathf.Sin(faseCaminar) * 6f;
            if (embistiendo) angulo = -15f;
        }
        else faseCaminar = 0f;

        visual.localPosition = new Vector3(0f, altura, 0f);
        visual.localRotation = Quaternion.Euler(0f, 0f, angulo);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 o = Application.isPlaying ? (Vector3)Origen : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(o + Vector3.left * radioPatrulla, o + Vector3.right * radioPatrulla);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(o + Vector3.up, new Vector3(distanciaMaxima * 2f, 2f, 0f));
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + Vector3.right * Direccion * rangoVision);
    }
}
