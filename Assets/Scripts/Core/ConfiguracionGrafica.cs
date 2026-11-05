using UnityEngine;

/// <summary>
/// Opciones gráficas (#26): calidad Baja / Media / Alta (niveles de QualitySettings), pantalla completa o ventana y
/// resolución. Se guardan en PlayerPrefs y se aplican solas al arrancar el juego. En WebGL la resolución la decide la
/// página: solo aplica la calidad y la pantalla completa.
/// </summary>
public static class ConfiguracionGrafica
{
    const string ClaveCalidad = "graficos_calidad", ClaveCompleta = "graficos_completa";
    const string ClaveAncho = "graficos_ancho", ClaveAlto = "graficos_alto";

    public static readonly string[] NombresCalidad = { "BAJA", "MEDIA", "ALTA" };
    // Nivel de QualitySettings que usa cada opción (Alta es el que trae el proyecto)
    static readonly string[] NivelesCalidad = { "Low", "Medium", "Ultra" };

    public static int Calidad { get; private set; } = 2;
    // Lo elegido, no Screen.fullScreen: la pantalla cambia hasta el siguiente cuadro (y en el editor no cambia)
    public static bool PantallaCompleta { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Cargar()
    {
        FijarCalidad(PlayerPrefs.GetInt(ClaveCalidad, 2), false);
        PantallaCompleta = Screen.fullScreen;
        if (PlayerPrefs.HasKey(ClaveCompleta)) FijarPantallaCompleta(PlayerPrefs.GetInt(ClaveCompleta) == 1, false);
        if (PlayerPrefs.HasKey(ClaveAncho) && !Application.isEditor && Application.platform != RuntimePlatform.WebGLPlayer)
            Screen.SetResolution(PlayerPrefs.GetInt(ClaveAncho), PlayerPrefs.GetInt(ClaveAlto), Screen.fullScreenMode);
    }

    public static void FijarCalidad(int calidad, bool guardar = true)
    {
        Calidad = Mathf.Clamp(calidad, 0, NombresCalidad.Length - 1);
        int nivel = System.Array.IndexOf(QualitySettings.names, NivelesCalidad[Calidad]);
        if (nivel >= 0) QualitySettings.SetQualityLevel(nivel, true);
        // Cada nivel trae su propio VSync: se vuelve a dejar el de ConfiguracionRendimiento (60 FPS fijos)
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = ConfiguracionRendimiento.FpsObjetivo;
        if (guardar) { PlayerPrefs.SetInt(ClaveCalidad, Calidad); PlayerPrefs.Save(); }
    }

    public static void FijarPantallaCompleta(bool completa, bool guardar = true)
    {
        PantallaCompleta = completa;
        Screen.fullScreenMode = completa ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        Screen.fullScreen = completa;
        if (guardar) { PlayerPrefs.SetInt(ClaveCompleta, completa ? 1 : 0); PlayerPrefs.Save(); }
    }

    /// <summary>Resoluciones del monitor sin repetir (Unity las lista por cada frecuencia), de menor a mayor.</summary>
    public static Resolution[] Resoluciones()
    {
        var lista = new System.Collections.Generic.List<Resolution>();
        foreach (var r in Screen.resolutions)
            if (lista.FindIndex(x => x.width == r.width && x.height == r.height) < 0) lista.Add(r);
        if (lista.Count == 0) lista.Add(new Resolution { width = Screen.width, height = Screen.height });
        return lista.ToArray();
    }

    public static bool PuedeCambiarResolucion => Application.platform != RuntimePlatform.WebGLPlayer;

    public static void FijarResolucion(int ancho, int alto)
    {
        if (!PuedeCambiarResolucion) return;
        Screen.SetResolution(ancho, alto, Screen.fullScreenMode);
        PlayerPrefs.SetInt(ClaveAncho, ancho);
        PlayerPrefs.SetInt(ClaveAlto, alto);
        PlayerPrefs.Save();
    }

    /// <summary>Lo usan las pruebas para dejar todo como venía.</summary>
    public static void Restablecer()
    {
        PlayerPrefs.DeleteKey(ClaveCalidad);
        PlayerPrefs.DeleteKey(ClaveCompleta);
        PlayerPrefs.DeleteKey(ClaveAncho);
        PlayerPrefs.DeleteKey(ClaveAlto);
        FijarCalidad(2, false);
    }
}
