using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón "BORRAR PARTIDA" de las opciones del menú. Pide presionarlo dos veces seguidas para no borrar por accidente;
/// sin partida guardada queda desactivado.
/// </summary>
[RequireComponent(typeof(Button))]
public class BotonBorrarPartida : MonoBehaviour
{
    [SerializeField] Text texto;
    [SerializeField] float tiempoParaConfirmar = 3f;

    Button boton;
    float confirmarHasta = -1f;      // Tiempo real hasta el que el segundo clic borra

    public bool EsperandoConfirmacion => confirmarHasta > 0f;

    void Awake()
    {
        boton = GetComponent<Button>();
        boton.onClick.AddListener(Presionar);
    }

    void OnEnable() { Refrescar(); }

    void Update()
    {
        if (EsperandoConfirmacion && Time.unscaledTime > confirmarHasta) Refrescar();
    }

    public void Presionar()
    {
        if (!GestorGuardado.HayPartida) return;
        if (!EsperandoConfirmacion)
        {
            confirmarHasta = Time.unscaledTime + tiempoParaConfirmar;
            texto.text = "PRESIONA DE NUEVO";
            return;
        }

        GestorGuardado.Borrar();
        // Al desactivarse el botón se perdería la selección con teclado o gamepad: pasa al de abajo (VOLVER)
        var siguiente = boton.FindSelectableOnDown();
        Refrescar();
        texto.text = "PARTIDA BORRADA";
        if (siguiente != null) siguiente.Select();
    }

    void Refrescar()
    {
        confirmarHasta = -1f;
        bool hay = GestorGuardado.HayPartida;
        boton.interactable = hay;
        texto.text = hay ? "BORRAR PARTIDA" : "SIN PARTIDA GUARDADA";
    }
}
