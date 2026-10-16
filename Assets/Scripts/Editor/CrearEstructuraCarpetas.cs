using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Menú Alfarero > Crear estructura de carpetas.
/// Crea todas las carpetas del proyecto de una vez (con .gitkeep para que Git las conserve aunque estén vacías;
/// Unity ignora los archivos que empiezan con punto, así que no genera .meta).
/// </summary>
public static class CrearEstructuraCarpetas
{
    static readonly string[] carpetas =
    {
        "Art/Sprites/Player", "Art/Sprites/Enemies", "Art/Sprites/Bosses",
        "Art/Sprites/Environment", "Art/Sprites/Projectiles", "Art/Sprites/UI",
        "Art/Sprites/Allies", "Art/Sprites/Items",
        "Art/Animations", "Art/Tilesets",
        "Audio/SFX", "Audio/Music",
        "Prefabs/Player", "Prefabs/Enemies", "Prefabs/Bosses", "Prefabs/Projectiles", "Prefabs/Environment",
        "ScriptableObjects/Ceramicas",
        "Scenes", "Tests/PlayMode",
        "Scripts/Core", "Scripts/Enemies",
        "Settings"
    };

    [MenuItem("Alfarero/Crear estructura de carpetas")]
    static void Crear()
    {
        foreach (string carpeta in carpetas)
        {
            string ruta = Path.Combine(Application.dataPath, carpeta);
            Directory.CreateDirectory(ruta);

            string keep = Path.Combine(ruta, ".gitkeep");
            if (!File.Exists(keep)) File.WriteAllText(keep, string.Empty);
        }

        AssetDatabase.Refresh();
        Debug.Log("Estructura de carpetas creada.");
    }
}
