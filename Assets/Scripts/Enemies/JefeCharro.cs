using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Jefe final (#90): El Charro Negro, en dos fases.
/// Fase 1, a caballo: embestidas de lado a lado de la arena (aviso: el caballo se encabrita; se esquivan saltando o
/// evadiendo) y ráfagas de fuego infernal. Al vencer al caballo, este cae y se hace humo; EncuentroCharro pone el pacto
/// y el Charro sigue a pie.
/// Fase 2, a pie, con los ataques más difíciles del juego: espadazo si Mateo se le acerca, abanico de ráfagas y
/// llamaradas que brotan bajo los pies de Mateo (cada una avisa con una grieta encendida). A media vida se enfurece:
/// más rápido, abanico de cinco, más llamaradas y el espadazo deja fuego enfrente.
/// Cada fase tiene su propia vida, en su cara ("Caballo" y "Pie"), para que la barra del jefe se llene de nuevo.
/// Si Mateo cae en la fase 2, el reintento empieza ahí (punto de control estático, sobrevive a la recarga).
/// Cuerpo cinemático con collider trigger en cada cara, como el Jinete. El arte mira a la DERECHA.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class JefeCharro : MonoBehaviour
{
    public enum Estado { Dormido, Acechar, Embestida, Rafaga, Caida, Pacto, Espadazo, Abanico, Llamaradas, Furia, Muerto }

    static readonly int AnimVelocidad = Animator.StringToHash("Velocidad");
    static readonly int AnimCaido = Animator.StringToHash("Caido");
    static readonly int AnimMuerto = Animator.StringToHash("Muerto");

    [Header("Caras: cada fase con su vida")]
    [SerializeField] Health saludCaballo;
    [SerializeField] Health saludPie;
    [SerializeField] Animator animCaballo;
    [SerializeField] Animator animPie;

    [Header("Fase 1: a caballo")]
    [SerializeField] float velocidadCaballo = 3.5f;
    [SerializeField] float pausaCaballo = 1.1f;
    [SerializeField] float avisoEmbestida = 0.75f;
    [SerializeField] float velocidadEmbestida = 16f;

    [Header("Fase 2: a pie")]
    [SerializeField] float velocidadPie = 2.8f;
    [SerializeField] float pausaPie = 0.9f;
    [SerializeField] float multiplicadorFurioso = 1.4f;
    [SerializeField] float distanciaEspadazo = 3.2f;
    [SerializeField] float avisoEspadazo = 0.35f;
    [SerializeField] int danoEspadazo = 25;
    [SerializeField] Vector2 cajaEspadazo = new Vector2(3.4f, 3.2f);
    [SerializeField] Llamarada prefabLlamarada;
    [SerializeField] float retrasoLlamaradas = 0.45f;

    [Header("Fuego infernal")]
    [SerializeField] BalaEnemiga prefabFuego;
    [SerializeField] float velocidadFuego = 10f;
    [SerializeField] int danoFuego = 18;
    [SerializeField] Vector2 manoCaballo = new Vector2(1.6f, 3.4f);    // De dónde sale el fuego, desde los pies
    [SerializeField] Vector2 manoPie = new Vector2(1.4f, 2.4f);

    [Header("Referencias")]
    [SerializeField] Transform objetivo;
    [SerializeField] LayerMask capaJugador;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] float esperaVictoria = 3.5f;

    public Estado EstadoActual { get; private set; } = Estado.Dormido;
    public int Fase { get; private set; } = 1;
    public bool Furioso { get; private set; }
    public Health SaludCaballo => saludCaballo;
    public Health SaludPie => saludPie;
    public int Direccion { get; private set; } = -1;

    /// <summary>Lo que lleva lanzado en total: sirve a las pruebas para medir cada ataque.</summary>
    public int Embestidas { get; private set; }
    public int FuegosLanzados { get; private set; }
    public int Espadazos { get; private set; }
    public int LlamaradasLanzadas { get; private set; }

    /// <summary>El caballo cayó: quien escuche pone el pacto y luego llama a EmpezarFase2. Sin nadie, sigue solo.</summary>
    public event Action OnCaballoVencido;

    // Punto de control: si Mateo cae peleando contra el Charro a pie, el reintento de esta escena empieza en la fase 2.
    // Es estático para sobrevivir a la recarga; se usa una sola vez y se borra al vencer al Charro.
    static string escenaPuntoDeControl;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos() => escenaPuntoDeControl = null;

    public static bool HayPuntoDeControl => escenaPuntoDeControl == SceneManager.GetActiveScene().name;

    /// <summary>True si esta escena se recargó tras caer en la fase 2; lo consume.</summary>
    public static bool TomarPuntoDeControl()
    {
        bool hay = HayPuntoDeControl;
        escenaPuntoDeControl = null;
        return hay;
    }

    Rigidbody2D rb;
    Health saludJugador;
    DanoPorContacto danoCaballo, danoPie;
    SpriteRenderer[] spritesPie;
    float minX = float.NegativeInfinity, maxX = float.PositiveInfinity;
    float vx;
    Coroutine rutina;

    Animator Anim => Fase == 1 ? animCaballo : animPie;
    float Ritmo => Furioso ? multiplicadorFurioso : 1f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;               // Si no, las cerámicas (también cinemáticas) no lo tocan
        rb.freezeRotation = true;
        danoCaballo = saludCaballo.GetComponent<DanoPorContacto>();
        danoPie = saludPie.GetComponent<DanoPorContacto>();
        spritesPie = saludPie.GetComponentsInChildren<SpriteRenderer>(true);
        Dormir();
    }

    void Start()
    {
        if (objetivo == null)
        {
            var jugador = FindFirstObjectByType<PlayerController>();
            if (jugador != null) objetivo = jugador.transform;
        }
        saludJugador = objetivo != null ? objetivo.GetComponent<Health>() : null;
        if (saludJugador != null) saludJugador.OnMuerte += AlMorirJugador;
    }

    void OnDestroy() { if (saludJugador != null) saludJugador.OnMuerte -= AlMorirJugador; }

    void OnEnable()
    {
        saludCaballo.OnMuerte += AlCaerCaballo;
        saludPie.OnMuerte += AlMorir;
        saludPie.OnVidaCambiada += AlCambiarVidaPie;
    }

    void OnDisable()
    {
        saludCaballo.OnMuerte -= AlCaerCaballo;
        saludPie.OnMuerte -= AlMorir;
        saludPie.OnVidaCambiada -= AlCambiarVidaPie;
    }

    void Update()
    {
        if (EstadoActual == Estado.Dormido) return;      // Dormido lo anima la cinemática a mano
        bool anda = EstadoActual == Estado.Acechar || EstadoActual == Estado.Embestida;
        if (Anim != null && Anim.isActiveAndEnabled) Anim.SetFloat(AnimVelocidad, anda ? Mathf.Abs(vx) : 0f);
    }

    void FixedUpdate()
    {
        if (EstadoActual == Estado.Dormido || EstadoActual == Estado.Muerto || vx == 0f) return;
        float x = Mathf.Clamp(rb.position.x + vx * Time.fixedDeltaTime, minX, maxX);   // No sale de la arena
        rb.MovePosition(new Vector2(x, rb.position.y));
    }

    // ------------------------------------------------------------------ Control desde la cinemática
    /// <summary>Quieto, sin daño: así lo mueve la cinemática a mano.</summary>
    public void Dormir()
    {
        EstadoActual = Estado.Dormido;
        vx = 0f;
        saludCaballo.Invulnerable = saludPie.Invulnerable = true;
        Contacto(false);
    }

    public void Pose(string estado)
    {
        if (Anim != null && Anim.isActiveAndEnabled) Anim.Play(estado, 0, 0f);
    }

    /// <summary>Empieza la fase 1 (a caballo) dentro de la arena [min, max].</summary>
    public void Activar(float min, float max)
    {
        minX = min; maxX = max;
        MostrarCara(1);
        saludCaballo.Invulnerable = false;
        Contacto(true);
        rutina = StartCoroutine(PelearACaballo());
    }

    /// <summary>Directo a pie, sin caballo: así empieza el reintento desde el punto de control.</summary>
    public void PrepararFase2(float min, float max)
    {
        minX = min; maxX = max;
        saludCaballo.gameObject.SetActive(false);
        MostrarCara(2);
        EstadoActual = Estado.Pacto;
    }

    /// <summary>Termina el pacto (o el reintento): el Charro pelea a pie.</summary>
    public void EmpezarFase2()
    {
        if (Fase != 2 || EstadoActual == Estado.Muerto) return;
        saludPie.Invulnerable = false;
        Contacto(true);
        rutina = StartCoroutine(PelearAPie());
    }

    // ------------------------------------------------------------------ Fase 1: a caballo
    IEnumerator PelearACaballo()
    {
        int ataque = 0;
        while (Fase == 1)
        {
            if (!PuedePelear()) { vx = 0f; yield return null; continue; }

            // Ronda a Mateo sin pegársele
            EstadoActual = Estado.Acechar;
            Pose("Galope");
            float hasta = Time.time + pausaCaballo;
            while (Time.time < hasta && EstadoActual == Estado.Acechar)
            {
                MirarAlObjetivo();
                float dx = objetivo.position.x - transform.position.x;
                vx = Mathf.Abs(dx) > 5f ? Mathf.Sign(dx) * velocidadCaballo : -Mathf.Sign(dx) * velocidadCaballo * 0.5f;
                yield return new WaitForFixedUpdate();
            }
            vx = 0f;
            if (Fase != 1) yield break;

            if (ataque++ % 2 == 0) yield return Embestida();
            else yield return Rafaga(ataque % 4 == 0 ? 2 : 1);   // Cada tanto, dos ráfagas seguidas
        }
    }

    IEnumerator Embestida()
    {
        EstadoActual = Estado.Embestida;
        MirarAlObjetivo();
        vx = 0f;
        Pose("Encabritar");                               // El caballo se encabrita: aviso
        GestorAudio.Reproducir(Sfx.Estampida, 0f);
        yield return new WaitForSeconds(avisoEmbestida);
        if (EstadoActual != Estado.Embestida) yield break;

        Embestidas++;
        Pose("Galope");
        float destino = Direccion > 0 ? maxX : minX;
        float limite = Time.time + 3f;
        while (EstadoActual == Estado.Embestida && Mathf.Abs(destino - rb.position.x) > 0.6f && Time.time < limite)
        {
            vx = Direccion * velocidadEmbestida;
            yield return new WaitForFixedUpdate();
        }
        vx = 0f;
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.3f, 0.22f);
        yield return new WaitForSeconds(0.35f);
    }

    IEnumerator Rafaga(int veces)
    {
        EstadoActual = Estado.Rafaga;
        for (int i = 0; i < veces && EstadoActual == Estado.Rafaga; i++)
        {
            MirarAlObjetivo();
            vx = 0f;
            Pose("Lanzar");
            yield return new WaitForSeconds(0.45f);           // Se le enciende la mano: aviso
            if (EstadoActual != Estado.Rafaga) yield break;
            LanzarFuego(manoCaballo, 1, 0f);
            yield return new WaitForSeconds(0.5f);
        }
    }

    void AlCaerCaballo()
    {
        if (Fase != 1) return;
        if (rutina != null) StopCoroutine(rutina);
        StartCoroutine(Caer());
    }

    IEnumerator Caer()
    {
        EstadoActual = Estado.Caida;
        vx = 0f;
        Contacto(false);
        animCaballo.SetBool(AnimCaido, true);
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Efectos.GolpeFuerte(0.12f, 0.3f, 0.6f);
        GestorNivel.Actual?.MostrarLetrero("", "¡EL CABALLO CAE!", "");
        yield return new WaitForSeconds(1.3f);            // Tropieza, cae y se hace humo

        // De entre el humo se levanta el Charro a pie
        saludCaballo.gameObject.SetActive(false);
        MostrarCara(2);
        MirarAlObjetivo();
        EstadoActual = Estado.Pacto;
        if (OnCaballoVencido != null) OnCaballoVencido.Invoke();
        else EmpezarFase2();
    }

    // ------------------------------------------------------------------ Fase 2: a pie
    IEnumerator PelearAPie()
    {
        int ataque = 0;
        while (EstadoActual != Estado.Muerto)
        {
            if (!PuedePelear()) { vx = 0f; yield return null; continue; }

            // Camina hacia Mateo: a pie no le huye
            EstadoActual = Estado.Acechar;
            Pose("Caminar");
            float hasta = Time.time + pausaPie / Ritmo;
            while (Time.time < hasta && EstadoActual == Estado.Acechar)
            {
                MirarAlObjetivo();
                float dx = objetivo.position.x - transform.position.x;
                vx = Mathf.Abs(dx) > distanciaEspadazo * 0.8f ? Mathf.Sign(dx) * velocidadPie * Ritmo : 0f;
                yield return new WaitForFixedUpdate();
            }
            vx = 0f;
            if (EstadoActual != Estado.Acechar) yield break;

            if (Mathf.Abs(objetivo.position.x - transform.position.x) < distanciaEspadazo) yield return Espadazo();
            else if (ataque++ % 2 == 0) yield return Abanico();
            else yield return Llamaradas();
        }
    }

    IEnumerator Espadazo()
    {
        EstadoActual = Estado.Espadazo;
        MirarAlObjetivo();
        vx = 0f;
        Pose("Espadazo");                                 // Levanta la espada: aviso
        yield return new WaitForSeconds(avisoEspadazo / Ritmo);
        if (EstadoActual != Estado.Espadazo) yield break;

        Espadazos++;
        GestorAudio.Reproducir(Sfx.Machetazo);
        Vector2 centro = (Vector2)transform.position + new Vector2(Direccion * cajaEspadazo.x / 2f, cajaEspadazo.y / 2f);
        var golpe = Physics2D.OverlapBox(centro, cajaEspadazo, 0f, capaJugador);
        var s = golpe != null ? golpe.GetComponentInParent<Health>() : null;
        if (s != null && !s.EsInvulnerable)
        {
            s.RecibirDano(danoEspadazo);
            golpe.GetComponentInParent<ReaccionDanoJugador>()?.Empujar(transform.position, 10f);
        }
        if (Furioso) SacarLlamarada(transform.position.x + Direccion * 3.5f);   // Furioso: la estela deja fuego enfrente
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Abanico()
    {
        EstadoActual = Estado.Abanico;
        MirarAlObjetivo();
        vx = 0f;
        Pose("Lanzar");
        yield return new WaitForSeconds(0.4f / Ritmo);
        if (EstadoActual != Estado.Abanico) yield break;
        LanzarFuego(manoPie, Furioso ? 5 : 3, 13f);
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator Llamaradas()
    {
        EstadoActual = Estado.Llamaradas;
        MirarAlObjetivo();
        vx = 0f;
        Pose("Clavar");                                   // Clava la espada en el suelo
        yield return new WaitForSeconds(0.45f / Ritmo);
        if (EstadoActual != Estado.Llamaradas) yield break;

        // Cada llamarada brota donde está Mateo en ese momento: hay que seguir moviéndose
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(0.4f, 0.15f);
        int cuantas = Furioso ? 5 : 3;
        for (int i = 0; i < cuantas && EstadoActual == Estado.Llamaradas; i++)
        {
            SacarLlamarada(objetivo.position.x);
            yield return new WaitForSeconds(retrasoLlamaradas / Ritmo);
        }
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Enfurecerse()
    {
        EstadoActual = Estado.Furia;
        vx = 0f;
        saludPie.Invulnerable = true;
        Pose("Furia");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Camera.main?.GetComponent<CameraFollow>()?.Sacudir(1.2f, 0.3f);
        GestorNivel.Actual?.MostrarLetrero("", "¡EL CHARRO NEGRO SE ENFURECE!", "");
        for (float t = 0f; t < 1.3f; t += Time.deltaTime)
        {
            Teñir(Color.Lerp(Color.white, new Color(1f, 0.35f, 0.2f), Mathf.PingPong(t * 6f, 1f)));
            yield return null;
        }
        Teñir(new Color(1f, 0.75f, 0.7f));               // Queda encendido
        Furioso = true;
        saludPie.Invulnerable = false;
        rutina = StartCoroutine(PelearAPie());
    }

    void AlCambiarVidaPie(int actual, int maxima)
    {
        if (Furioso || Fase != 2 || EstadoActual == Estado.Muerto || EstadoActual == Estado.Furia || EstadoActual == Estado.Pacto) return;
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
        Contacto(false);
        escenaPuntoDeControl = null;
        Teñir(Color.white);
        animPie.SetBool(AnimMuerto, true);
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        Efectos.GolpeFuerte(0.2f, 0.4f, 1f);              // El último golpe del juego se congela un instante
        GestorNivel.Actual?.MostrarLetrero("", "¡HECHIZO ROTO!", "EL CHARRO NEGRO CAE");
        StartCoroutine(Victoria());
    }

    IEnumerator Victoria()
    {
        yield return new WaitForSeconds(esperaVictoria);
        GestorNivel.Actual?.LiberarJugador();
        GestorNivel.Actual?.Completar();                  // El final (#91) va después de esta victoria
    }

    void AlMorirJugador()
    {
        if (Fase == 2 && EstadoActual != Estado.Muerto) escenaPuntoDeControl = SceneManager.GetActiveScene().name;
    }

    // ------------------------------------------------------------------ Utilidades
    void LanzarFuego(Vector2 mano, int cuantas, float apertura)
    {
        if (prefabFuego == null || objetivo == null) return;
        Vector2 origen = (Vector2)transform.position + new Vector2(Direccion * mano.x, mano.y);
        Vector2 hacia = (Vector2)objetivo.position + Vector2.up * 1.2f - origen;
        for (int i = 0; i < cuantas; i++)
        {
            float angulo = (i - (cuantas - 1) / 2f) * apertura;
            Vector2 dir = Quaternion.Euler(0f, 0f, angulo) * hacia;
            Reciclaje.Sacar(prefabFuego, origen, Quaternion.identity).Disparar(dir, velocidadFuego, danoFuego);
            FuegosLanzados++;
        }
        GestorAudio.Reproducir(Sfx.Lanzar);
    }

    void SacarLlamarada(float x)
    {
        if (prefabLlamarada == null) return;
        x = Mathf.Clamp(x, minX, maxX);
        Reciclaje.Sacar(prefabLlamarada, new Vector3(x, ySuelo, 0f), Quaternion.identity);
        LlamaradasLanzadas++;
    }

    void MostrarCara(int fase)
    {
        Fase = fase;
        saludCaballo.gameObject.SetActive(fase == 1);
        saludPie.gameObject.SetActive(fase == 2);
    }

    void Contacto(bool activo)
    {
        if (danoCaballo != null) danoCaballo.enabled = activo && Fase == 1;
        if (danoPie != null) danoPie.enabled = activo && Fase == 2;
    }

    void Teñir(Color c)
    {
        foreach (var s in spritesPie) s.color = c;
    }

    bool PuedePelear()
    {
        if (objetivo == null) return false;
        if (saludJugador != null && saludJugador.EstaMuerto) return false;
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
        transform.localScale = new Vector3(Mathf.Abs(e.x) * lado, e.y, e.z);   // El arte mira a la derecha
    }
}
