using UnityEngine;
using UnityEngine.UI;

/// <summary>Menú de pausa: continuar, reiniciar el nivel, opciones de sonido y volver al menú principal.</summary>
public class MenuPausa : MonoBehaviour
{
    [SerializeField] Panel panelPrincipal;
    [SerializeField] Panel panelOpciones;
    [SerializeField] Button botonContinuar;
    [SerializeField] Button botonReiniciar;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonMenu;
    [SerializeField] Button botonVolverOpciones;

    void Awake()
    {
        botonContinuar.onClick.AddListener(() => GestorNivel.Actual?.Reanudar());
        botonReiniciar.onClick.AddListener(() => GestorNivel.Actual?.Reintentar());
        botonMenu.onClick.AddListener(() => GestorNivel.Actual?.IrAlMenu());
        botonOpciones.onClick.AddListener(() => { panelPrincipal.Cerrar(); panelOpciones.Abrir(); });
        botonVolverOpciones.onClick.AddListener(() => { panelOpciones.Cerrar(); panelPrincipal.Abrir(); });
        gameObject.SetActive(false);
    }

    public void Mostrar(bool visible)
    {
        gameObject.SetActive(visible);
        panelOpciones.Cerrar();
        if (visible) panelPrincipal.Abrir();
    }
}
