using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// El Alebrije liberado del hechizo del Charro se vuelve la montura de Mateo (#50).
/// Montado (F): corre más rápido, salta más alto y en lugar de lanzar cerámica ataca con zarpazos (Espacio o clic).
/// Tiene su propia vida: mientras Mateo va montado, los golpes se los lleva el alebrije; si se le acaba, Mateo cae
/// y sigue a pie. Al llegar montado al final de la plaza se completa el nivel (rumbo a Mineral de Pozos).
/// El arte mira a la IZQUIERDA, por eso la escala X es negativa cuando mira a la derecha.
/// </summary>
[RequireComponent(typeof(Health))]
public class MonturaAlebrije : MonoBehaviour
{
    public enum Estado { Oculto, Libre, Montado, Agotado }

    static readonly int AnimVelocidad = Animator.StringToHash("Velocidad");

    [Header("Montado")]
    [SerializeField] float multiplicadorVelocidad = 1.4f;
    [SerializeField] float multiplicadorSalto = 1.2f;
    [SerializeField] Vector2 asiento = new Vector2(0.2f, 1.9f);      // Dónde va sentado Mateo, desde los pies del alebrije
    [SerializeField] float distanciaMontar = 3f;

    [Header("Zarpazo")]
    [SerializeField] int danoZarpazo = 30;
    [SerializeField] float cadenciaZarpazo = 0.45f;
    [SerializeField] Vector2 cajaZarpazo = new Vector2(2.6f, 2.4f);
    [SerializeField] LayerMask capasEnemigo;

    [Header("Referencias")]
    [SerializeField] Animator animator;
    [SerializeField] float xMeta = float.PositiveInfinity;           // Montado aquí se completa el nivel

    public Estado EstadoActual { get; private set; } = Estado.Oculto;
    public Health Salud => salud;
    public bool Montado => EstadoActual == Estado.Montado;
    public float XMeta => xMeta;
    public event Action OnMontado;

    Health salud;
    SpriteRenderer[] sprites;
    InputAction accionMontar, accionZarpazo;
    PlayerController jinete;
    PlayerShooter tirador;
    PlayerAnimator animJinete;
    Rigidbody2D rbJinete;
    Health saludJinete;
    Vector3 posVisualJinete;
    float siguienteZarpazo;
    bool completado;
    readonly Collider2D[] golpeados = new Collider2D[12];

    void Awake()
    {
        salud = GetComponent<Health>();
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
        if (animator == null) animator = GetComponentInChildren<Animator>();

        accionMontar = new InputAction("Montar", InputActionType.Button, "<Keyboard>/f");
        accionMontar.AddBinding("<Gamepad>/leftShoulder");
        accionZarpazo = new InputAction("Zarpazo", InputActionType.Button, "<Keyboard>/space");
        accionZarpazo.AddBinding("<Mouse>/leftButton");
        accionZarpazo.AddBinding("<Gamepad>/rightTrigger");
    }

    void OnEnable()
    {
        accionMontar.Enable();
        accionZarpazo.Enable();
        salud.OnMuerte += AlAgotarse;
        salud.OnDano += AlRecibirGolpe;
    }

    void OnDisable()
    {
        accionMontar.Disable();
        accionZarpazo.Disable();
        salud.OnMuerte -= AlAgotarse;
        salud.OnDano -= AlRecibirGolpe;
    }

    void Start() => BuscarJinete();

    void BuscarJinete()
    {
        if (jinete != null) return;
        jinete = FindFirstObjectByType<PlayerController>();
        if (jinete == null) return;
        tirador = jinete.GetComponent<PlayerShooter>();
        animJinete = jinete.GetComponentInChildren<PlayerAnimator>();
        rbJinete = jinete.GetComponent<Rigidbody2D>();
        saludJinete = jinete.GetComponent<Health>();
    }

    /// <summary>Aparece ya calmado donde quedó el jefe, esperando a Mateo.</summary>
    public void Liberar(Vector3 posicion, int direccion)
    {
        transform.position = posicion;
        Mirar(direccion);
        gameObject.SetActive(true);
        BuscarJinete();
        EstadoActual = Estado.Libre;
        Pose("Reposo", 0f);
    }

    /// <summary>True si Mateo está lo bastante cerca para subirse.</summary>
    public bool AlAlcance => jinete != null && Mathf.Abs(jinete.transform.position.x - transform.position.x) <= distanciaMontar;

    void Update()
    {
        if (jinete == null || !PuedeActuar()) return;

        if (EstadoActual == Estado.Libre)
        {
            if (AlAlcance && accionMontar.WasPressedThisFrame() && !saludJinete.EstaMuerto) Montar();
        }
        else if (EstadoActual == Estado.Montado)
        {
            if (accionMontar.WasPressedThisFrame()) { Desmontar(); return; }
            if (accionZarpazo.IsPressed() && Time.time >= siguienteZarpazo) Zarpazo();
            if (!completado && jinete.transform.position.x >= xMeta)
            {
                completado = true;
                GestorNivel.Actual?.Completar();
            }
        }
    }

