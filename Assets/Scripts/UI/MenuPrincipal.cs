using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Pantalla de título y menú principal. Primero se ve la portada con "PRESIONA CUALQUIER TECLA";
/// al presionar aparece el menú: Jugar (va a la intro), Controles, Opciones y Salir.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] GameObject grupoTitulo;          // Logo
    [SerializeField] GameObject textoPresiona;        // "PRESIONA CUALQUIER TECLA"
    [SerializeField] Panel panelMenu;
    [SerializeField] Panel panelControles;
    [SerializeField] Panel panelOpciones;
    [SerializeField] RectTransform fondo;             // Se desplaza despacio para dar vida a la portada

    [Header("Botones")]
    [SerializeField] Button botonJugar;
    [SerializeField] Button botonControles;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonSalir;
    [SerializeField] Button botonVolverControles;
    [SerializeField] Button botonVolverOpciones;

    bool enTitulo = true;

    public bool EnTitulo => enTitulo;

    void Awake()
    {
        Time.timeScale = 1f;
        botonJugar.onClick.AddListener(() => CambioEscena.Cargar(Escenas.Intro));
        botonControles.onClick.AddListener(() => Cambiar(panelControles));
        botonOpciones.onClick.AddListener(() => Cambiar(panelOpciones));
        botonVolverControles.onClick.AddListener(() => Cambiar(panelMenu));
        botonVolverOpciones.onClick.AddListener(() => Cambiar(panelMenu));
        botonSalir.onClick.AddListener(Salir);

        panelMenu.Cerrar(); panelControles.Cerrar(); panelOpciones.Cerrar();
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
            panelMenu.Abrir();
        }
    }

    static bool AlgoPresionado()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && (Gamepad.current.startButton.wasPressedThisFrame || Gamepad.current.buttonSouth.wasPressedThisFrame));
    }

    void Cambiar(Panel destino)
    {
        panelMenu.Cerrar(); panelControles.Cerrar(); panelOpciones.Cerrar();
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
