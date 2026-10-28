using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Aplica automáticamente los ajustes de importación a todo lo que se guarde en Assets/Art/Sprites:
/// Sprite, filtro Point, sin compresión, sin mipmaps y Pixels Per Unit según la carpeta.
/// Así Noé y Omar no tienen que configurar cada PNG a mano y todos usan los mismos valores.
///
/// Escala del mundo: la cámara ortográfica mide 12 unidades de alto. Mateo mide ~2.6 unidades.
/// </summary>
public class ImportadorSprites : AssetPostprocessor
{
    const string Raiz = "Assets/Art/Sprites/";

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Raiz)) return;

        var importador = (TextureImporter)assetImporter;
        string carpeta = Path.GetFileName(Path.GetDirectoryName(assetPath));
        string nombre = Path.GetFileNameWithoutExtension(assetPath);

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.filterMode = FilterMode.Point;
        // Fondos e ilustraciones (2752x1536 / 1600x900) se comprimen en alta calidad (BC7): ~4 veces menos memoria de
        // video, clave en GPUs integradas. El resto son sprites chicos y se dejan sin compresión para no perder nitidez.
        bool grande = carpeta == "Environment" && !nombre.StartsWith("pixel_") || carpeta == "Intro";
        importador.textureCompression = grande ? TextureImporterCompression.CompressedHQ : TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.maxTextureSize = 4096;
        importador.spritePixelsPerUnit = PixelesPorUnidad(carpeta, nombre);

        var ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        bool pivoteEnPies = carpeta == "Player" || (carpeta == "Enemies" && !nombre.StartsWith("bruja_")) || carpeta == "Allies"
                            || carpeta == "Bosses" || (carpeta == "Obstacles" && !nombre.StartsWith("maceta"));   // La bruja vuela: pivote al centro
        ajustes.spriteAlignment = (int)(pivoteEnPies ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        importador.SetTextureSettings(ajustes);

        // Botones y paneles de la interfaz se estiran con 9-slice sin deformar el borde
        if (carpeta == "UI" && nombre.StartsWith("boton")) importador.spriteBorder = new Vector4(8, 8, 8, 8);
        else if (carpeta == "UI" && nombre.StartsWith("panel")) importador.spriteBorder = new Vector4(20, 20, 20, 20);
        else importador.spriteBorder = Vector4.zero;
    }

    static float PixelesPorUnidad(string carpeta, string nombre)
    {
        if (nombre.StartsWith("pixel_")) return 4f;           // Cuadro blanco 4x4 = 1 unidad (plataformas, etc.)

        // Cuadros recortados de las hojas de Flow: cada hoja salió a otra escala, así que se ajusta por nombre
        // para que Mateo mida igual que en la carrera (~2.5 u), los bandidos casi lo mismo y el alebrije ~4 u.
        if (nombre.StartsWith("mateo_idle") || nombre.StartsWith("mateo_muerte")) return 148f;
        if (nombre.StartsWith("bandido_")) return 100f;
        if (nombre.StartsWith("alebrije_")) return 55f;
        if (nombre.StartsWith("esqueleto_")) return 46f;      // Minero animado: mide igual que el sprite fijo (prefab a escala 0.55)
        if (nombre.StartsWith("bruja_bola")) return 160f;     // Bola de fuego: ~1.2 u de largo con todo y estela
        if (nombre.StartsWith("bruja_")) return 110f;         // Bruja de Fuego: ~2.9 u de ancho, como un bandido acostado
        if (nombre.StartsWith("nahual_")) return 155f;        // Nahual de la estampida: ~2.8 u de largo y ~1.8 de alto, más bajo que el salto de Mateo
        if (nombre.StartsWith("jinete_calavera")) return 90f; // Calavera en llamas: ~1.1 u con todo y estela
        if (nombre.StartsWith("jinete_")) return 42f;         // Jinete sin Cabeza: ~6 u de largo y ~4.5 de alto, más grande que Mateo

        switch (carpeta)
        {
            case "Environment": return 128f;                  // Fondo 2752x1536 -> 21.5 x 12 unidades
            case "Projectiles": return nombre.Contains("impact") ? 250f : 400f;
            case "Items": return 300f;
            case "UI": return 100f;                           // Igual que el Canvas: 1 px del sprite = 1 px de referencia
            case "Intro": return 100f;
            default: return 280f;                             // Personajes y enemigos (~2.6 unidades de alto)
        }
    }

    [MenuItem("Alfarero/Reimportar sprites")]
    static void ReimportarTodos()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }
}
