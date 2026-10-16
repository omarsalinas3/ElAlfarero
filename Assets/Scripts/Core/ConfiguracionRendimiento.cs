using UnityEngine;

/// <summary>
/// Ajustes de rendimiento que se aplican solos al arrancar el juego (antes de cargar la primera escena):
/// 60 FPS fijos con VSync. En laptops evita que la GPU trabaje de más, se caliente y baje de velocidad (throttling),
/// que se siente como tirones.
/// </summary>
public static class ConfiguracionRendimiento
{
    public const int FpsObjetivo = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Aplicar()
    {
        QualitySettings.vSyncCount = 1;
        Application.targetFrameRate = FpsObjetivo;   // Respaldo si el monitor/controlador ignora el VSync
    }
}
