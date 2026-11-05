using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Sección GRAFICOS del panel de opciones (#26): tres botones que al presionarlos cambian su valor —calidad
/// (BAJA / MEDIA / ALTA), pantalla (COMPLETA / VENTANA) y resolución—. Se guardan con ConfiguracionGrafica.
/// </summary>
public class OpcionesGraficos : MonoBehaviour
{
    [SerializeField] Button botonCalidad;
    [SerializeField] Button botonPantalla;
    [SerializeField] Button botonResolucion;

    Resolution[] resoluciones;

    void Awake()
    {
        botonCalidad.onClick.AddListener(() =>
        {
            ConfiguracionGrafica.FijarCalidad((ConfiguracionGrafica.Calidad + 1) % ConfiguracionGrafica.NombresCalidad.Length);
            Refrescar();
        });
        botonPantalla.onClick.AddListener(() =>
        {
            ConfiguracionGrafica.FijarPantallaCompleta(!ConfiguracionGrafica.PantallaCompleta);
            Refrescar();
        });
        botonResolucion.onClick.AddListener(SiguienteResolucion);
    }

    void OnEnable()
    {
        resoluciones = ConfiguracionGrafica.Resoluciones();
        botonResolucion.interactable = ConfiguracionGrafica.PuedeCambiarResolucion && resoluciones.Length > 1;
        Refrescar();
    }

    void SiguienteResolucion()
    {
        int actual = System.Array.FindIndex(resoluciones, r => r.width == Screen.width && r.height == Screen.height);
        var siguiente = resoluciones[(actual + 1) % resoluciones.Length];
        ConfiguracionGrafica.FijarResolucion(siguiente.width, siguiente.height);
        // Screen.width cambia hasta el siguiente cuadro: se muestra la elegida
        Texto(botonResolucion).text = $"RESOLUCION  {siguiente.width}x{siguiente.height}";
    }

    void Refrescar()
    {
        Texto(botonCalidad).text = "CALIDAD  " + ConfiguracionGrafica.NombresCalidad[ConfiguracionGrafica.Calidad];
        Texto(botonPantalla).text = ConfiguracionGrafica.PantallaCompleta ? "PANTALLA  COMPLETA" : "PANTALLA  VENTANA";
        Texto(botonResolucion).text = ConfiguracionGrafica.PuedeCambiarResolucion
            ? $"RESOLUCION  {Screen.width}x{Screen.height}"
            : "RESOLUCION  AUTOMATICA";
    }

    static Text Texto(Button b) => b.GetComponentInChildren<Text>();
}
