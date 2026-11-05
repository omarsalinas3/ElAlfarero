using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Una línea de diálogo: quién habla, su retrato y lo que dice.</summary>
[Serializable]
public class LineaDialogo
{
    public string personaje;
    public Sprite retrato;
    [TextArea(2, 4)] public string texto;
}

/// <summary>
/// Cuadro de diálogo con retrato y texto letra por letra (#35). No detiene el juego: aparece abajo como en los
/// arcades y Mateo puede seguir moviéndose. Cada línea avanza sola tras unos segundos (con el dedo no hace falta
/// tocar nada); Enter (o Select en el gamepad) completa el texto o pasa a la siguiente, y Retroceso (o mantener
/// Select) lo salta todo.
/// Uso: CuadroDialogo.Actual.Mostrar(lineas, alTerminar).
/// </summary>
public class CuadroDialogo : MonoBehaviour
{
    [SerializeField] CanvasGroup grupo;
    [SerializeField] Image retrato;
    [SerializeField] Text nombre;
    [SerializeField] Text texto;
    [SerializeField] float letrasPorSegundo = 40f;
    [SerializeField] float segundosPorLinea = 3.5f;        // Tras escribirse, cuánto espera antes de pasar sola

    public static CuadroDialogo Actual { get; private set; }

    public bool Activo { get; private set; }
    public int LineaActual { get; private set; } = -1;
    public string TextoVisible => texto != null ? texto.text : "";

    InputAction accionAvanzar, accionSaltar;
    bool avanzarPedido;
    Action alTerminar;
    Coroutine rutina;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos() { Actual = null; }

    void Awake()
    {
        Actual = this;
        accionAvanzar = new InputAction("AvanzarDialogo", InputActionType.Button, "<Keyboard>/enter");
        accionAvanzar.AddBinding("<Keyboard>/numpadEnter");
        accionAvanzar.AddBinding("<Gamepad>/select");
        accionSaltar = new InputAction("SaltarDialogo", InputActionType.Button, "<Keyboard>/backspace");
        accionSaltar.AddBinding("<Gamepad>/select").WithInteraction("hold(duration=0.6)");
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
    }

    void OnEnable() { accionAvanzar.Enable(); accionSaltar.Enable(); }
    void OnDisable() { accionAvanzar.Disable(); accionSaltar.Disable(); }
    void OnDestroy() { if (Actual == this) Actual = null; }

    void Update()
    {
        if (!Activo || Time.timeScale == 0f) return;       // En pausa no avanza
        if (accionSaltar.WasPerformedThisFrame()) { Saltar(); return; }
        if (accionAvanzar.WasPressedThisFrame()) avanzarPedido = true;
    }

    public void Mostrar(LineaDialogo[] lineas, Action alTerminar = null)
    {
        if (rutina != null) StopCoroutine(rutina);
        this.alTerminar = alTerminar;
        rutina = StartCoroutine(Reproducir(lineas));
    }

    /// <summary>Lo pasa todo y avisa que terminó.</summary>
    public void Saltar()
    {
        if (!Activo) return;
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Cerrar());
    }

    /// <summary>Como presionar Enter: completa el texto o pasa a la siguiente línea.</summary>
    public void Avanzar() => avanzarPedido = true;

    IEnumerator Reproducir(LineaDialogo[] lineas)
    {
        Activo = true;
        for (float t = 0f; t < 0.2f; t += Time.deltaTime) { grupo.alpha = t / 0.2f; yield return null; }
        grupo.alpha = 1f;

        for (int i = 0; i < lineas.Length; i++)
        {
            LineaActual = i;
            var l = lineas[i];
            nombre.text = l.personaje;
            retrato.sprite = l.retrato;
            retrato.enabled = l.retrato != null;
            texto.text = "";
            GestorAudio.Reproducir(Sfx.MenuMover, 0f);

            avanzarPedido = false;
            for (float n = 0f; n < l.texto.Length && !avanzarPedido; n += Time.deltaTime * letrasPorSegundo)
            {
                texto.text = l.texto.Substring(0, Mathf.Min(l.texto.Length, (int)n));
                yield return null;
            }
            texto.text = l.texto;

            avanzarPedido = false;
            for (float t = 0f; t < segundosPorLinea && !avanzarPedido; t += Time.deltaTime) yield return null;
        }
        yield return Cerrar();
    }

    IEnumerator Cerrar()
    {
        for (float t = 0f; t < 0.2f; t += Time.deltaTime) { grupo.alpha = 1f - t / 0.2f; yield return null; }
        grupo.alpha = 0f;
        Activo = false;
        LineaActual = -1;
        rutina = null;
        var aviso = alTerminar;
        alTerminar = null;
        aviso?.Invoke();
    }
}
