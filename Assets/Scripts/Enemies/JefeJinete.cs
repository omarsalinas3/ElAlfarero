using System.Collections;
using UnityEngine;

/// <summary>
/// Jefe del Nivel 2 (#56): el Jinete sin Cabeza, hechizado por el Charro Negro, en el patio de la capilla de Pozos.
/// Ataques: carga a galope de lado a lado de la arena (se esquiva saltando o evadiendo), sablazo si Mateo está cerca
/// y lanza la calavera en llamas de su linterna. A la mitad de la vida se enfurece: más rápido, cargas dobles y
/// tres calaveras en abanico. Empieza dormido (lo despierta EncuentroJinete) y al morir completa el nivel.
/// Cuerpo cinemático con collider trigger (como la bruja): las cargas lo atraviesan todo y Mateo lo esquiva saltándolo
/// o evadiendo a través de él; el collider cubre el caballo y el torso para que el salto alcance a librarlo.
/// El arte mira a la DERECHA.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class JefeJinete : MonoBehaviour
{
    public enum Estado { Dormido, Acechar, Carga, Sablazo, Lanzar, Furia, Muerto }

    static readonly int AnimVelocidad = Animator.StringToHash("Velocidad");
    static readonly int AnimMuerto = Animator.StringToHash("Muerto");

    [Header("Acecho")]
    [SerializeField] float velocidad = 3f;
    [SerializeField] float multiplicadorFurioso = 1.45f;
    [SerializeField] float pausaEntreAtaques = 1.2f;

    [Header("Carga")]
    [SerializeField] float avisoCarga = 0.7f;
    [SerializeField] float velocidadCarga = 15f;

    [Header("Sablazo")]
    [SerializeField] float distanciaSablazo = 3f;
    [SerializeField] float avisoSablazo = 0.4f;
    [SerializeField] int danoSablazo = 20;
    [SerializeField] Vector2 cajaSablazo = new Vector2(2.8f, 2.6f);

    [Header("Calavera")]
    [SerializeField] BalaEnemiga prefabCalavera;
    [SerializeField] float velocidadCalavera = 9f;
    [SerializeField] int danoCalavera = 15;
    [SerializeField] Vector2 linterna = new Vector2(1.1f, 2.4f);    // De dónde sale la calavera, desde los pies

    [Header("Referencias")]
    [SerializeField] Animator animator;
    [SerializeField] DanoPorContacto danoContacto;
    [SerializeField] Transform objetivo;
    [SerializeField] LayerMask capaJugador;
    [SerializeField] float esperaVictoria = 3f;

    public Estado EstadoActual { get; private set; } = Estado.Dormido;
    public bool Furioso { get; private set; }
    public Health Salud => salud;
    public int Direccion { get; private set; } = -1;

    Rigidbody2D rb;
    Health salud;
    SpriteRenderer[] sprites;
    float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;
    float vx;                                           // Velocidad actual (el cuerpo es cinemático)
    Coroutine rutina;
    Color[] colores;                                    // Color propio de cada sprite (el arte provisional va teñido)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Health>();
        sprites = GetComponentsInChildren<SpriteRenderer>();
        colores = System.Array.ConvertAll(sprites, s => s.color);
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (danoContacto == null) danoContacto = GetComponent<DanoPorContacto>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;               // Si no, las cerámicas (también cinemáticas) no lo tocan
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
        if (animator != null) animator.SetFloat(AnimVelocidad, EstadoActual == Estado.Acechar || EstadoActual == Estado.Carga ? Mathf.Abs(vx) : 0f);
    }

    void FixedUpdate()
    {
        if (EstadoActual == Estado.Dormido || EstadoActual == Estado.Muerto || vx == 0f) return;
        float x = Mathf.Clamp(rb.position.x + vx * Time.fixedDeltaTime, minX, maxX);   // No sale de la arena
        rb.MovePosition(new Vector2(x, rb.position.y));
    }

    /// <summary>Quieto, sin física ni daño: así lo mueve la cinemática a mano.</summary>
    public void Dormir()
    {
        EstadoActual = Estado.Dormido;
        vx = 0f;
        salud.Invulnerable = true;
        if (danoContacto != null) danoContacto.enabled = false;
    }

    /// <summary>Empieza la pelea dentro de la arena [min, max].</summary>
    public void Activar(float min, float max)
    {
        minX = min; maxX = max;
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
        int ataque = 0;
        while (EstadoActual != Estado.Muerto)
        {
            if (!PuedePelear()) { Mover(0f); yield return null; continue; }

            // Ronda a Mateo un rato, sin pegársele
            EstadoActual = Estado.Acechar;
            Pose("Galope");
            float hasta = Time.time + pausaEntreAtaques / Ritmo;
            while (Time.time < hasta && EstadoActual == Estado.Acechar)
            {
                MirarAlObjetivo();
                float dx = objetivo.position.x - transform.position.x;
                Mover(Mathf.Abs(dx) > 4f ? Mathf.Sign(dx) * velocidad * Ritmo : -Mathf.Sign(dx) * velocidad * 0.5f);
                yield return new WaitForFixedUpdate();
            }
            if (EstadoActual == Estado.Muerto) yield break;

            float distancia = Mathf.Abs(objetivo.position.x - transform.position.x);
            if (distancia < distanciaSablazo) yield return Sablazo();
            else if (ataque++ % 2 == 0)
            {
                yield return Carga();
                if (Furioso && EstadoActual != Estado.Muerto) yield return Carga();   // Furioso: de ida y vuelta
            }
            else yield return Lanzar();
        }
    }

    IEnumerator Carga()
    {
        EstadoActual = Estado.Carga;
        MirarAlObjetivo();
        Mover(0f);
        Pose("Golpe");                                   // Se encabrita: aviso
        GestorAudio.Reproducir(Sfx.Estampida, 0f);
        yield return new WaitForSeconds(avisoCarga / Ritmo);
        if (EstadoActual != Estado.Carga) yield break;

        // Galopa hasta el otro extremo de la arena
        Pose("Galope");
        float destino = Direccion > 0 ? maxX : minX;
        float limite = Time.time + 3f;
        while (EstadoActual == Estado.Carga && Mathf.Abs(destino - rb.position.x) > 0.6f && Time.time < limite)
        {
            Mover(Direccion * velocidadCarga * Mathf.Sqrt(Ritmo));
            yield return new WaitForFixedUpdate();
        }
        Mover(0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.25f, 0.2f);
        yield return new WaitForSeconds(0.3f);
    }

    IEnumerator Sablazo()
    {
        EstadoActual = Estado.Sablazo;
        MirarAlObjetivo();
        Mover(0f);
        Pose("Sablazo");
        yield return new WaitForSeconds(avisoSablazo / Ritmo);
        if (EstadoActual != Estado.Sablazo) yield break;

        GestorAudio.Reproducir(Sfx.Machetazo);
        Vector2 centro = (Vector2)transform.position + new Vector2(Direccion * cajaSablazo.x / 2f, cajaSablazo.y / 2f);
        var golpe = Physics2D.OverlapBox(centro, cajaSablazo, 0f, capaJugador);
        var s = golpe != null ? golpe.GetComponentInParent<Health>() : null;
        if (s != null && !s.EsInvulnerable)
        {
            s.RecibirDano(danoSablazo);
            golpe.GetComponentInParent<ReaccionDanoJugador>()?.Empujar(transform.position, 9f);
        }
        yield return new WaitForSeconds(0.45f);
    }

    IEnumerator Lanzar()
    {
        EstadoActual = Estado.Lanzar;
        MirarAlObjetivo();
        Mover(0f);
        Pose("Lanzar");
        yield return new WaitForSeconds(0.35f / Ritmo);
        if (EstadoActual != Estado.Lanzar || prefabCalavera == null) yield break;

        // La calavera de la linterna va directo a Mateo; furioso, tres en abanico
        Vector2 origen = (Vector2)transform.position + new Vector2(Direccion * linterna.x, linterna.y);
        Vector2 hacia = (Vector2)objetivo.position + Vector2.up * 1.2f - origen;
        int cuantas = Furioso ? 3 : 1;
        for (int i = 0; i < cuantas; i++)
        {
            float angulo = (i - (cuantas - 1) / 2f) * 14f;
            Vector2 dir = Quaternion.Euler(0f, 0f, angulo) * hacia;
            Reciclaje.Sacar(prefabCalavera, origen, Quaternion.identity).Disparar(dir, velocidadCalavera, danoCalavera);
        }
        GestorAudio.Reproducir(Sfx.Lanzar);
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator Enfurecerse()
    {
        EstadoActual = Estado.Furia;
        Mover(0f);
        salud.Invulnerable = true;
        Pose("Golpe");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(1f, 0.25f);
        GestorNivel.Actual?.MostrarLetrero("", "¡EL JINETE SE ENFURECE!", "");
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            // Las llamas del cuello se avivan
            Color c = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.15f), Mathf.PingPong(t * 6f, 1f));
            Teñir(c);
            yield return null;
        }
        Teñir(new Color(1f, 0.8f, 0.7f));                 // Queda encendido
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
            StartCoroutine(Enfurecerse());
        }
    }

    void AlMorir()
    {
        EstadoActual = Estado.Muerto;
        StopAllCoroutines();
        vx = 0f;
        if (danoContacto != null) danoContacto.enabled = false;
        Teñir(Color.white);
        if (animator != null) animator.SetBool(AnimMuerto, true);
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.8f, 0.3f);
        StartCoroutine(Victoria());
    }

    IEnumerator Victoria()
    {
        yield return new WaitForSeconds(esperaVictoria);
        GestorNivel.Actual?.LiberarJugador();
        GestorNivel.Actual?.Completar();
    }

    // ------------------------------------------------------------------ Utilidades
    void Teñir(Color c)
    {
        for (int i = 0; i < sprites.Length; i++) sprites[i].color = colores[i] * c;
    }

    float Ritmo => Furioso ? multiplicadorFurioso : 1f;

    bool PuedePelear()
    {
        if (objetivo == null) return false;
        var s = objetivo.GetComponent<Health>();
        if (s != null && s.EstaMuerto) return false;
        var gestor = GestorNivel.Actual;
        return gestor == null || gestor.EstadoActual == GestorNivel.Estado.Jugando;
    }

    void Mover(float velocidadX) => vx = velocidadX;

    void MirarAlObjetivo()
    {
        if (objetivo != null) Mirar(objetivo.position.x >= transform.position.x ? 1 : -1);
    }

    public void Mirar(int lado)
    {
        Direccion = lado;
        Vector3 e = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(e.x) * lado, e.y, e.z);   // El arte mira a la derecha
    }
}
