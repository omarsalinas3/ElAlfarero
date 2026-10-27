using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menú Alfarero > Construir WebGL (itch.io). Genera el juego para el navegador en Builds/WebGL con los ajustes
/// que pide itch.io: compresión desactivada (Brotli da errores ahí), plantilla Default y canvas de 960x600.
/// Deja fuera la escena Sandbox, que solo es para las pruebas. También arma la versión de Windows (Builds/Windows).
/// En consola (Unity cerrado): Unity -batchmode -quit -buildTarget WebGL -executeMethod ConstruirWeb.Construir
/// </summary>
public static class ConstruirWeb
{
    public const string Carpeta = "Builds/WebGL";
    public const string CarpetaWindows = "Builds/Windows";

    static string[] EscenasDelJuego() => EditorBuildSettings.scenes
        .Where(e => e.enabled && !e.path.EndsWith("Sandbox.unity"))
        .Select(e => e.path).ToArray();

    [MenuItem("Alfarero/Construir Windows")]
    public static void ConstruirWindows()
    {
        if (Directory.Exists(CarpetaWindows)) Directory.Delete(CarpetaWindows, true);
        var r = BuildPipeline.BuildPlayer(EscenasDelJuego(), CarpetaWindows + "/ElAlfarero.exe", BuildTarget.StandaloneWindows64, BuildOptions.None).summary;
        Debug.Log($"Windows: {r.result}, {r.totalSize / (1024 * 1024)} MB, {r.totalErrors} errores");
        if (Application.isBatchMode && r.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }

    [MenuItem("Alfarero/Construir WebGL (itch.io)")]
    public static void Construir()
    {
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = false;
        PlayerSettings.WebGL.template = "APPLICATION:Default";
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 600;

        string[] escenas = EscenasDelJuego();

        if (Directory.Exists(Carpeta)) Directory.Delete(Carpeta, true);
        var reporte = BuildPipeline.BuildPlayer(escenas, Carpeta, BuildTarget.WebGL, BuildOptions.None);

        var r = reporte.summary;
        Debug.Log($"WebGL: {r.result}, {r.totalSize / (1024 * 1024)} MB, {r.totalErrors} errores, escenas: {string.Join(", ", escenas)}");
        if (Application.isBatchMode && r.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
