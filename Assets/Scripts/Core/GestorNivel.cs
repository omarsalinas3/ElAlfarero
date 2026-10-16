using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Reglas de un nivel: arranque con el letrero de misión, pausa (Esc / P), game over cuando Mateo se queda sin vida
/// y victoria al llegar a la meta. Las pantallas (pausa, game over, victoria) solo muestran; las decisiones están aquí.
/// </summary>
public class GestorNivel : MonoBehaviour
{
    public enum Estado { Jugando, Pausa, GameOver, Victoria }

    [Header("Jugador")]
    [SerializeField] Health saludJugador;
    [SerializeField] PlayerController controlJugador;
    [SerializeField] InventarioJugador inventario;

    [Header("Interfaz")]
    [SerializeField] BannerMision banner;
    [SerializeField] MenuPausa menuPausa;
    [SerializeField] PantallaFinal pantallaGameOver;
    [SerializeField] PantallaFinal pantallaVictoria;

    [Header("Nivel")]
    [SerializeField] string tituloMision = "MISION 1";
    [SerializeField] string nombreNivel = "DOLORES HIDALGO";
    [SerializeField] Musica musica = Musica.Nivel1;
    [SerializeField] float esperaGameOver = 1.6f;

    public static GestorNivel Actual { get; private set; }
    public Estado EstadoActual { get; private set; } = Estado.Jugando;
    public int EnemigosDerrotados { get; private set; }
    public float TiempoJugado { get; private set; }
    public InventarioJugador Inventario => inventario;
    public Health SaludJugador => saludJugador;

    public event Action<Estado> OnEstadoCambiado;

    InputAction accionPausa;

    void Awake()
    {
        Actual = this;
        Time.timeScale = 1f;
        accionPausa = new InputAction("Pausa", InputActionType.Button, "<Keyboard>/escape");
        accionPausa.AddBinding("<Keyboard>/p");
        accionPausa.AddBinding("<Gamepad>/start");
    }

    void OnEnable()
    {
        accionPausa.Enable();
        if (saludJugador != null) saludJugador.OnMuerte += AlMorirJugador;
    }

    void OnDisable()
    {
        accionPausa.Disable();
        if (saludJugador != null) saludJugador.OnMuerte -= AlMorirJugador;
        if (Actual == this) Actual = null;
    }

    void Start()
    {
        GestorAudio.CambiarMusica(musica);
        GestorAudio.Reproducir(Sfx.JingleMision, 0f);
        if (banner != null) banner.Mostrar(tituloMision, nombreNivel, "¡A LA CARGA!");
    }

    void Update()
    {
        if (EstadoActual == Estado.Jugando) TiempoJugado += Time.deltaTime;

        if (accionPausa.WasPressedThisFrame() && !CambioEscena.Cargando)
        {
            if (EstadoActual == Estado.Jugando) Pausar();
            else if (EstadoActual == Estado.Pausa) Reanudar();
        }
    }

    // ------------------------------------------------------------------ Pausa
    public void Pausar()
    {
        if (EstadoActual != Estado.Jugando) return;
        CambiarEstado(Estado.Pausa);
        Time.timeScale = 0f;
        GestorAudio.Atenuar(true);
        GestorAudio.Reproducir(Sfx.Pausa, 0f);
        if (menuPausa != null) menuPausa.Mostrar(true);
    }

    public void Reanudar()
    {
        if (EstadoActual != Estado.Pausa) return;
        if (menuPausa != null) menuPausa.Mostrar(false);
        Time.timeScale = 1f;
        GestorAudio.Atenuar(false);
        CambiarEstado(Estado.Jugando);
    }

    // ------------------------------------------------------------------ Fin de partida
    void AlMorirJugador()
    {
        if (EstadoActual == Estado.GameOver || EstadoActual == Estado.Victoria) return;
        CambiarEstado(Estado.GameOver);
        StartCoroutine(SecuenciaGameOver());
    }

    IEnumerator SecuenciaGameOver()
    {
        // Mateo cae al suelo (el sprite se acuesta) y se apaga la música
        var visual = controlJugador.GetComponentInChildren<SpriteRenderer>();
        var animador = controlJugador.GetComponentInChildren<Animator>();
        if (animador != null) animador.enabled = false;
        if (visual != null) visual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        GestorAudio.DetenerMusica(0.4f);

        yield return new WaitForSeconds(esperaGameOver);
        GestorAudio.Reproducir(Sfx.JingleGameOver, 0f);
        if (pantallaGameOver != null)
            pantallaGameOver.Mostrar("GAME OVER", "Carmen sigue en manos del Charro Negro...", Resumen());
    }

    /// <summary>Lo llama MetaNivel al llegar al final.</summary>
    public void Completar()
    {
        if (EstadoActual != Estado.Jugando) return;
        CambiarEstado(Estado.Victoria);
        saludJugador.HacerInvulnerable(999f);
        GestorAudio.DetenerMusica(0.3f);
        GestorAudio.Reproducir(Sfx.JingleVictoria, 0f);
        if (pantallaVictoria != null)
            pantallaVictoria.Mostrar("¡MISION CUMPLIDA!", nombreNivel + " está a salvo... por ahora.", Resumen());
    }

    public void RegistrarEnemigoDerrotado() => EnemigosDerrotados++;

    string Resumen()
    {
        int min = (int)(TiempoJugado / 60f), seg = (int)(TiempoJugado % 60f);
        int monedas = inventario != null ? inventario.Monedas : 0;
        return $"ENEMIGOS  {EnemigosDerrotados}\nMONEDAS   {monedas}\nTIEMPO    {min:00}:{seg:00}";
    }

    void CambiarEstado(Estado nuevo)
    {
        EstadoActual = nuevo;
        if (controlJugador != null) controlJugador.ControlActivo = nuevo == Estado.Jugando;
        OnEstadoCambiado?.Invoke(nuevo);
    }

    // ------------------------------------------------------------------ Botones
    public void Reintentar() => CambioEscena.Reiniciar();
    public void IrAlMenu() => CambioEscena.Cargar(Escenas.Menu);
}
