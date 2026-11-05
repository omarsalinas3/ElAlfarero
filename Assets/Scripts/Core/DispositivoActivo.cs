using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

public enum TipoDispositivo { Teclado, Gamepad, Tactil }

/// <summary>
/// El último dispositivo que usó el jugador (#36): teclado/mouse, gamepad o pantalla táctil. Lo usan los carteles
/// del tutorial, el panel de controles y los avisos de "presiona..." para mostrar el botón correcto, y los controles
/// táctiles para aparecer solo cuando hacen falta. Se crea solo al arrancar.
/// </summary>
public class DispositivoActivo : MonoBehaviour
{
    static DispositivoActivo instancia;

    public static TipoDispositivo Actual { get; private set; }
    public static event Action<TipoDispositivo> OnCambio;

    /// <summary>Hay pantalla táctil (celular, tableta o laptop táctil) o es una plataforma móvil.</summary>
    public static bool HayTactil => Touchscreen.current != null || Application.isMobilePlatform;

    IDisposable escucha;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos()
    {
        instancia = null;
        Actual = TipoDispositivo.Teclado;
        OnCambio = null;
        virtuales.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (instancia != null) return;
        var go = new GameObject("DispositivoActivo");
        DontDestroyOnLoad(go);
        instancia = go.AddComponent<DispositivoActivo>();
        if (Application.isMobilePlatform) Actual = TipoDispositivo.Tactil;
    }

    /// <summary>Para pruebas y para los controles táctiles: fija el dispositivo a mano.</summary>
    public static void Usar(TipoDispositivo tipo)
    {
        if (Actual == tipo) return;
        Actual = tipo;
        OnCambio?.Invoke(tipo);
    }

    // Cualquier botón o tecla decide. Los botones táctiles en pantalla alimentan un gamepad virtual: ese no cuenta
    // como gamepad (lo marca el propio toque en la pantalla).
    void OnEnable() => escucha = InputSystem.onAnyButtonPress.Call(c => Detectar(c.device));
    void OnDisable() => escucha?.Dispose();

    static void Detectar(InputDevice d)
    {
        if (d is Touchscreen) Usar(TipoDispositivo.Tactil);
        else if (d is Gamepad && !EsVirtual(d)) Usar(TipoDispositivo.Gamepad);
        else if (d is Keyboard || d is Mouse) Usar(TipoDispositivo.Teclado);
    }

    static readonly System.Collections.Generic.HashSet<int> virtuales = new System.Collections.Generic.HashSet<int>();

    /// <summary>Lo llaman los controles táctiles con el gamepad virtual que alimentan sus botones.</summary>
    public static void RegistrarVirtual(InputDevice d) { if (d != null) virtuales.Add(d.deviceId); }

    static bool EsVirtual(InputDevice d) => virtuales.Contains(d.deviceId);

    void Update()
    {
        // Mover el stick o el mouse también cuenta (no son botones); con margen por el ruido en reposo
        var g = Gamepad.current;
        if (g != null && !EsVirtual(g) && g.leftStick.ReadValue().sqrMagnitude > 0.25f) Usar(TipoDispositivo.Gamepad);
        var m = Mouse.current;
        if (m != null && m.delta.ReadValue().sqrMagnitude > 4f && Actual != TipoDispositivo.Tactil) Usar(TipoDispositivo.Teclado);
    }

    /// <summary>Elige el texto según el dispositivo; el táctil usa el del gamepad si no tiene uno propio.</summary>
    public static string Elegir(string teclado, string gamepad, string tactil = null)
    {
        switch (Actual)
        {
            case TipoDispositivo.Gamepad: return string.IsNullOrEmpty(gamepad) ? teclado : gamepad;
            case TipoDispositivo.Tactil: return !string.IsNullOrEmpty(tactil) ? tactil : string.IsNullOrEmpty(gamepad) ? teclado : gamepad;
            default: return teclado;
        }
    }
}
