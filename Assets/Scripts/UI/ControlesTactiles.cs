using UnityEngine;
using UnityEngine.InputSystem.OnScreen;

/// <summary>
/// Controles táctiles en pantalla (#36): joystick virtual a la izquierda y botones a la derecha. Usan los controles
/// en pantalla del Input System, que alimentan un gamepad virtual: el juego no distingue si el botón A vino de un
/// control de verdad o de un dedo, así que no hay que duplicar la lógica de cada acción.
/// Solo aparecen si hay pantalla táctil y lo último que se usó fue el dedo (en celular desde el inicio).
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ControlesTactiles : MonoBehaviour
{
    [SerializeField] GameObject contenido;          // Joystick y botones (se apagan con el teclado o un gamepad real)

    CanvasGroup grupo;

    public bool Visibles => contenido != null && contenido.activeSelf;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        if (!DispositivoActivo.HayTactil) { gameObject.SetActive(false); return; }
        Actualizar(DispositivoActivo.Actual);
    }

    void OnEnable() { DispositivoActivo.OnCambio += Actualizar; }
    void OnDisable() { DispositivoActivo.OnCambio -= Actualizar; }

    void Actualizar(TipoDispositivo tipo)
    {
        bool ver = tipo == TipoDispositivo.Tactil;
        if (contenido != null && contenido.activeSelf != ver) contenido.SetActive(ver);
        if (grupo != null) grupo.blocksRaycasts = ver;
        // Al activarse, los botones crean su gamepad virtual: se marca para que no cuente como "gamepad de verdad"
        if (ver && contenido != null)
            foreach (var c in contenido.GetComponentsInChildren<OnScreenControl>())
                if (c.control != null) DispositivoActivo.RegistrarVirtual(c.control.device);
    }

    /// <summary>Para las pruebas en PC: muestra los controles aunque no haya pantalla táctil.</summary>
    public void Forzar()
    {
        gameObject.SetActive(true);
        DispositivoActivo.Usar(TipoDispositivo.Tactil);
        Actualizar(TipoDispositivo.Tactil);
    }
}