    void LateUpdate()
    {
        if (EstadoActual != Estado.Montado || jinete == null) return;
        // El alebrije va debajo de Mateo: lo sigue y mira hacia donde mira él
        transform.position = jinete.transform.position;
        Mirar(jinete.DireccionMirada);
        if (animator == null) return;
        animator.SetFloat(AnimVelocidad, Mathf.Abs(rbJinete.linearVelocity.x));
        if (Time.time < siguienteZarpazo - cadenciaZarpazo * 0.4f) return;   // Deja ver el zarpazo
        if (!jinete.EstaEnSuelo) { if (!EnPose("Salto")) Pose("Salto", 0f); }
        else if (EnPose("Salto") || EnPose("Zarpazo")) Pose("Reposo", 0f);   // De ahí pasa solo a Caminar con la velocidad
    }

    public void Montar()
    {
        BuscarJinete();
        if (EstadoActual != Estado.Libre || jinete == null) return;
        EstadoActual = Estado.Montado;
        jinete.MultiplicadorVelocidad = multiplicadorVelocidad;
        jinete.MultiplicadorSalto = multiplicadorSalto;
        if (tirador != null) tirador.enabled = false;             // Montado ataca con el zarpazo, no lanza
        if (saludJinete != null) saludJinete.Protector = salud;   // Los golpes se los lleva el alebrije
        if (animJinete != null)
        {
            animJinete.Montado = true;
            posVisualJinete = animJinete.transform.localPosition;
            animJinete.transform.localPosition = posVisualJinete + (Vector3)asiento;
        }
        siguienteZarpazo = Time.time + 0.2f;                       // La misma tecla no lanza un zarpazo al montar
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        if (!float.IsInfinity(xMeta) && !completado)
        {
            GestorNivel.Actual?.MostrarLetrero("", "¡RUMBO A POZOS!", "¡A GALOPE!");
            GestorNivel.Actual?.FijarObjetivo("CABALGA FUERA DE DOLORES");
        }
        OnMontado?.Invoke();
    }

    public void Desmontar()
    {
        if (EstadoActual != Estado.Montado) return;
        EstadoActual = salud.EstaMuerto ? Estado.Agotado : Estado.Libre;
        jinete.MultiplicadorVelocidad = 1f;
        jinete.MultiplicadorSalto = 1f;
        if (tirador != null) tirador.enabled = true;
        if (saludJinete != null) saludJinete.Protector = null;
        if (animJinete != null)
        {
            animJinete.Montado = false;
            animJinete.transform.localPosition = posVisualJinete;
        }
        if (animator != null) animator.SetFloat(AnimVelocidad, 0f);
        Pose("Reposo", 0f);
    }

    void Zarpazo()
    {
        siguienteZarpazo = Time.time + cadenciaZarpazo;
        Pose("Zarpazo", 0f);
        GestorAudio.Reproducir(Sfx.Machetazo);

        int dir = jinete.DireccionMirada;
        Vector2 centro = (Vector2)jinete.transform.position + new Vector2(dir * (cajaZarpazo.x / 2f + 0.4f), cajaZarpazo.y / 2f);
        var filtro = new ContactFilter2D { useLayerMask = true, layerMask = capasEnemigo, useTriggers = true };
        int n = Physics2D.OverlapBox(centro, cajaZarpazo, 0f, filtro, golpeados);
        for (int i = 0; i < n; i++)
        {
            var h = golpeados[i].GetComponentInParent<Health>();
            if (h == null || h == salud || h == saludJinete) continue;
            bool repetido = false;                                 // Un enemigo con varios colliders recibe un solo golpe
            for (int j = 0; j < i && !repetido; j++) repetido = golpeados[j].GetComponentInParent<Health>() == h;
            if (!repetido) h.RecibirDano(danoZarpazo);
        }
    }

    // Como Mateo: tras un golpe, un momento sin recibir otro
    void AlRecibirGolpe(int _)
    {
        if (salud.EstaMuerto) return;
        salud.HacerInvulnerable(0.8f);
        GestorAudio.Reproducir(Sfx.DanoJugador);
    }

    void AlAgotarse()
    {
        // Se acabó la vida de la montura: Mateo cae y sigue a pie; el alebrije se va volando
        bool llevaba = Montado;
        Desmontar();
        EstadoActual = Estado.Agotado;
        if (llevaba) jinete.AplicarEmpuje(new Vector2(-jinete.DireccionMirada * 4f, 7f), 0.3f);
        if (saludJinete != null) saludJinete.HacerInvulnerable(1.5f);
        GestorNivel.Actual?.MostrarLetrero("", "¡EL ALEBRIJE SE CANSO!", "SIGUE A PIE");
        GestorAudio.Reproducir(Sfx.RugidoAlebrije, 0f);
        StartCoroutine(Irse());
    }

    IEnumerator Irse()
    {
        Pose("Salto", 0f);
        Vector3 desde = transform.position;
        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            transform.position = desde + new Vector3(0f, t * t * 6f, 0f);
            foreach (var s in sprites) { var c = s.color; c.a = 1f - t / 1.2f; s.color = c; }
            yield return null;
        }
        gameObject.SetActive(false);
    }

    // ------------------------------------------------------------------ Utilidades
    static bool PuedeActuar()
    {
        var gestor = GestorNivel.Actual;
        return gestor == null || gestor.EstadoActual == GestorNivel.Estado.Jugando;
    }

    void Pose(string estado, float tiempo)
    {
        if (animator != null) animator.Play(estado, 0, tiempo);
    }

    bool EnPose(string estado) => animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(estado);

    void Mirar(int lado)
    {
        Vector3 e = transform.localScale;
        transform.localScale = new Vector3(Mathf.Abs(e.x) * -lado, e.y, e.z);
    }
}
