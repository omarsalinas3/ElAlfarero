using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.UI;

/// <summary>
/// Cartel del tutorial del Nivel 1 (#28): un letrero en el mundo que explica una acción. Muestra la tecla o el botón
/// según el último dispositivo que se usó (teclado/mouse o gamepad) y se aclara al acercarse Mateo.
/// </summary>
public class CartelTutorial : MonoBehaviour
{
    [SerializeField] Text texto;
    [SerializeField] CanvasGroup grupo;
    [SerializeField, TextArea] string textoTeclado;
    [SerializeField, TextArea] string textoGamepad;
    [SerializeField] float distanciaVisible = 7f;

    Transform jugador;
    IDisposable escucha;

    /// <summary>True si lo último que se tocó fue un gamepad (lo comparten todos los carteles).</summary>
    public static bool UsandoGamepad { get; private set; }

    // Domain reload desactivado: lo estático sobrevive entre un Play y otro
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos() => UsandoGamepad = false;

    public string TextoActual => texto != null ? texto.text : "";

    void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) jugador = pc.transform;
        Actualizar();
    }

    // Cualquier botón o tecla que se presione decide el dispositivo activo
    void OnEnable() => escucha = InputSystem.onAnyButtonPress.Call(c => UsandoGamepad = c.device is Gamepad);
    void OnDisable() => escucha?.Dispose();

    void Update()
    {
        // Mover el stick o el mouse también cuenta (no son botones); el stick con margen por el ruido en reposo
        var g = Gamepad.current;
        if (g != null && g.leftStick.ReadValue().sqrMagnitude > 0.25f) UsandoGamepad = true;
        var m = Mouse.current;
        if (m != null && m.delta.ReadValue().sqrMagnitude > 4f) UsandoGamepad = false;
        Actualizar();
    }

    void Actualizar()
    {
        if (texto != null) texto.text = UsandoGamepad && !string.IsNullOrEmpty(textoGamepad) ? textoGamepad : textoTeclado;
        if (grupo != null && jugador != null)
        {
            float d = Mathf.Abs(jugador.position.x - transform.position.x);
            float alfa = Mathf.Lerp(1f, 0.65f, Mathf.InverseLerp(distanciaVisible * 0.5f, distanciaVisible, d));
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, alfa, Time.deltaTime * 3f);
        }
    }
}
