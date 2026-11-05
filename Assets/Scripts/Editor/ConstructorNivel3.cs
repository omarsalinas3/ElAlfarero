using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConfigurarSprint1;

/// <summary>
/// Nivel 3: Panteón junto al río (#58). Escena base para poblarla en el Sprint 4: fondo de 5 tramos unidos con
/// degradado, suelo, muros, Mateo, cámara, HUD, pausa y pantallas finales. Mientras la pelea con la Llorona no existe,
/// llegar a la orilla del río completa el nivel (meta provisional, como tuvo Pozos antes del Jinete).
///
/// Recorrido (fondos de Flow, Docs/Arte/Flow/lvl3_*): entrada del panteón → nichos y charco → capillas con niebla →
/// muelle y capilla del río → orilla abierta (arena de la Llorona, sin obstáculos en los últimos ~21 u).
/// Los tramos 2 y 3 son los fondos del panteón de la carpeta Modelos; 1, 4 y 5 se pidieron a Flow con ellos de
/// referencia. Se preparan con Docs/Arte/preparar_tramos.py (2752 px y 320 px de degradado a la izquierda).
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaNivel3 = "Assets/Scenes/Nivel3_Panteon.unity";

    static readonly (string archivo, int solapePx)[] SegmentosNivel3 =
    {
        ("lvl3_seg01.png", 0),
        ("lvl3_seg02.png", 320),
        ("lvl3_seg03.png", 320),
        ("lvl3_seg04.png", 320),
        ("lvl3_seg05.png", 320),
    };

    static void CrearEscenaNivel3()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefabAlfarero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabAlfarero);
        GameObject jugador = CrearBaseNivel(prefabAlfarero, SegmentosNivel3, out float izquierda, out float derecha);
        new GameObject("Obstaculos");
        new GameObject("Enemigos");
        new GameObject("Recogibles");

        // Arena de la Llorona: la orilla del río queda libre; ella flota sobre el agua, todavía sin pelear (Sprint 4)
        float inicioArena = derecha - ArenaLargo;
        var arena = new GameObject("Arena_Llorona");
        arena.transform.position = new Vector3(inicioArena + ArenaLargo / 2f, YSuelo + 2f, 0f);
        var llorona = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabLlorona));
        llorona.transform.position = new Vector3(derecha - 5f, YSuelo + 1.2f, 0f);
        llorona.transform.localScale = new Vector3(1f, 1f, 1f);   // El arte ya mira a la izquierda, hacia Mateo

        var meta = new GameObject("Meta_Provisional");
        meta.transform.position = new Vector3(inicioArena + 4f, YSuelo + 2f, 0f);
        meta.AddComponent<BoxCollider2D>().size = new Vector2(1f, 8f);
        meta.AddComponent<MetaNivel>();

        // Interfaz
        CrearEventSystem();
        var hud = CrearHud(jugador, out BannerMision banner);
        MenuPausa pausa = CrearMenuPausa();
        PantallaFinal gameOver = CrearPantallaFinal("GameOver", new Color(0.95f, 0.2f, 0.15f), new Color(0.12f, 0f, 0f, 0.8f), "REINTENTAR", 40);
        PantallaFinal victoria = CrearPantallaFinal("Victoria", Dorado, new Color(0f, 0.04f, 0.14f, 0.8f), "JUGAR DE NUEVO", 41, true);

        var gestor = new GameObject("GestorNivel").AddComponent<GestorNivel>();
        var so = new SerializedObject(gestor);
        so.FindProperty("saludJugador").objectReferenceValue = jugador.GetComponent<Health>();
        so.FindProperty("controlJugador").objectReferenceValue = jugador.GetComponent<PlayerController>();
        so.FindProperty("inventario").objectReferenceValue = jugador.GetComponent<InventarioJugador>();
        so.FindProperty("banner").objectReferenceValue = banner;
        so.FindProperty("menuPausa").objectReferenceValue = pausa;
        so.FindProperty("pantallaGameOver").objectReferenceValue = gameOver;
        so.FindProperty("pantallaVictoria").objectReferenceValue = victoria;
        so.FindProperty("numeroNivel").intValue = 3;
        so.FindProperty("tituloMision").stringValue = "MISION 3";
        so.FindProperty("nombreNivel").stringValue = "PANTEON JUNTO AL RIO";
        so.FindProperty("musica").enumValueIndex = (int)Musica.Nivel2;   // Música propia en el Sprint 4
        so.FindProperty("objetivoRecorrido").stringValue = "LLEGA A LA ORILLA DEL RIO";
        so.FindProperty("exvotosEnNivel").intValue = 0;                  // Los secretos se esconden en el Sprint 4
        so.FindProperty("xInicioRecorrido").floatValue = jugador.transform.position.x;
        so.FindProperty("xFinRecorrido").floatValue = meta.transform.position.x;
        so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(hud, "gestor", gestor);

        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), RutaNivel3);
    }
}
