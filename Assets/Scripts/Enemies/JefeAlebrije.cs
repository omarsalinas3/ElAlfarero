using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe del Nivel 1: el Alebrije Gigante que el Charro Negro despierta en la parroquia de Dolores.
/// Ataques físicos (GDD): acecha, lanza zarpazos con embestida y salta para caer encima de Mateo con una onda de choque.
/// Transformación a la mitad de la vida: ruge (invulnerable un momento), se vuelve más rápido y encadena saltos.
/// Empieza dormido (Activar lo despierta al terminar la cinemática). Al vencerlo se rompe el hechizo del Charro: se calma
/// y se vuelve la montura de Mateo (#50). Sin montura asignada se deshace y avisa a GestorNivel (victoria directa).
/// El arte mira a la IZQUIERDA, por eso la escala X es negativa cuando mira a la derecha.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class JefeAlebrije : MonoBehaviour
{
    public enum Estado { Dormido, Acechar, Zarpazo, Salto, Transformacion, Muerto }

    static readonly int AnimVelocidad = Animator.StringToHash("Velocidad");
    static readonly int AnimMuerto = Animator.StringToHash("Muerto");

    [Header("Acecho")]
    [SerializeField] float velocidad = 2.4f;
    [SerializeField] float multiplicadorFurioso = 1.6f;
    [SerializeField] float pausaEntreAtaques = 1.1f;

    [Header("Zarpazo")]
    [SerializeField] float distanciaZarpazo = 3.5f;
    [SerializeField] float avisoZarpazo = 0.5f;
    [SerializeField] float velocidadEmbestida = 9f;
    [SerializeField] float duracionEmbestida = 0.35f;

    [Header("Salto")]
    [SerializeField] float avisoSalto = 0.45f;
    [SerializeField] float duracionSalto = 0.9f;
    [SerializeField] float alturaSalto = 5f;
    [SerializeField] int danoOnda = 15;
    [SerializeField] float radioOnda = 3.2f;

    [Header("Referencias")]
    [SerializeField] Animator animator;
    [SerializeField] DanoPorContacto danoContacto;
    [SerializeField] LayerMask capaSuelo;
    [SerializeField] Transform objetivo;
    [SerializeField] float esperaVictoria = 4f;          // La animación de deshacerse dura 2.4 s y luego se desvanece
    [SerializeField] MonturaAlebrije montura;              // En lo que se convierte al romperse el hechizo

    public Estado EstadoActual { get; private set; } = Estado.Dormido;
    public bool Furioso { get; private set; }
    public Health Salud => salud;
    public int Direccion { get; private set; } = -1;

    Rigidbody2D rb;
    Health salud;
    SpriteRenderer[] sprites;
    float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;
    float ySuelo;
    Coroutine rutina;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Health>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (danoContacto == null) danoContacto = GetComponent<DanoPorContacto>();
        rb.freezeRotation = true;
        Dormir();
    }

    void Start()
    {
        if (objetivo == null)
        {
            var jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null) objetivo = jugador.transform;
        }
    }

    void OnEnable() { salud.OnMuerte += AlMorir; salud.OnVidaCambiada += AlCambiarVida; }
    void OnDisable() { salud.OnMuerte -= AlMorir; salud.OnVidaCambiada -= AlCambiarVida; }

    void Update()
    {
        if (animator != null) animator.SetFloat(AnimVelocidad, EstadoActual == Estado.Acechar ? Mathf.Abs(rb.linearVelocity.x) : 0f);
    }

    /// <summary>Quieto, sin física ni daño: así lo mueve la cinemática a mano.</summary>
    public void Dormir()
    {
        EstadoActual = Estado.Dormido;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        salud.Invulnerable = true;
        if (danoContacto != null) danoContacto.enabled = false;
    }

    /// <summary>Empieza la pelea dentro de la arena [min, max].</summary>
    public void Activar(float min, float max)
    {
        minX = min; maxX = max;
        ySuelo = rb.position.y;
        rb.bodyType = RigidbodyType2D.Dynamic;
        salud.Invulnerable = false;
        if (danoContacto != null) danoContacto.enabled = true;
        EstadoActual = Estado.Acechar;
        rutina = StartCoroutine(Pelear());
    }

    public void Pose(string estado)
    {
        if (animator != null) animator.Play(estado, 0, 0f);
    }

    // ------------------------------------------------------------------ Pelea
    IEnumerator Pelear()
    {
        while (EstadoActual != Estado.Muerto)
        {
            if (!PuedePelear()) { Mover(0f); yield return null; continue; }

            // Acecha un rato acercándose a Mateo
            EstadoActual = Estado.Acechar;
            Pose("Caminar");
            float hasta = Time.time + pausaEntreAtaques / Ritmo;
            while (Time.time < hasta && EstadoActual == Estado.Acechar)
            {
                MirarAlObjetivo();
                float dx = objetivo.position.x - transform.position.x;
                Mover(Mathf.Abs(dx) > 1.5f ? Mathf.Sign(dx) * velocidad * Ritmo : 0f);
                yield return new WaitForFixedUpdate();
            }
            if (EstadoActual == Estado.Muerto) yield break;

            float distancia = Mathf.Abs(objetivo.position.x - transform.position.x);
            if (distancia < distanciaZarpazo && Random.value < 0.75f) yield return Zarpazo();
            else
            {
                yield return Salto();
                if (Furioso && EstadoActual != Estado.Muerto && Random.value < 0.6f) yield return Salto();   // Furioso: doble salto
            }
        }
    }

    IEnumerator Zarpazo()
    {
        EstadoActual = Estado.Zarpazo;
        MirarAlObjetivo();
        Mover(0f);
        Pose("Aterrizaje");                         // Se agazapa: aviso
        yield return new WaitForSeconds(avisoZarpazo / Ritmo);
        if (EstadoActual != Estado.Zarpazo) yield break;

        Pose("Zarpazo");
        GestorAudio.Reproducir(Sfx.Machetazo);
        for (float t = 0f; t < duracionEmbestida && EstadoActual == Estado.Zarpazo; t += Time.fixedDeltaTime)
        {
            Mover(Direccion * velocidadEmbestida);
            yield return new WaitForFixedUpdate();
        }
        Mover(0f);
        yield return new WaitForSeconds(0.25f);
    }

    IEnumerator Salto()
    {
        EstadoActual = Estado.Salto;
        MirarAlObjetivo();
        Mover(0f);
        Pose("Aterrizaje");
        yield return new WaitForSeconds(avisoSalto / Ritmo);
        if (EstadoActual != Estado.Salto) yield break;

        // Arco hacia donde está Mateo (sin salirse de la arena)
        Vector2 inicio = rb.position;
        float destinoX = Mathf.Clamp(objetivo.position.x, minX + 2f, maxX - 2f);
        Pose("Salto");
        rb.bodyType = RigidbodyType2D.Kinematic;
        float dur = duracionSalto / Mathf.Sqrt(Ritmo);
        for (float t = 0f; t < dur && EstadoActual == Estado.Salto; t += Time.fixedDeltaTime)
        {
            float k = t / dur;
            rb.MovePosition(new Vector2(Mathf.Lerp(inicio.x, destinoX, k), inicio.y + 4f * alturaSalto * k * (1f - k)));
            yield return new WaitForFixedUpdate();
        }
        if (EstadoActual == Estado.Muerto) yield break;
        rb.position = new Vector2(destinoX, inicio.y);
        rb.bodyType = RigidbodyType2D.Dynamic;

        // Aterrizaje: temblor y onda de choque a ras de suelo
        Pose("Aterrizaje");
        GestorAudio.Reproducir(Sfx.RomperHorno);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.35f, 0.35f);
        OndaDeChoque();
        yield return new WaitForSeconds(0.45f);
    }

    void OndaDeChoque()
    {
        if (objetivo == null) return;
        var jugador = objetivo.GetComponent<PlayerController>();
        float dx = Mathf.Abs(objetivo.position.x - transform.position.x);
        if (dx > radioOnda || jugador == null || !jugador.EstaEnSuelo) return;   // Brincando se esquiva
        var s = objetivo.GetComponent<Health>();
        if (s == null || s.EsInvulnerable) return;
        s.RecibirDano(danoOnda);
        objetivo.GetComponent<ReaccionDanoJugador>()?.Empujar(transform.position, 8f);
    }

    IEnumerator Transformarse()
    {
        EstadoActual = Estado.Transformacion;
        Mover(0f);
        salud.Invulnerable = true;
        Pose("Rugido");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(1f, 0.25f);
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            // Destellos de colores mientras se transforma
            Color c = Color.Lerp(Color.white, new Color(1f, 0.55f, 0.25f), Mathf.PingPong(t * 6f, 1f));
            foreach (var s in sprites) s.color = c;
            yield return null;
        }
        foreach (var s in sprites) s.color = new Color(1f, 0.85f, 0.75f);   // Queda encendido
        Furioso = true;
        salud.Invulnerable = false;
        rutina = StartCoroutine(Pelear());
    }

    void AlCambiarVida(int actual, int maxima)
    {
        if (Furioso || EstadoActual == Estado.Muerto || EstadoActual == Estado.Dormido) return;
        if (actual > 0 && actual <= maxima / 2)
        {
            if (rutina != null) StopCoroutine(rutina);
            if (rb.bodyType == RigidbodyType2D.Kinematic) rb.bodyType = RigidbodyType2D.Dynamic;   // Por si estaba en pleno salto
            StartCoroutine(Transformarse());
        }
    }

    void AlMorir()
    {
        EstadoActual = Estado.Muerto;
        StopAllCoroutines();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
        if (danoContacto != null) danoContacto.enabled = false;
        foreach (var s in sprites) s.color = Color.white;
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.8f, 0.3f);
        if (montura != null) { StartCoroutine(Liberarse()); return; }
        if (animator != null) animator.SetBool(AnimMuerto, true);
        StartCoroutine(Victoria());
    }

    /// <summary>La Talavera consagrada rompe el hechizo: ruge, destella en dorado, se calma y queda como montura.</summary>
    IEnumerator Liberarse()
    {
        var gestor = GestorNivel.Actual;
        gestor?.IniciarCinematica();
        rb.position = new Vector2(rb.position.x, ySuelo);       // Por si lo vencieron en pleno salto
        Pose("Rugido");
        for (float t = 0f; t < 1.6f; t += Time.deltaTime)
        {
            // El hechizo se le escapa en destellos dorados
            Color c = Color.Lerp(Color.white, new Color(1f, 0.85f, 0.3f), Mathf.PingPong(t * 5f, 1f));
            foreach (var s in sprites) s.color = c;
            yield return null;
        }
        foreach (var s in sprites) s.color = Color.white;
        gestor?.MostrarLetrero("", "¡HECHIZO ROTO!", "EL ALEBRIJE ES LIBRE");
        gestor?.TerminarCinematica();
        gestor?.FijarObjetivo("MONTA AL ALEBRIJE: F");
        montura.Liberar(new Vector3(rb.position.x, ySuelo, 0f), Direccion);
        gameObject.SetActive(false);                          // Lo último: apagarlo detiene esta corrutina
    }

    IEnumerator Victoria()
    {
        // Se deja ver cómo se deshace en confeti y luego los restos se desvanecen despacio
        const float desvanecer = 1.2f;
        yield return new WaitForSeconds(Mathf.Max(0f, esperaVictoria - desvanecer));
        for (float t = 0f; t < desvanecer; t += Time.deltaTime)
        {
            foreach (var s in sprites) { var c = s.color; c.a = 1f - t / desvanecer; s.color = c; }
            yield return null;
        }
        GestorNivel.Actual?.LiberarJugador();
        GestorNivel.Actual?.Completar();
    }

    // ------------------------------------------------------------------ Utilidades
    float Ritmo => Furioso ? multiplicadorFurioso : 1f;

    bool PuedePelear()
    {
        if (objetivo == null) return false;
        var s = objetivo.GetComponent<Health>();
        if (s != null && s.EstaMuerto) return false;
        var gestor = GestorNivel.Actual;
        return gestor == null || gestor.EstadoActual == GestorNivel.Estado.Jugando;
    }

    void Mover(float vx)
    {
        if (rb.bodyType != RigidbodyType2D.Dynamic) return;
        float x = rb.position.x;
        if (x <= minX && vx < 0f || x >= maxX && vx > 0f) vx = 0f;   // No sale de la arena
        rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
    }

    void MirarAlObjetivo()
    {
        if (objetivo != null) Mirar(objetivo.position.x >= transform.position.x ? 1 : -1);
    }

    public void Mirar(int lado)
    {
        Direccion = lado;
        Vector3 e = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(e.x) * -lado, e.y, e.z);   // El arte mira a la izquierda
    }
}
