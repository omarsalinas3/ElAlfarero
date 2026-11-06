using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Texto de ayuda que cambia con el dispositivo activo (#36): "PRESIONA CUALQUIER TECLA" / "PRESIONA START" /
/// "TOCA LA PANTALLA", o "ENTER SIGUIENTE" / "A SIGUIENTE"... Sin texto táctil usa el del gamepad.
/// </summary>
[RequireComponent(typeof(Text))]
public class TextoSegunDispositivo : MonoBehaviour
{
    [SerializeField, TextArea] string teclado;
    [SerializeField, TextArea] string gamepad;
    [SerializeField, TextArea] string tactil;

    Text texto;

    void Awake()
    {
        texto = GetComponent<Text>();
        Actualizar(DispositivoActivo.Actual);
    }

    void OnEnable() { DispositivoActivo.OnCambio += Actualizar; Actualizar(DispositivoActivo.Actual); }
    void OnDisable() { DispositivoActivo.OnCambio -= Actualizar; }

    void Actualizar(TipoDispositivo _)
    {
        if (texto != null) texto.text = DispositivoActivo.Elegir(teclado, gamepad, tactil);
    }
}
