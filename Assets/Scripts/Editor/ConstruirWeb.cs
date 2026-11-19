using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menú Alfarero > Construir WebGL (itch.io). Genera el juego para el navegador en Builds/WebGL con los ajustes
/// que pide itch.io: Gzip con descompresión en el navegador (Brotli da errores ahí; sin compresión, itch comprime al
/// servir y el cargador de Unity se queda en 90 % con "Invalid binary data file size"), plantilla Default y 960x600.
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
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;   // El cargador descomprime si el servidor no lo hace
        // itch.io marca los archivos como gzip y el navegador los descomprime antes: el caché de datos de Unity
        // (IndexedDB) se confunde con el tamaño y el juego se queda en 90 %. Sin caché se descarga en cada visita, pero carga
        PlayerSettings.WebGL.dataCaching = false;
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
