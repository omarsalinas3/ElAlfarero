using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Menú Alfarero > Construir Android (#94). Genera Builds/Android/ElAlfarero.apk para instalarlo directo en un celular
/// (sin pasar por la Play Store): horizontal, IL2CPP con ARM64 y ARMv7 (hay celulares nuevos que ya solo corren 64 bits)
/// y Android 8.0 (API 26) o mayor. Usa las mismas escenas que Windows y WebGL (sin el Sandbox de pruebas).
/// Los controles táctiles aparecen solos en el celular (DispositivoActivo/ControlesTactiles).
/// Requiere el módulo Android Build Support con su SDK, NDK y OpenJDK (Unity Hub > Installs > Add modules).
/// Gradle (Java) abre una conexión local por un socket que crea en la carpeta temporal; con un usuario de Windows con
/// espacio ("Noe Chavero") esa ruta queda abreviada y falla con "Unable to establish loopback connection". Por eso el
/// build le da a Java una carpeta temporal corta (C:\tmpgradle).
/// En consola (Unity cerrado): Unity -batchmode -quit -buildTarget Android -executeMethod ConstruirAndroid.Construir
/// </summary>
public static class ConstruirAndroid
{
    public const string Carpeta = "Builds/Android";
    public const string Paquete = "com.omarynoe.elalfarero";
    const string TemporalJava = @"C:\tmpgradle";

    /// <summary>Ajustes de Android en Player Settings (también los usa el build de la entrega final, #98).</summary>
    public static void Configurar()
    {
        PlayerSettings.companyName = "Omar y Noe";
        PlayerSettings.productName = "El Alfarero de Dolores";
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, Paquete);
        PlayerSettings.Android.bundleVersionCode = 1;

        PonerIcono();

        // Siempre acostado: el juego está diseñado en 16:9 (AjustePantalla pone barras si el celular es más ancho)
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.Android.renderOutsideSafeArea = true;   // AreaSegura acomoda la interfaz fuera de la muesca

        EditorUserBuildSettings.buildAppBundle = false;          // APK para instalar directo; el .aab es para la Play Store
    }

    /// <summary>
    /// Ícono adaptativo (en Unity 6.6 es el único que admite Android): fondo de barro oscuro y al frente el logo del
    /// equipo, más chico para que quepa en la zona segura que cada celular recorta (círculo, cuadro redondeado...).
    /// Las capas salen de Assets/Art/Icono. Se usa GetSupportedIconKinds en lugar de UnityEditor.Android para que el
    /// proyecto compile aunque no esté instalado el módulo de Android.
    /// </summary>
    static void PonerIcono()
    {
        var fondo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icono/icono_fondo.png");
        var frente = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Icono/icono_frente.png");
        if (fondo == null || frente == null) return;
        var android = UnityEditor.Build.NamedBuildTarget.Android;
        foreach (PlatformIconKind tipo in PlayerSettings.GetSupportedIconKinds(android))
        {
            PlatformIcon[] iconos = PlayerSettings.GetPlatformIcons(android, tipo);
            foreach (var icono in iconos)
            {
                if (icono.maxLayerCount >= 2) icono.SetTextures(fondo, frente);
                else icono.SetTexture(frente);
            }
            PlayerSettings.SetPlatformIcons(android, tipo, iconos);
        }
    }

    /// <summary>Gradle hereda las variables de entorno de Unity: se le pasa una carpeta temporal corta.</summary>
    static void PrepararJava()
    {
        Directory.CreateDirectory(TemporalJava);
        System.Environment.SetEnvironmentVariable("JAVA_TOOL_OPTIONS",
            $"-Djdk.net.unixdomain.tmpdir={TemporalJava} -Djava.io.tmpdir={TemporalJava}");
        System.Environment.SetEnvironmentVariable("TMP", TemporalJava);
        System.Environment.SetEnvironmentVariable("TEMP", TemporalJava);
    }

    [MenuItem("Alfarero/Construir Android (APK)")]
    public static void Construir()
    {
        Configurar();
        PrepararJava();
        string[] escenas = ConstruirWeb.EscenasDelJuego();
        if (Directory.Exists(Carpeta)) Directory.Delete(Carpeta, true);
        Directory.CreateDirectory(Carpeta);

        var r = BuildPipeline.BuildPlayer(escenas, Carpeta + "/ElAlfarero.apk", BuildTarget.Android, BuildOptions.None).summary;
        Debug.Log($"Android: {r.result}, {r.totalSize / (1024 * 1024)} MB, {r.totalErrors} errores, {r.totalTime.TotalMinutes:0.0} min");
        if (Application.isBatchMode && r.result != BuildResult.Succeeded) EditorApplication.Exit(1);
    }
}
