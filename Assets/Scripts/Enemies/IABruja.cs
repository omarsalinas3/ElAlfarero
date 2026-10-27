using System.Collections;
using UnityEngine;

/// <summary>
/// Bruja de Fuego (Nivel 2): enemigo que ataca desde el aire. Flota en zigzag sobre su zona; al ver a Mateo se le pone
/// encima a cierta distancia, le suelta bolas de fuego (BalaEnemiga) y cada tanto se lanza en picada hacia él.
/// Con la cerámica se derriba: cae, se apaga y queda un montón de cenizas que se desvanece.
/// El arte mira a la izquierda (como el Alebrije). Animator: Volar, "Lanzar", "Picada", "Golpe" y "Muerto".
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class IABruja : MonoBehaviour
{
    public enum Estado { Flotar, Acechar, Picada, Subir, Muerta }

    [Header("Vuelo")]
    [SerializeField] float alturaVuelo = 3.5f;        // Sobre el suelo
    [SerializeField] float velocidad = 3f;
    [SerializeField] float amplitudZigzag = 0.8f;
    [SerializeField] float frecuenciaZigzag = 1.6f;
    [SerializeField] float radioFlotar = 3f;          // Mientras no ve a Mateo va y viene sobre su origen
    [SerializeField] float distanciaAcecho = 4.5f;    // Se queda a este lado de Mateo, no justo encima
    [SerializeField] float rangoVision = 11f;
    [SerializeField] float ySuelo = -4.5f;

    [Header("Fuego")]
    [SerializeField] BalaEnemiga prefabBola;
    [SerializeField] int danoBola = 10;
    [SerializeField] float velocidadBola = 6f;
    [SerializeField] float intervaloFuego = 2.6f;
    [SerializeField] float preparacionFuego = 0.45f;  // Junta la bola entre las garras: es el aviso

    [Header("Picada")]
    [SerializeField] float intervaloPicada = 5f;
    [SerializeField] float velocidadPicada = 9f;
    [SerializeField] float duracionPicadaMaxima = 1.2f;

    [Header("Golpe y muerte")]
    [SerializeField] float retroceso = 2.5f;
    [SerializeField] float tiempoCenizas = 1.5f;
    [SerializeField] float alturaCadaver = 0.85f;     // Medio alto del lienzo de los cuadros (lo pone el constructor)
    [SerializeField] Animator animator;
    [SerializeField] Transform objetivo;              // Si está vacío, busca a Mateo al iniciar

    static readonly int AnimLanzar = Animator.StringToHash("Lanzar");
    static readonly int AnimPicada = Animator.StringToHash("Picada");
    static readonly int AnimGolpe = Animator.StringToHash("Golpe");
    static readonly int AnimMuerto = Animator.StringToHash("Muerto");

    public Estado EstadoActual { get; private set; } = Estado.Flotar;
    public int Direccion { get; private set; } = -1;  // -1 = izquierda (hacia donde mira el arte)
    public Vector2 Origen { get; private set; }

    Rigidbody2D rb;
    Health salud;
    Health saludObjetivo;
    Collider2D col;
    float fase;
    float siguienteFuego, siguientePicada, finPicada, finGolpe;
    Vector2 destinoPicada;
    bool lanzando;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Health>();
        col = GetComponent<Collider2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;      // Vuela: no la afecta la gravedad
        rb.useFullKinematicContacts = true;           // Sin esto la cerámica (también cinemática) no la detecta
        col.isTrigger = true;                         // Lastima al tocar (DanoPorContacto) sin empujar a Mateo
        fase = Random.value * 10f;
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
        siguienteFuego = Time.time + intervaloFuego;
        siguientePicada = Time.time + intervaloPicada;
    }

    void OnEnable() { salud.OnDano += AlRecibirDano; salud.OnMuerte += AlMorir; }
    void OnDisable() { salud.OnDano -= AlRecibirDano; salud.OnMuerte -= AlMorir; }

    void FixedUpdate()
    {
        if (EstadoActual == Estado.Muerta) return;
        if (!ObjetivoDisponible())
        {
            // Pausa, cinemática o Mateo muerto: se queda flotando donde está
            if (EstadoActual != Estado.Flotar) { EstadoActual = Estado.Flotar; Origen = rb.position; }
            Flotar();
            return;
        }

        bool loVe = Mathf.Abs(objetivo.position.x - rb.position.x) <= rangoVision;
        switch (EstadoActual)
        {
            case Estado.Flotar:
                Flotar();
                if (loVe) EstadoActual = Estado.Acechar;
                break;
            case Estado.Acechar: Acechar(); break;
            case Estado.Picada: Picada(); break;
            case Estado.Subir: Subir(); break;
        }
    }

    void Flotar()
    {
        float t = Time.time + fase;
        var destino = new Vector2(Origen.x + Mathf.Sin(t * 0.5f) * radioFlotar, Altura(t));
        Mover(destino, velocidad * 0.6f);
    }

    void Acechar()
    {
        float t = Time.time + fase;
        // Se pone del lado donde ya está, a distanciaAcecho de Mateo, en zigzag
        int lado = rb.position.x >= objetivo.position.x ? 1 : -1;
        var destino = new Vector2(objetivo.position.x + lado * distanciaAcecho, Altura(t));
        Mover(destino, velocidad);
        Mirar(objetivo.position.x - rb.position.x);

        if (Time.time < finGolpe || lanzando) return;
        if (Time.time >= siguientePicada && Mathf.Abs(objetivo.position.x - rb.position.x) < distanciaAcecho + 2f)
        {
            EstadoActual = Estado.Picada;
            destinoPicada = (Vector2)objetivo.position + Vector2.up * 0.8f;
            finPicada = Time.time + duracionPicadaMaxima;
            if (animator != null) animator.SetBool(AnimPicada, true);
            return;
        }
        if (Time.time >= siguienteFuego) StartCoroutine(LanzarFuego());
    }

    void Picada()
    {
        Mover(destinoPicada, velocidadPicada);
        Mirar(destinoPicada.x - rb.position.x);
        // El daño lo hace DanoPorContacto al chocar con Mateo
        if (Time.time >= finPicada || Vector2.Distance(rb.position, destinoPicada) < 0.3f) EmpezarASubir();
    }

    void Subir()
    {
        var destino = new Vector2(rb.position.x, ySuelo + alturaVuelo);
        Mover(destino, velocidad * 1.3f);
        if (Mathf.Abs(rb.position.y - destino.y) < 0.2f) EstadoActual = Estado.Acechar;
    }

    void EmpezarASubir()
    {
        EstadoActual = Estado.Subir;
        siguientePicada = Time.time + intervaloPicada;
        if (animator != null) animator.SetBool(AnimPicada, false);
    }

    IEnumerator LanzarFuego()
    {
        lanzando = true;
        siguienteFuego = Time.time + intervaloFuego;
        if (animator != null) animator.SetTrigger(AnimLanzar);
        yield return new WaitForSeconds(preparacionFuego);
        lanzando = false;
        if (EstadoActual != Estado.Acechar || !ObjetivoDisponible() || prefabBola == null) yield break;

        // Sale de las garras (al frente y un poco abajo) directo hacia Mateo
        Vector2 origen = rb.position + new Vector2(Direccion * 0.9f, -0.2f);
        Vector2 hacia = (Vector2)objetivo.position + Vector2.up * 1.2f - origen;
        Reciclaje.Sacar(prefabBola, origen, Quaternion.identity).Disparar(hacia, velocidadBola, danoBola);
        GestorAudio.Reproducir(Sfx.Disparo, 0.15f);
    }

    void AlRecibirDano(int cantidad)
    {
        if (salud.EstaMuerto) return;
        if (animator != null) animator.SetTrigger(AnimGolpe);
        finGolpe = Time.time + 0.3f;
        int lado = objetivo != null && objetivo.position.x > rb.position.x ? -1 : 1;
        rb.position += new Vector2(lado * retroceso * 0.1f, 0.15f);
        if (EstadoActual == Estado.Picada) EmpezarASubir();   // Un golpe la saca de la picada
    }

    void AlMorir()
    {
        EstadoActual = Estado.Muerta;
        StopAllCoroutines();
        if (animator != null) { animator.SetBool(AnimPicada, false); animator.SetBool(AnimMuerto, true); }
        var dano = GetComponent<DanoPorContacto>();
        if (dano != null) dano.enabled = false;
        col.enabled = false;                          // Ni estorba ni la vuelve a tocar la cerámica
        StartCoroutine(Desvanecer());
    }

    IEnumerator Desvanecer()
    {
        // Cae hasta el suelo mientras se apaga. Los cuadros de muerte se apoyan abajo de su lienzo (centrado en el
        // pivote), así que el centro queda medio lienzo arriba del suelo para que las cenizas pisen la tierra
        float yFinal = ySuelo + alturaCadaver;
        for (float v = 0f; rb.position.y > yFinal; v += 18f * Time.fixedDeltaTime)
        {
            rb.MovePosition(new Vector2(rb.position.x, Mathf.Max(yFinal, rb.position.y - v * Time.fixedDeltaTime)));
            yield return new WaitForFixedUpdate();
        }
        yield return new WaitForSeconds(tiempoCenizas);
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        for (float t = 0f; t < 0.6f; t += Time.deltaTime)
        {
            foreach (var s in sprites) { var c = s.color; c.a = 1f - t / 0.6f; s.color = c; }
            yield return null;
        }
        Destroy(gameObject);
    }

    // ------------------------------------------------------------------ Utilidades
    float Altura(float t) => ySuelo + alturaVuelo + Mathf.Sin(t * frecuenciaZigzag * Mathf.PI) * amplitudZigzag;

    void Mover(Vector2 destino, float rapidez) =>
        rb.MovePosition(Vector2.MoveTowards(rb.position, destino, rapidez * Time.fixedDeltaTime));

    void Mirar(float dx)
    {
        int lado = dx >= 0f ? 1 : -1;
        if (Mathf.Abs(dx) < 0.1f || lado == Direccion) return;
        Direccion = lado;
        Vector3 e = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(e.x) * -lado, e.y, e.z);   // El arte mira a la izquierda
    }

    bool ObjetivoDisponible()
    {
        if (objetivo == null || (saludObjetivo != null && saludObjetivo.EstaMuerto)) return false;
        var gestor = GestorNivel.Actual;
        return gestor == null || gestor.EstadoActual == GestorNivel.Estado.Jugando;
    }
}
