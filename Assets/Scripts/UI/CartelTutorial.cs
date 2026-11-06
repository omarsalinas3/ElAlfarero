using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cartel del tutorial del Nivel 1 (#28): un letrero en el mundo que explica una acción. Muestra la tecla, el botón
/// del gamepad o el botón táctil según el último dispositivo que se usó (DispositivoActivo, #36) y se aclara al
/// acercarse Mateo.
/// </summary>
public class CartelTutorial : MonoBehaviour
{
    [SerializeField] Text texto;
    [SerializeField] CanvasGroup grupo;
    [SerializeField, TextArea] string textoTeclado;
    [SerializeField, TextArea] string textoGamepad;
    [SerializeField, TextArea] string textoTactil;
    [SerializeField] float distanciaVisible = 7f;

    Transform jugador;

    /// <summary>True si lo último que se tocó fue un gamepad (lo comparten todos los carteles).</summary>
    public static bool UsandoGamepad => DispositivoActivo.Actual == TipoDispositivo.Gamepad;

    public string TextoActual => texto != null ? texto.text : "";

    void Start()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) jugador = pc.transform;
        Actualizar();
    }

    void Update() => Actualizar();

    void Actualizar()
    {
        if (texto != null) texto.text = DispositivoActivo.Elegir(textoTeclado, textoGamepad, textoTactil);
        if (grupo != null && jugador != null)
        {
            float d = Mathf.Abs(jugador.position.x - transform.position.x);
            float alfa = Mathf.Lerp(1f, 0.65f, Mathf.InverseLerp(distanciaVisible * 0.5f, distanciaVisible, d));
            grupo.alpha = Mathf.MoveTowards(grupo.alpha, alfa, Time.deltaTime * 3f);
        }
    }
}
