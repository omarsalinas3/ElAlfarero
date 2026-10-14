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
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.mipmapEnabled = false;
        importador.alphaIsTransparency = true;
        importador.maxTextureSize = 4096;
        importador.spritePixelsPerUnit = PixelesPorUnidad(carpeta, nombre);

        var ajustes = new TextureImporterSettings();
        importador.ReadTextureSettings(ajustes);
        bool pivoteEnPies = carpeta == "Player" || carpeta == "Enemies" || carpeta == "Allies" || carpeta == "Bosses";
        ajustes.spriteAlignment = (int)(pivoteEnPies ? SpriteAlignment.BottomCenter : SpriteAlignment.Center);
        ajustes.spriteMeshType = SpriteMeshType.FullRect;
        importador.SetTextureSettings(ajustes);
    }

    static float PixelesPorUnidad(string carpeta, string nombre)
    {
        if (nombre.StartsWith("pixel_")) return 4f;           // Cuadro blanco 4x4 = 1 unidad (plataformas, etc.)
        switch (carpeta)
        {
            case "Environment": return 128f;                  // Fondo 2752x1536 -> 21.5 x 12 unidades
            case "Projectiles": return nombre.Contains("impact") ? 250f : 400f;
            case "Items": return 300f;
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
