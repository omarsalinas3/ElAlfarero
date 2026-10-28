using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Movimiento del Alfarero: correr con aceleración, salto preciso (coyote time, buffer, altura variable,
/// caída más pesada) y evasión rápida con invulnerabilidad.
/// Controles: A/D o flechas = mover, flecha arriba o W = saltar, Shift izquierdo = evadir.
/// Espacio o clic = lanzar y el mouse apunta (PlayerShooter). Mateo mira hacia el último control usado: flechas o mouse.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float velocidadMaxima = 8f;
    [SerializeField] float aceleracion = 80f;       // Unidades/s² al pulsar una dirección
    [SerializeField] float desaceleracion = 100f;   // Unidades/s² al soltar (frenado casi inmediato = control preciso)

    [Header("Salto")]
    [SerializeField] float fuerzaSalto = 14f;
    [SerializeField, Range(0.1f, 1f)] float multiplicadorCorteSalto = 0.5f; // Al soltar el botón en ascenso
    [SerializeField] float multiplicadorCaida = 1.8f;                        // Gravedad extra al caer
    [SerializeField] float tiempoCoyote = 0.1f;     // Margen para saltar justo después de salir de una plataforma
    [SerializeField] float tiempoBufferSalto = 0.12f; // Margen para "adelantar" el salto antes de tocar suelo

    [Header("Evasión")]
    [SerializeField] float velocidadEvasion = 14f;
    [SerializeField] float duracionEvasion = 0.2f;
    [SerializeField] float cooldownEvasion = 0.6f;

    [Header("Detección de suelo")]
    [SerializeField] Transform puntoSuelo;          // Hijo en los pies del personaje
    [SerializeField] Vector2 tamanoCajaSuelo = new Vector2(0.5f, 0.1f);
    [SerializeField] LayerMask capaSuelo;

    // --- Estado público (lo usará el Animator más adelante) ---
    public bool EstaEnSuelo { get; private set; }
    public bool EstaEvadiendo { get; private set; }
    public int DireccionMirada { get; private set; } = 1;   // 1 = derecha, -1 = izquierda

    /// <summary>False durante pausa, muerte, victoria o cinemáticas: ignora la entrada del jugador.</summary>
    public bool ControlActivo { get; set; } = true;

    // --- Componentes ---
    Rigidbody2D rb;
    PlayerShooter tirador;
    Health salud;

    // --- Input (se crea por código para no depender de un asset .inputactions) ---
    InputAction accionMover;
    InputAction accionSaltar;
    InputAction accionEvadir;

    // --- Estado interno ---
    float entradaHorizontal;
    float gravedadBase;
    float temporizadorCoyote;
    float temporizadorBuffer;
    float temporizadorEvasion;
    float temporizadorCooldown;
    float temporizadorEmpuje;
    int direccionEvasion;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        tirador = GetComponent<PlayerShooter>();   // Opcional: si existe, el mouse decide hacia dónde mira
        salud = GetComponent<Health>();            // Opcional: para la invulnerabilidad de la evasión

        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        gravedadBase = rb.gravityScale;

        if (puntoSuelo == null)
        {
            Debug.LogError("PlayerController: falta asignar puntoSuelo (un hijo en los pies).", this);
            puntoSuelo = transform;
        }

        // Mover: A/D y flechas como eje horizontal (-1 a 1)
        accionMover = new InputAction("Mover", InputActionType.Value);
        accionMover.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/a")
            .With("Positive", "<Keyboard>/d");
        accionMover.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/leftArrow")
            .With("Positive", "<Keyboard>/rightArrow");

        // Espacio quedó para lanzar cerámica (PlayerShooter)
        accionSaltar = new InputAction("Saltar", InputActionType.Button, "<Keyboard>/upArrow");
        accionSaltar.AddBinding("<Keyboard>/w");

        accionEvadir = new InputAction("Evadir", InputActionType.Button, "<Keyboard>/leftShift");
    }

    void OnEnable()
    {
        accionMover.Enable();
        accionSaltar.Enable();
        accionEvadir.Enable();
    }

    void OnDisable()
    {
        accionMover.Disable();
        accionSaltar.Disable();
        accionEvadir.Disable();
    }

    void Update()
    {
        EstaEnSuelo = Physics2D.OverlapBox(puntoSuelo.position, tamanoCajaSuelo, 0f, capaSuelo);
        temporizadorEmpuje -= Time.deltaTime;

        if (!ControlActivo || Time.timeScale == 0f)
        {
            entradaHorizontal = 0f;
            if (EstaEvadiendo) TerminarEvasion();
            return;
        }

        entradaHorizontal = accionMover.ReadValue<float>();

        ProcesarSalto();
        ProcesarEvasion();
        ActualizarOrientacion();
    }

    void FixedUpdate()
    {
        Vector2 velocidad = rb.linearVelocity;

        if (EstaEvadiendo)
        {
            // Durante la evasión: velocidad fija horizontal, sin caer
            velocidad = new Vector2(direccionEvasion * velocidadEvasion, 0f);
        }
        else if (temporizadorEmpuje <= 0f)
        {
            // Aceleración / frenado hacia la velocidad objetivo
            float objetivo = entradaHorizontal * velocidadMaxima;
            float tasa = Mathf.Abs(objetivo) > 0.01f ? aceleracion : desaceleracion;
            velocidad.x = Mathf.MoveTowards(velocidad.x, objetivo, tasa * Time.fixedDeltaTime);
        }

        rb.linearVelocity = velocidad;

        // Gravedad: nula en evasión, reforzada al caer (salto con más "peso")
        if (EstaEvadiendo)            rb.gravityScale = 0f;
        else if (velocidad.y < 0f)    rb.gravityScale = gravedadBase * multiplicadorCaida;
        else                          rb.gravityScale = gravedadBase;
    }

    // ------------------------------------------------------------------ Salto
    void ProcesarSalto()
    {
        // Coyote: mientras estemos en suelo se recarga; en el aire se va agotando
        if (EstaEnSuelo) temporizadorCoyote = tiempoCoyote;
        else             temporizadorCoyote -= Time.deltaTime;

        // Buffer: recordamos que se pulsó saltar unos milisegundos
        if (PulsoSaltar()) temporizadorBuffer = tiempoBufferSalto;
        else               temporizadorBuffer -= Time.deltaTime;

        if (!EstaEvadiendo && temporizadorBuffer > 0f && temporizadorCoyote > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            temporizadorBuffer = 0f;   // Consumidos: evitan doble salto accidental
            temporizadorCoyote = 0f;
            GestorAudio.Reproducir(Sfx.Salto);
        }

        // Altura variable: si sueltas el botón mientras subes, cortamos el impulso
        if (accionSaltar.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * multiplicadorCorteSalto);
        }
    }

    // Mientras se lanza con Espacio, la flecha arriba apunta hacia arriba (PlayerShooter) en lugar de saltar; W siempre salta
    bool PulsoSaltar()
    {
        if (!accionSaltar.WasPressedThisFrame()) return false;
        var kb = Keyboard.current;
        bool soloFlecha = kb != null && kb.upArrowKey.wasPressedThisFrame && !kb.wKey.wasPressedThisFrame;
        return !(soloFlecha && tirador != null && tirador.LanzandoConTeclado);
    }

    // --------------------------------------------------------------- Evasión
    void ProcesarEvasion()
    {
        temporizadorCooldown -= Time.deltaTime;

        if (EstaEvadiendo)
        {
            temporizadorEvasion -= Time.deltaTime;
            if (temporizadorEvasion <= 0f) TerminarEvasion();
        }
        else if (accionEvadir.WasPressedThisFrame() && temporizadorCooldown <= 0f)
        {
            IniciarEvasion();
        }
    }

    void IniciarEvasion()
    {
        EstaEvadiendo = true;
        temporizadorEvasion = duracionEvasion;
        temporizadorCooldown = cooldownEvasion;

        // Evade hacia donde se está moviendo; si está quieto, hacia donde mira
        direccionEvasion = Mathf.Abs(entradaHorizontal) > 0.01f ? (int)Mathf.Sign(entradaHorizontal) : DireccionMirada;

        if (salud != null) salud.Invulnerable = true;   // Frames de invulnerabilidad
        GestorAudio.Reproducir(Sfx.Evadir);
    }

    /// <summary>Empujón al recibir un golpe: durante unos instantes la entrada no frena el retroceso.</summary>
    public void AplicarEmpuje(Vector2 velocidad, float duracion = 0.25f)
    {
        if (EstaEvadiendo) TerminarEvasion();
        rb.linearVelocity = velocidad;
        temporizadorEmpuje = duracion;
    }

    void TerminarEvasion()
    {
        EstaEvadiendo = false;
        if (salud != null) salud.Invulnerable = false;
    }

    // ----------------------------------------------------------- Orientación
    void ActualizarOrientacion()
    {
        // Gana el último control usado: caminar con flechas/A-D voltea hacia donde camina (y el disparo va al frente);
        // si después se mueve el mouse, mira hacia el mouse
        if (Mathf.Abs(entradaHorizontal) > 0.01f)
        {
            DireccionMirada = entradaHorizontal > 0f ? 1 : -1;
            if (tirador != null) tirador.UsarTeclado();
        }
        else if (tirador != null && tirador.UsandoMouse && Mathf.Abs(tirador.DireccionApuntado.x) > 0.01f)
        {
            DireccionMirada = tirador.DireccionApuntado.x > 0f ? 1 : -1;
        }

        // Volteamos con la escala X. Ojo: al tener sprite y collider en la misma raíz, todo se voltea junto.
        Vector3 escala = transform.localScale;
        escala.x = Mathf.Abs(escala.x) * DireccionMirada;
        transform.localScale = escala;
    }

    // Caja verde en el editor para ver y ajustar la detección de suelo
    void OnDrawGizmosSelected()
    {
        if (puntoSuelo == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(puntoSuelo.position, tamanoCajaSuelo);
    }
}
