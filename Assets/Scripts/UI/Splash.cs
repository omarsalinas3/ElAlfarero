using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Primera escena del juego: logo del equipo y luego el título, cada uno con fundido de entrada y salida,
/// y después el menú principal. Cualquier tecla, clic o botón del gamepad la salta.
/// </summary>
public class Splash : MonoBehaviour
{
    [SerializeField] CanvasGroup grupoLogo;           // Emblema + "OMAR & NOE PRESENTAN"
    [SerializeField] CanvasGroup grupoTitulo;         // "EL ALFARERO DE DOLORES"
    [SerializeField] float fundido = 0.6f;
    [SerializeField] float esperaLogo = 1.4f;
    [SerializeField] float esperaTitulo = 1.6f;

    bool saliendo;

    public bool Saliendo => saliendo;

    void Start()
    {
        Time.timeScale = 1f;
        grupoLogo.alpha = 0f;
        grupoTitulo.alpha = 0f;
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (!saliendo && AlgoPresionado()) IrAlMenu();
    }

    IEnumerator Secuencia()
    {
        yield return Mostrar(grupoLogo, esperaLogo);
        yield return Mostrar(grupoTitulo, esperaTitulo);
        IrAlMenu();
    }

    IEnumerator Mostrar(CanvasGroup grupo, float espera)
    {
        yield return Fundir(grupo, 0f, 1f);
        yield return new WaitForSecondsRealtime(espera);
        yield return Fundir(grupo, 1f, 0f);
    }

    IEnumerator Fundir(CanvasGroup grupo, float desde, float hasta)
    {
        for (float t = 0f; t < fundido; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, t / fundido);
            yield return null;
        }
        grupo.alpha = hasta;
    }

    void IrAlMenu()
    {
        if (saliendo) return;
        saliendo = true;
        StopAllCoroutines();
        CambioEscena.Cargar(Escenas.Menu);
    }

    static bool AlgoPresionado()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && GamepadPresionado(Gamepad.current));
    }

    static bool GamepadPresionado(Gamepad g) =>
        g.startButton.wasPressedThisFrame || g.buttonSouth.wasPressedThisFrame || g.buttonEast.wasPressedThisFrame
        || g.buttonWest.wasPressedThisFrame || g.buttonNorth.wasPressedThisFrame;
}
