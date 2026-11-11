using System.Collections;
using UnityEngine;

/// <summary>
/// Jefa del Nivel 3: La Llorona, hechizada por el Charro Negro, en la orilla del río. Flota sin tocar el suelo y guarda
/// su distancia. Ataques: onda sónica que va directo a Mateo (se esquiva saltando o evadiendo; la Maceta Escudo la
/// detiene) y chorros de agua que brotan bajo sus pies con un aviso de burbujas. Si Mateo se le pega, se desvanece y
/// reaparece del otro lado. A la mitad de la vida se enfurece: más rápida, grita dos veces seguidas (tres ondas en
/// abanico cada vez), sus chorros persiguen a Mateo con una segunda tanda y tras cada ataque de agua se esfuma y
/// reaparece en el otro extremo de la orilla. Se endureció tras la retroalimentación del Avance 3, que pedía una
/// pelea más difícil: con la Talavera (unos 80 de daño por segundo) caía en segundos.
/// Empieza dormida (la despierta EncuentroLlorona) y al morir la Talavera rompe el hechizo y se completa el nivel.
/// El arte mira a la IZQUIERDA.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class JefeLlorona : MonoBehaviour
{
    public enum Estado { Dormida, Acechar, Grito, Agua, Desvanecida, Furia, Muerta }

    static readonly int AnimMuerto = Animator.StringToHash("Muerto");

    [Header("Acecho")]
    [SerializeField] float velocidad = 2.6f;
    [SerializeField] float distanciaIdeal = 7f;
    [SerializeField] float multiplicadorFuriosa = 1.5f;
    [SerializeField] float pausaEntreAtaques = 1f;
    [SerializeField] float amplitudFlote = 0.25f;

    [Header("Onda sónica")]
    [SerializeField] BalaEnemiga prefabOnda;
    [SerializeField] float avisoGrito = 0.55f;
    [SerializeField] float velocidadOnda = 11f;
    [SerializeField] int danoOnda = 18;
    [SerializeField] Vector2 boca = new Vector2(1.2f, 3.4f);     // De dónde sale la onda, desde su posición
    [SerializeField] float retrasoSegundoGrito = 0.7f;           // Más que el enfriamiento de la evasión (0.6 s): se puede evadir las dos

    [Header("Chorros de agua")]
    [SerializeField] ChorroAgua prefabChorro;
    [SerializeField] float separacionChorros = 3f;
    [SerializeField] float retrasoEntreChorros = 0.15f;
    [SerializeField] float retrasoPersecucion = 0.6f;            // Furiosa: segunda tanda donde Mateo fue a parar
    [SerializeField] float ySuelo = -4.5f;

    [Header("Desvanecerse")]
    [SerializeField] float distanciaDesvanecer = 3f;
    [SerializeField] float duracionFundido = 0.35f;

    [Header("Referencias")]
    [SerializeField] Animator animator;
    [SerializeField] DanoPorContacto danoContacto;
    [SerializeField] Transform objetivo;
    [SerializeField] float esperaVictoria = 3f;

    public Estado EstadoActual { get; private set; } = Estado.Dormida;
    public bool Furiosa { get; private set; }
    public Health Salud => salud;
    public int Direccion { get; private set; } = -1;
    /// <summary>Cuántas ondas y chorros ha lanzado en total: sirven a las pruebas para medir cada ataque.</summary>
    public int OndasLanzadas { get; private set; }
    public int ChorrosLanzados { get; private set; }

    Rigidbody2D rb;
    Health salud;
    Collider2D cuerpo;
    SpriteRenderer[] sprites;
    float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;
    float vx, yBase;
    Coroutine rutina;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Health>();
        cuerpo = GetComponent<Collider2D>();
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (danoContacto == null) danoContacto = GetComponent<DanoPorContacto>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;               // Si no, las cerámicas (también cinemáticas) no la tocan
        rb.freezeRotation = true;
        yBase = transform.position.y;
        Dormir();
    }

    void Start()
    {
        if (objetivo != null) return;
        var jugador = FindFirstObjectByType<PlayerController>();
        if (jugador != null) objetivo = jugador.transform;
    }

    void OnEnable() { salud.OnMuerte += AlMorir; salud.OnVidaCambiada += AlCambiarVida; }
    void OnDisable() { salud.OnMuerte -= AlMorir; salud.OnVidaCambiada -= AlCambiarVida; }

    void FixedUpdate()
    {
        if (EstadoActual == Estado.Dormida || EstadoActual == Estado.Muerta) return;
        float x = Mathf.Clamp(rb.position.x + vx * Time.fixedDeltaTime, minX, maxX);    // No sale de la arena
        rb.MovePosition(new Vector2(x, yBase + Mathf.Sin(Time.time * 2.2f) * amplitudFlote));
    }

    /// <summary>Quieta y sin daño: así la mueve la cinemática a mano.</summary>
    public void Dormir()
    {
        EstadoActual = Estado.Dormida;
        vx = 0f;
        salud.Invulnerable = true;
        if (danoContacto != null) danoContacto.enabled = false;
    }

    /// <summary>Empieza la pelea dentro de la arena [min, max].</summary>
    public void Activar(float min, float max)
    {
        minX = min; maxX = max;
        yBase = transform.position.y;
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
        while (EstadoActual != Estado.Muerta)
        {
            if (!PuedePelear()) { vx = 0f; yield return null; continue; }

            EstadoActual = Estado.Acechar;
            Pose("Flotar");
            float hasta = Time.time + pausaEntreAtaques / Ritmo;
            while (Time.time < hasta && EstadoActual == Estado.Acechar)
            {
                MirarAlObjetivo();
                float dx = objetivo.position.x - transform.position.x;
                vx = Mathf.Abs(Mathf.Abs(dx) - distanciaIdeal) < 0.5f ? 0f
                   : Mathf.Sign(dx) * Mathf.Sign(Mathf.Abs(dx) - distanciaIdeal) * velocidad * Ritmo;
                if (Mathf.Abs(dx) < distanciaDesvanecer) break;
                yield return new WaitForFixedUpdate();
            }
            vx = 0f;
            if (EstadoActual != Estado.Acechar) yield break;

            // Pegado a ella no hay cómo castigarlo con los ataques a distancia: se desvanece y reaparece lejos
            if (Mathf.Abs(objetivo.position.x - transform.position.x) < distanciaDesvanecer)
                yield return Desvanecerse();
            else if (ataque++ % 2 == 0) yield return Grito();
            else
            {
                yield return Agua();
                // Furiosa no se deja tirar desde un solo lugar: se esfuma y reaparece en el otro extremo
                if (Furiosa && EstadoActual == Estado.Agua) yield return Desvanecerse();
            }
        }
    }

    IEnumerator Grito()
    {
        EstadoActual = Estado.Grito;
        MirarAlObjetivo();
        Pose("Grito");
        GestorAudio.Reproducir(Sfx.GritoLlorona, 0f);
        yield return new WaitForSeconds(avisoGrito / Ritmo);
        if (EstadoActual != Estado.Grito || prefabOnda == null) yield break;

        LanzarOndas(Furiosa ? 3 : 1, objetivo.position.y);
        if (Furiosa)
        {
            // Segundo grito: apunta a donde Mateo quedó tras esquivar el primero (a la altura del suelo, para que
            // saltarlo siga siendo posible)
            yield return new WaitForSeconds(retrasoSegundoGrito);
            if (EstadoActual != Estado.Grito) yield break;
            MirarAlObjetivo();
            Pose("Grito");
            GestorAudio.Reproducir(Sfx.GritoLlorona, 0f);
            LanzarOndas(3, ySuelo);
        }
        yield return new WaitForSeconds(0.55f);
    }

    /// <summary>Ondas en abanico hacia la x de Mateo, a la altura de su pecho si estuviera parado en <paramref name="yPies"/>.</summary>
    void LanzarOndas(int cuantas, float yPies)
    {
        Vector2 origen = (Vector2)transform.position + new Vector2(Direccion * boca.x, boca.y);
        Vector2 hacia = new Vector2(objetivo.position.x, yPies + 1.2f) - origen;
        for (int i = 0; i < cuantas; i++)
        {
            float angulo = (i - (cuantas - 1) / 2f) * 16f;
            Vector2 dir = Quaternion.Euler(0f, 0f, angulo) * hacia;
            Reciclaje.Sacar(prefabOnda, origen, Quaternion.identity).Disparar(dir, velocidadOnda, danoOnda);
            OndasLanzadas++;
        }
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.2f, 0.12f);
    }

    IEnumerator Agua()
    {
        EstadoActual = Estado.Agua;
        MirarAlObjetivo();
        Pose("Agua");
        yield return new WaitForSeconds(0.45f / Ritmo);
        if (EstadoActual != Estado.Agua || prefabChorro == null) yield break;

        yield return Tanda(Furiosa ? 2 : 1);
        if (Furiosa)
        {
            // La segunda tanda brota donde Mateo fue a parar al esquivar la primera
            yield return new WaitForSeconds(retrasoPersecucion);
            if (EstadoActual != Estado.Agua) yield break;
            yield return Tanda(1);
        }
        if (EstadoActual != Estado.Agua) yield break;
        yield return new WaitForSeconds(0.8f);
    }

    /// <summary>Uno bajo los pies de Mateo y los demás a sus lados, de adentro hacia afuera.</summary>
    IEnumerator Tanda(int aCadaLado)
    {
        float centro = objetivo.position.x;
        Sacar(centro);
        for (int i = 1; i <= aCadaLado; i++)
        {
            yield return new WaitForSeconds(retrasoEntreChorros);
            if (EstadoActual != Estado.Agua) yield break;
            Sacar(centro - i * separacionChorros);
            Sacar(centro + i * separacionChorros);
        }
    }

    void Sacar(float x)
    {
        if (x < minX || x > maxX) return;                  // Fuera de la orilla no hay agua
        Reciclaje.Sacar(prefabChorro, new Vector3(x, ySuelo, 0f), Quaternion.identity);
        ChorrosLanzados++;
    }

    IEnumerator Desvanecerse()
    {
        EstadoActual = Estado.Desvanecida;
        salud.Invulnerable = true;
        if (danoContacto != null) danoContacto.enabled = false;
        yield return Fundir(1f, 0.15f);

        // Reaparece en el extremo de la arena más lejano de Mateo
        float x = objetivo.position.x - minX > maxX - objetivo.position.x ? minX + 1f : maxX - 1f;
        rb.position = new Vector2(x, rb.position.y);
        transform.position = rb.position;
        MirarAlObjetivo();

        yield return Fundir(0.15f, 1f);
        salud.Invulnerable = false;
        if (danoContacto != null) danoContacto.enabled = true;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        for (float t = 0f; t < duracionFundido; t += Time.deltaTime)
        {
            Alfa(Mathf.Lerp(desde, hasta, t / duracionFundido));
            yield return null;
        }
        Alfa(hasta);
    }

    IEnumerator Enfurecerse()
    {
        EstadoActual = Estado.Furia;
        vx = 0f;
        salud.Invulnerable = true;
        Alfa(1f);
        Pose("Golpe");
        GestorAudio.Reproducir(Sfx.GritoLlorona, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(1f, 0.25f);
        GestorNivel.Actual?.MostrarLetrero("", "¡AY, MIS HIJOS!", "LA LLORONA SE ENFURECE");
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            Teñir(Color.Lerp(Color.white, new Color(0.45f, 0.9f, 1f), Mathf.PingPong(t * 6f, 1f)));
            yield return null;
        }
        Teñir(new Color(0.75f, 0.95f, 1f));                // Queda con un aura fría
        Furiosa = true;
        salud.Invulnerable = false;
        if (danoContacto != null) danoContacto.enabled = true;
        rutina = StartCoroutine(Pelear());
    }

    void AlCambiarVida(int actual, int maxima)
    {
        if (Furiosa || EstadoActual == Estado.Muerta || EstadoActual == Estado.Dormida || EstadoActual == Estado.Furia) return;
        if (actual > 0 && actual <= maxima / 2)
        {
            if (rutina != null) StopCoroutine(rutina);
            StartCoroutine(Enfurecerse());
        }
    }

    void AlMorir()
    {
        EstadoActual = Estado.Muerta;
        StopAllCoroutines();
        vx = 0f;
        if (danoContacto != null) danoContacto.enabled = false;
        if (cuerpo != null) cuerpo.enabled = false;
        Teñir(Color.white);
        Alfa(1f);
        if (animator != null) animator.SetBool(AnimMuerto, true);
        GestorAudio.Reproducir(Sfx.GritoLlorona, 0f);
        Efectos.GolpeFuerte(0.15f, 0.3f, 0.8f);            // El golpe final se congela un instante (#30)
        GestorNivel.Actual?.MostrarLetrero("", "¡HECHIZO ROTO!", "LA LLORONA ENCUENTRA PAZ");
        StartCoroutine(Victoria());
    }

    IEnumerator Victoria()
    {
        // Se deshace en el aire mientras el hechizo la suelta
        const float fundido = 1.5f;
        for (float t = 0f; t < fundido; t += Time.deltaTime)
        {
            Alfa(1f - t / fundido);
            yield return null;
        }
        Alfa(0f);
        yield return new WaitForSeconds(Mathf.Max(0f, esperaVictoria - fundido));
        GestorNivel.Actual?.LiberarJugador();
        GestorNivel.Actual?.Completar();
    }

    // ------------------------------------------------------------------ Utilidades
    void Teñir(Color c)
    {
        foreach (var s in sprites) { c.a = s.color.a; s.color = c; }
    }

    public void Alfa(float a)
    {
        foreach (var s in sprites) { Color c = s.color; c.a = a; s.color = c; }
    }

    float Ritmo => Furiosa ? multiplicadorFuriosa : 1f;

    bool PuedePelear()
    {
        if (objetivo == null) return false;
        var s = objetivo.GetComponent<Health>();
        if (s != null && s.EstaMuerto) return false;
        var gestor = GestorNivel.Actual;
        return gestor == null || gestor.EstadoActual == GestorNivel.Estado.Jugando;
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
