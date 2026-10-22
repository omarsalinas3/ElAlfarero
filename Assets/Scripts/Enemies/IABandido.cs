using System.Collections;
using UnityEngine;

/// <summary>
/// IA de los bandidos del Charro Negro. Esperan quietos (apuntando) hasta ver a Mateo y entonces:
/// - Revólver / Rifle / Escopeta: se colocan a su distancia de tiro, apuntan un instante (aviso) y disparan.
///   La escopeta suelta varios perdigones en abanico; el rifle tira lejos y fuerte pero más lento.
/// - Machete: se le echa encima, alza el machete y lanza el machetazo enfrente.
/// Al recibir un golpe se encogen un momento; al morir se reproduce su animación y desaparecen.
/// Con "siempreAlerta" (emboscada) ya saben dónde está Mateo y entran corriendo desde fuera de la pantalla.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class IABandido : MonoBehaviour
{
    public enum Arma { Revolver, Rifle, Escopeta, Machete }
    public enum Estado { Esperar, Acercarse, Atacar, Aturdido, Muerto }

    static readonly int AnimVelocidad = Animator.StringToHash("Velocidad");
    static readonly int AnimAtacar = Animator.StringToHash("Atacar");
    static readonly int AnimGolpe = Animator.StringToHash("Golpe");
    static readonly int AnimMuerto = Animator.StringToHash("Muerto");

    [Header("Arma")]
    [SerializeField] Arma arma = Arma.Revolver;
    [SerializeField] int dano = 10;
    [SerializeField] float distanciaPreferida = 6f;   // A qué distancia de Mateo le gusta pararse (machete: cuerpo a cuerpo)
    [SerializeField] float preparacion = 0.4f;         // Tiempo apuntando / alzando el machete antes de atacar
    [SerializeField] float enfriamiento = 1.5f;
    [SerializeField] int balasPorDisparo = 1;
    [SerializeField] float dispersionGrados = 0f;
    [SerializeField] float velocidadBala = 11f;
    [SerializeField] BalaEnemiga prefabBala;
    [SerializeField] Vector2 puntoDisparo = new Vector2(0.95f, 1.45f);   // Local, mirando a la derecha
    [SerializeField] Vector2 alcanceMachete = new Vector2(1.6f, 1.8f);

    [Header("Movimiento y sentidos")]
    [SerializeField] float velocidad = 3.2f;
    [SerializeField] float rangoVision = 11f;
    [SerializeField] float alturaVision = 3f;
    [SerializeField] float tiempoOlvido = 2.5f;
    [SerializeField] bool siempreAlerta;               // Emboscada: persigue desde el principio
    [SerializeField] LayerMask capaSuelo;

    [Header("Golpe y muerte")]
    [SerializeField] float retroceso = 2.5f;
    [SerializeField] float duracionAturdido = 0.25f;
    [SerializeField] float tiempoCadaver = 2.5f;

    [Header("Referencias")]
    [SerializeField] Animator animator;
    [SerializeField] Transform objetivo;

    public Estado EstadoActual { get; private set; } = Estado.Esperar;
    public Arma TipoArma => arma;
    public int Direccion { get; private set; } = 1;
    public bool SiempreAlerta { get => siempreAlerta; set => siempreAlerta = value; }

    Rigidbody2D rb;
    Collider2D col;
    Health salud;
    Health saludObjetivo;
    float ultimaVezVisto = -999f;
    float siguienteAtaque;
    float finAturdido;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        salud = GetComponent<Health>();
        rb.freezeRotation = true;
        if (animator == null) animator = GetComponentInChildren<Animator>();
        Direccion = transform.localScale.x < 0f ? -1 : 1;
        siguienteAtaque = Time.time + Random.Range(0.3f, 0.9f);   // Que no disparen todos al mismo tiempo
    }

    void Start()
    {
        if (objetivo == null)
        {
            var jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null) objetivo = jugador.transform;
        }
        if (objetivo != null) saludObjetivo = objetivo.GetComponent<Health>();
        if (siempreAlerta) ultimaVezVisto = Time.time;
    }

    void OnEnable() { salud.OnDano += AlRecibirDano; salud.OnMuerte += AlMorir; }
    void OnDisable() { salud.OnDano -= AlRecibirDano; salud.OnMuerte -= AlMorir; }

    void FixedUpdate()
    {
        if (EstadoActual == Estado.Muerto) return;
        if (!ObjetivoDisponible()) { if (EstadoActual != Estado.Atacar) Mover(0f); return; }

        if (siempreAlerta || PuedeVerAlObjetivo()) ultimaVezVisto = Time.time;
        bool alerta = Time.time - ultimaVezVisto < tiempoOlvido;

        switch (EstadoActual)
        {
            case Estado.Esperar:
                Mover(0f);
                if (alerta) EstadoActual = Estado.Acercarse;
                break;
            case Estado.Acercarse:
                if (!alerta) { EstadoActual = Estado.Esperar; break; }
                Acercarse();
                break;
            case Estado.Aturdido:
                if (Time.time >= finAturdido) EstadoActual = Estado.Acercarse;
                break;
        }
    }

    void Update()
    {
        if (animator != null) animator.SetFloat(AnimVelocidad, EstadoActual == Estado.Muerto ? 0f : Mathf.Abs(rb.linearVelocity.x));
    }

    // ------------------------------------------------------------------ Comportamiento
    void Acercarse()
    {
        Vector2 delta = (Vector2)objetivo.position - (Vector2)transform.position;
        int lado = delta.x >= 0f ? 1 : -1;
        Girar(lado);

        float dx = Mathf.Abs(delta.x);
        bool cuerpoACuerpo = arma == Arma.Machete;
        float margen = cuerpoACuerpo ? 0.3f : 1.5f;

        bool enRango = dx <= distanciaPreferida + margen && Mathf.Abs(delta.y) < (cuerpoACuerpo ? 1.5f : alturaVision)
                       && (cuerpoACuerpo || LineaLibre());
        if (enRango && Time.time >= siguienteAtaque)
        {
            StartCoroutine(Atacar());
            return;
        }

        if (dx > distanciaPreferida + 0.4f) Mover(PuedeAvanzar(lado) ? lado * velocidad : 0f);
        else if (!cuerpoACuerpo && dx < distanciaPreferida - 2f) Mover(PuedeAvanzar(-lado) ? -lado * velocidad * 0.6f : 0f);   // Retrocede para no quedar encima
        else Mover(0f);
    }

    IEnumerator Atacar()
    {
        EstadoActual = Estado.Atacar;
        Mover(0f);

        // Aviso: se queda apuntando (o con el machete en alto) un instante para que Mateo alcance a reaccionar
        yield return new WaitForSeconds(preparacion);
        if (EstadoActual != Estado.Atacar) yield break;   // Lo golpearon o murió mientras apuntaba

        if (objetivo != null) Girar(objetivo.position.x >= transform.position.x ? 1 : -1);
        if (animator != null) animator.SetTrigger(AnimAtacar);

        if (arma == Arma.Machete)
        {
            GestorAudio.Reproducir(Sfx.Machetazo);
            yield return new WaitForSeconds(0.1f);
            Machetazo();
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            Disparar();
            yield return new WaitForSeconds(0.25f);
        }

        siguienteAtaque = Time.time + enfriamiento * Random.Range(0.85f, 1.25f);
        if (EstadoActual == Estado.Atacar) EstadoActual = Estado.Acercarse;
    }

    void Disparar()
    {
        GestorAudio.Reproducir(arma == Arma.Escopeta ? Sfx.Escopetazo : Sfx.Disparo);
        if (prefabBala == null) return;

        Vector2 origen = transform.TransformPoint(new Vector2(puntoDisparo.x, puntoDisparo.y));
        Vector2 destino = objetivo.position + Vector3.up * 1.1f;
        Vector2 dir = destino - origen;
        // Dispara hacia donde mira, inclinándose como mucho 20° para alcanzar a Mateo si salta
        float angulo = Mathf.Clamp(Mathf.Atan2(dir.y, Mathf.Abs(dir.x)) * Mathf.Rad2Deg, -20f, 20f);

        for (int i = 0; i < balasPorDisparo; i++)
        {
            float abanico = balasPorDisparo > 1 ? Mathf.Lerp(-dispersionGrados, dispersionGrados, i / (balasPorDisparo - 1f)) : 0f;
            float a = (angulo + abanico) * Mathf.Deg2Rad;
            var d = new Vector2(Mathf.Cos(a) * Direccion, Mathf.Sin(a));
            var bala = Instantiate(prefabBala, origen, Quaternion.identity);
            bala.Disparar(d, velocidadBala, dano);
        }
    }

    void Machetazo()
    {
        Vector2 centro = (Vector2)transform.position + new Vector2(Direccion * alcanceMachete.x * 0.5f, 1.1f);
        foreach (var c in Physics2D.OverlapBoxAll(centro, alcanceMachete, 0f))
        {
            if (c.GetComponentInParent<PlayerController>() == null) continue;
            var s = c.GetComponentInParent<Health>();
            if (s == null || s.EsInvulnerable || s.EstaMuerto) continue;
            s.RecibirDano(dano);
            var reaccion = s.GetComponent<ReaccionDanoJugador>();
            if (reaccion != null) reaccion.Empujar(transform.position, 7f);
            break;
        }
    }

    void AlRecibirDano(int cantidad)
    {
        if (salud.EstaMuerto) return;
        ultimaVezVisto = Time.time;                         // Si le disparan por la espalda, ya sabe dónde está Mateo
        if (animator != null) animator.SetTrigger(AnimGolpe);
        int lado = objetivo != null && objetivo.position.x > transform.position.x ? -1 : 1;
        rb.linearVelocity = new Vector2(lado * retroceso, rb.linearVelocity.y);
        EstadoActual = Estado.Aturdido;                     // Cancela el disparo si estaba apuntando
        finAturdido = Time.time + duracionAturdido;
    }

    void AlMorir()
    {
        EstadoActual = Estado.Muerto;
        StopAllCoroutines();
        if (animator != null) animator.SetBool(AnimMuerto, true);
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;            // Que el cuerpo no estorbe ni lo empujen
        col.enabled = false;
        StartCoroutine(Desvanecer());
    }

    IEnumerator Desvanecer()
    {
        yield return new WaitForSeconds(tiempoCadaver);
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            foreach (var s in sprites) { var c = s.color; c.a = 1f - t / 0.6f; s.color = c; }
            yield return null;
        }
        Destroy(gameObject);
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
        Vector2 delta = (Vector2)objetivo.position - (Vector2)transform.position;
        if (Mathf.Abs(delta.y) > alturaVision || Mathf.Abs(delta.x) > rangoVision) return false;
        bool enfrente = Mathf.Sign(delta.x) == Direccion;
        if (!enfrente && Mathf.Abs(delta.x) > 2.5f) return false;
        return LineaLibre();
    }

    bool LineaLibre()
    {
        Vector2 ojos = (Vector2)transform.position + Vector2.up * 1.5f;
        return !Physics2D.Linecast(ojos, objetivo.position + Vector3.up * 1f, capaSuelo);
    }

    bool PuedeAvanzar(int lado)
    {
        Bounds b = col.bounds;
        if (!Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.05f), Vector2.down, 0.3f, capaSuelo)) return true;
        float alcance = b.extents.x + 0.15f;
        if (Physics2D.Raycast(new Vector2(b.center.x, b.min.y + 0.3f), Vector2.right * lado, alcance, capaSuelo)) return false;
        if (Physics2D.Raycast(b.center, Vector2.right * lado, alcance, capaSuelo)) return false;
        return Physics2D.Raycast(new Vector2(b.center.x + lado * alcance, b.min.y + 0.1f), Vector2.down, 0.6f, capaSuelo);
    }

    // ------------------------------------------------------------------ Cuerpo
    void Mover(float vx) => rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);

    void Girar(int lado)
    {
        if (lado == 0 || lado == Direccion) return;
        Direccion = lado;
        Vector3 e = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(e.x) * lado, e.y, e.z);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.TransformPoint(puntoDisparo), 0.1f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position + Vector3.up, transform.position + Vector3.up + Vector3.right * Direccion * distanciaPreferida);
    }
}
