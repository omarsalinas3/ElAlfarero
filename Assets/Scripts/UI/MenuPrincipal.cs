using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pantalla de título y menú principal. Primero se ve la portada con "PRESIONA CUALQUIER TECLA";
/// al presionar aparece el menú: Jugar (va a la intro), Continuar (último nivel desbloqueado), Opciones
/// (con Controles dentro), Créditos y Salir. Jugar con una partida guardada pide confirmación porque la borra.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] GameObject grupoTitulo;          // Logo
    [SerializeField] GameObject textoPresiona;        // "PRESIONA CUALQUIER TECLA"
    [SerializeField] Panel panelMenu;
    [SerializeField] Panel panelControles;
    [SerializeField] Panel panelOpciones;
    [SerializeField] Panel panelCreditos;
    [SerializeField] Panel panelConfirmar;            // "¿EMPEZAR DE NUEVO?" al jugar con partida guardada
    [SerializeField] RectTransform fondo;             // Se desplaza despacio para dar vida a la portada

    [Header("Botones")]
    [SerializeField] Button botonJugar;
    [SerializeField] Button botonContinuar;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonCreditos;
    [SerializeField] Button botonSalir;
    [SerializeField] Button botonControles;           // Dentro de Opciones
    [SerializeField] Button botonVolverControles;     // Regresa a Opciones
    [SerializeField] Button botonVolverOpciones;
    [SerializeField] Button botonVolverCreditos;
    [SerializeField] Button botonConfirmarSi;
    [SerializeField] Button botonConfirmarNo;

    bool enTitulo = true;
    Panel[] paneles;

    public bool EnTitulo => enTitulo;

    void Awake()
    {
        Time.timeScale = 1f;
        paneles = new[] { panelMenu, panelControles, panelOpciones, panelCreditos, panelConfirmar };

        botonJugar.onClick.AddListener(Jugar);
        botonContinuar.onClick.AddListener(Continuar);
        botonOpciones.onClick.AddListener(() => Cambiar(panelOpciones));
        botonCreditos.onClick.AddListener(() => Cambiar(panelCreditos));
        botonSalir.onClick.AddListener(Salir);
        botonControles.onClick.AddListener(() => Cambiar(panelControles));
        botonVolverControles.onClick.AddListener(() => Cambiar(panelOpciones));
        botonVolverOpciones.onClick.AddListener(VolverAlMenu);
        botonVolverCreditos.onClick.AddListener(VolverAlMenu);
        botonConfirmarSi.onClick.AddListener(EmpezarDeNuevo);
        botonConfirmarNo.onClick.AddListener(VolverAlMenu);

        foreach (var p in paneles) p.Cerrar();
        grupoTitulo.SetActive(true);
    }

    void Start() { GestorAudio.CambiarMusica(Musica.Menu, 1.2f); }

    void Update()
    {
        if (fondo != null)
        {
            float t = Time.unscaledTime * 0.08f;
            fondo.anchoredPosition = new Vector2(Mathf.Sin(t) * 40f, Mathf.Cos(t * 0.7f) * 12f);
        }

        if (enTitulo && AlgoPresionado())
        {
            enTitulo = false;
            if (textoPresiona != null) textoPresiona.SetActive(false);
            GestorAudio.Reproducir(Sfx.MenuAceptar, 0f);
            VolverAlMenu();
        }
    }

    static bool AlgoPresionado()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame));
    }

    // ------------------------------------------------------------------ Botones
    public void Jugar()
    {
        if (GestorGuardado.HayPartida) Cambiar(panelConfirmar);
        else CambioEscena.Cargar(Escenas.Intro);
    }

    public void Continuar()
    {
        if (!GestorGuardado.HayPartida) return;
        CambioEscena.Cargar(Escenas.Nivel(GestorGuardado.Datos.nivelDesbloqueado));
    }

    public void EmpezarDeNuevo()
    {
        GestorGuardado.Borrar();
        CambioEscena.Cargar(Escenas.Intro);
    }

    /// <summary>Abre el menú con Continuar al día (se pudo borrar la partida en Opciones).</summary>
    void VolverAlMenu()
    {
        bool hay = GestorGuardado.HayPartida;
        botonContinuar.interactable = hay;
        EnlazarVertical(new[] { botonJugar, botonContinuar, botonOpciones, botonCreditos, botonSalir });
        Cambiar(panelMenu);
        // Con partida guardada lo natural es seguirla: Continuar queda seleccionado
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject((hay ? botonContinuar : botonJugar).gameObject);
    }

    /// <summary>Teclado y gamepad: arriba/abajo recorre los botones activos y se salta los desactivados.</summary>
    static void EnlazarVertical(Button[] botones)
    {
        var activos = botones.Where(b => b.interactable).ToArray();
        for (int i = 0; i < activos.Length; i++)
        {
            activos[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = activos[(i - 1 + activos.Length) % activos.Length],
                selectOnDown = activos[(i + 1) % activos.Length],
            };
        }
    }

    void Cambiar(Panel destino)
    {
        foreach (var p in paneles) p.Cerrar();
        destino.Abrir();
    }

    void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
