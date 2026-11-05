using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Paso entre el recorrido y un cuarto escondido (#63). El tiro de mina: parado encima y presionando abajo, Mateo baja
/// con un fundido al cuarto subterráneo; la escalera del cuarto: presionando arriba regresa. El cuarto está en la misma
/// escena, lejos a la derecha, así que solo se mueve a Mateo y se cambian los límites de la cámara.
/// Mientras dura el fundido es una cinemática (sin control y los enemigos esperan).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PasoSecreto : MonoBehaviour
{
    [SerializeField] bool conArriba;                     // false: se usa con abajo (tiro); true: con arriba (escalera)
    [SerializeField] Transform destino;
    [Tooltip("Límites de la cámara al llegar (x = izquierdo, y = derecho). Vacío (0, 0): los del nivel.")]
    [SerializeField] Vector2 limitesCamara;
    [SerializeField] GameObject aviso;                   // "ABAJO" / "ARRIBA" que aparece al pararse encima
    [SerializeField] float duracionFundido = 0.35f;

    InputAction accion;
    PlayerController dentro;
    bool usando;
    static Image cortina;

    public bool Usando => usando;

    void Awake()
    {
        accion = conArriba
            ? new InputAction("SubirEscalera", InputActionType.Button, "<Keyboard>/upArrow")
            : new InputAction("BajarTiro", InputActionType.Button, "<Keyboard>/downArrow");
        accion.AddBinding(conArriba ? "<Keyboard>/w" : "<Keyboard>/s");
        accion.AddBinding(conArriba ? "<Gamepad>/dpad/up" : "<Gamepad>/dpad/down");
        accion.AddBinding(conArriba ? "<Gamepad>/leftStick/up" : "<Gamepad>/leftStick/down");   // También el joystick táctil (#36)
        GetComponent<Collider2D>().isTrigger = true;
        if (aviso != null) aviso.SetActive(false);
    }

    void OnEnable() => accion.Enable();
    void OnDisable() => accion.Disable();

    void OnTriggerEnter2D(Collider2D otro)
    {
        var j = otro.GetComponentInParent<PlayerController>();
        if (j == null) return;
        dentro = j;
        if (aviso != null) aviso.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.GetComponentInParent<PlayerController>() != dentro) return;
        dentro = null;
        if (aviso != null) aviso.SetActive(false);
    }

    void Update()
    {
        // Arriba también salta: en la escalera no se exige estar en el suelo (el salto empieza en el mismo cuadro)
        if (dentro == null || usando || !dentro.ControlActivo || (!conArriba && !dentro.EstaEnSuelo)) return;
        if (accion.WasPressedThisFrame()) Usar();
    }

    /// <summary>Lleva a Mateo al otro lado (también lo usan las pruebas).</summary>
    public void Usar()
    {
        if (!usando && dentro != null) StartCoroutine(Pasar(dentro));
    }

    IEnumerator Pasar(PlayerController jugador)
    {
        usando = true;
        var gestor = GestorNivel.Actual;
        gestor?.IniciarCinematica();
        GestorAudio.Reproducir(conArriba ? Sfx.Salto : Sfx.Evadir, 0f);
        yield return Fundir(0f, 1f);

        var rb = jugador.GetComponent<Rigidbody2D>();
        rb.position = destino.position;
        jugador.transform.position = destino.position;
        rb.linearVelocity = Vector2.zero;
        Physics2D.SyncTransforms();
        var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
        if (cam != null)
        {
            if (limitesCamara == Vector2.zero) cam.CambiarLimites();
            else cam.CambiarLimites(limitesCamara.x, limitesCamara.y);
        }

        yield return Fundir(1f, 0f);
        gestor?.TerminarCinematica();
        usando = false;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        if (cortina == null) CrearCortina();
        for (float t = 0f; t < duracionFundido; t += Time.deltaTime)
        {
            cortina.color = new Color(0f, 0f, 0f, Mathf.Lerp(desde, hasta, t / duracionFundido));
            yield return null;
        }
        cortina.color = new Color(0f, 0f, 0f, hasta);
    }

    // Una sola cortina por escena (se destruye con ella)
    static void CrearCortina()
    {
        var go = new GameObject("CortinaPasoSecreto");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var img = new GameObject("Negro").AddComponent<Image>();
        img.transform.SetParent(go.transform, false);
        img.rectTransform.anchorMin = Vector2.zero;
        img.rectTransform.anchorMax = Vector2.one;
        img.rectTransform.offsetMin = img.rectTransform.offsetMax = Vector2.zero;
        img.raycastTarget = false;
        img.color = Color.clear;
        cortina = img;
    }
}
