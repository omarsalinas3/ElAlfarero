using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menú de pausa: continuar, reiniciar el nivel, opciones (audio, gráficos y controles, igual que desde el menú
/// principal) y volver al menú principal.
/// </summary>
public class MenuPausa : MonoBehaviour
{
    [SerializeField] Panel panelPrincipal;
    [SerializeField] Panel panelOpciones;
    [SerializeField] Panel panelControles;
    [SerializeField] Button botonContinuar;
    [SerializeField] Button botonReiniciar;
    [SerializeField] Button botonOpciones;
    [SerializeField] Button botonMenu;
    [SerializeField] Button botonVolverOpciones;
    [SerializeField] Button botonControles;             // Dentro de Opciones
    [SerializeField] Button botonVolverControles;       // Regresa a Opciones

    void Awake()
    {
        botonContinuar.onClick.AddListener(() => GestorNivel.Actual?.Reanudar());
        botonReiniciar.onClick.AddListener(() => GestorNivel.Actual?.Reintentar());
        botonMenu.onClick.AddListener(() => GestorNivel.Actual?.IrAlMenu());
        botonOpciones.onClick.AddListener(() => { panelPrincipal.Cerrar(); panelOpciones.Abrir(); });
        botonVolverOpciones.onClick.AddListener(() => { panelOpciones.Cerrar(); panelPrincipal.Abrir(); });
        if (panelControles != null)
        {
            botonControles.onClick.AddListener(() => { panelOpciones.Cerrar(); panelControles.Abrir(); });
            botonVolverControles.onClick.AddListener(() => { panelControles.Cerrar(); panelOpciones.Abrir(); });
        }
        gameObject.SetActive(false);
    }

    public void Mostrar(bool visible)
    {
        gameObject.SetActive(visible);
        panelOpciones.Cerrar();
        if (panelControles != null) panelControles.Cerrar();
        if (visible) panelPrincipal.Abrir();
    }
}
