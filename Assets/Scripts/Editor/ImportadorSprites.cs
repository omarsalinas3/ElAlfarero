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
                            || carpeta == "Bosses" || (carpeta == "Obstacles" && !nombre.StartsWith("maceta") && !nombre.StartsWith("roca_"))    // La bruja vuela: pivote al centro
                            || nombre.StartsWith("huella_") || nombre.StartsWith("chorro_")                       // Las huellas y los chorros salen del suelo
                            || nombre.StartsWith("charro_llamarada");                                             // y las llamaradas del Charro también
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
        if (nombre.StartsWith("mateo_golpe")) return 94f;     // Combo (#62): la hoja salió más chica; mide igual que en reposo
        if (nombre.StartsWith("bandido_")) return 100f;
        if (nombre.StartsWith("alebrije_")) return 55f;
        if (nombre.StartsWith("montado_")) return 48f;        // Mateo montado en el alebrije: mismo tamaño que los dos por separado
        if (nombre.StartsWith("esqueleto_")) return 46f;      // Minero animado: mide igual que el sprite fijo (prefab a escala 0.55)
        if (nombre.StartsWith("bruja_bola")) return 160f;     // Bola de fuego: ~1.2 u de largo con todo y estela
        if (nombre.StartsWith("bruja_")) return 110f;         // Bruja de Fuego: ~2.9 u de ancho, como un bandido acostado
        if (nombre.StartsWith("nahual_")) return 155f;        // Nahual de la estampida: ~2.8 u de largo y ~1.8 de alto, más bajo que el salto de Mateo
        if (nombre.StartsWith("jinete_calavera")) return 90f; // Calavera en llamas: ~1.1 u con todo y estela
        if (nombre.StartsWith("jinete_")) return 42f;         // Jinete sin Cabeza: ~6 u de largo y ~4.5 de alto, más grande que Mateo
        if (nombre.StartsWith("llorona_")) return 40f;        // La Llorona (#59): ~4.3 u de alto, flotando sobre Mateo
        if (nombre.StartsWith("onda_sonica")) return 32f;     // Onda sónica de la Llorona: ~1 x 1.6 u (pixel art)
        if (nombre.StartsWith("chorro_")) return 32f;         // Chorro de agua de la Llorona: ~1.5 u de ancho y 4 de alto
        // El Charro Negro (#89): las dos hojas salieron a escalas distintas; montado mide más que el Jinete y a pie
        // queda de ~3.8 u, el mismo tamaño que tiene sentado en el caballo
        if (nombre.StartsWith("charro_caballo")) return 36f;  // ~5.1 u de alto y ~7 de largo: el jefe más grande
        if (nombre.StartsWith("charro_pie")) return 45f;      // ~3.8 u de alto: más alto que Mateo (~2.5)
        if (nombre.StartsWith("charro_fuego")) return 95f;    // Ráfaga de fuego infernal: ~2 u con todo y estela
        if (nombre.StartsWith("charro_llamarada")) return 80f; // Llamarada del suelo: ~4.5 u, más alta que el salto de Mateo (~3.3)
        if (nombre.StartsWith("huella_fuego")) return 32f;    // Herradura en llamas del viaje a Pozos: ~0.9 u (pixel art de 28 px)
        // Armas de la carpeta Modelos (#33), recortadas a 2K
        if (nombre.StartsWith("caja_")) return 330f;          // Cajas de arma: ~1 u, como la de Talavera
        if (nombre.StartsWith("negro_bomba")) return 450f;    // Granada de Barro Negro: ~0.8 u con todo y humo
        if (nombre.StartsWith("negro_explosion")) return 200f; // Explosión: ~4 u, igual al radio de daño (2.2)
        if (nombre.StartsWith("escudo_maceta")) return 260f;  // Maceta Escudo: ~1.7 u, le cubre el cuerpo a Mateo
        // Secretos (#63), de objetos_secretos_flow.jpg
        if (nombre.StartsWith("muro_agrietado")) return 75f;  // ~3.8 u de alto: más que el salto de Mateo (~3.3)
        if (nombre.StartsWith("viga_")) return 120f;          // Plataforma de ~3 u de ancho
        if (nombre.StartsWith("tiro_mina")) return 100f;      // Bocamina de ~3 u
        if (nombre.StartsWith("exvoto")) return 150f;         // ~1 u de alto
        if (nombre.StartsWith("cofre_")) return 190f;         // ~1.2 u
        if (nombre.StartsWith("roca_")) return 300f;          // Rocas del derrumbe (#88): ~1.3 u
        if (nombre.StartsWith("sombra_")) return 110f;        // Sombra donde va a caer la roca: ~1.5 u de ancho

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
