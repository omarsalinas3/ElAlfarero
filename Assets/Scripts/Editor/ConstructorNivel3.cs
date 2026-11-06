using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConfigurarSprint1;

/// <summary>
/// Nivel 3: Panteón junto al río (noche de neblina). Lo arma "Alfarero > Construir juego completo" junto con el resto.
///
/// Recorrido (fondos de Flow, Docs/Arte/Flow/lvl3_*): entrada del panteón → nichos y charco → capillas con niebla y
/// emboscada de 3 oleadas → muelle y capilla del río → orilla abierta (arena de La Llorona, últimos ~21 u).
/// Los tramos 2 y 3 son los fondos del panteón de la carpeta Modelos; 1, 4 y 5 se pidieron a Flow con ellos de
/// referencia. Se preparan con Docs/Arte/preparar_tramos.py (2752 px y 320 px de degradado a la izquierda).
/// Más difícil que Pozos: tres Brujas de Fuego, oleadas más grandes y poca curación. Vencer a La Llorona completa el nivel.
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

    static readonly (string quien, string texto)[] DialogoPanteon =
    {
        (Charro, "Llegaste lejos, alfarero. Este río está lleno de lágrimas que nadie consuela."),
        (Carmen, "¡Mateo, ella también está hechizada! ¡La Talavera puede liberarla!"),
        (Charro, "Si cruzas el panteón, nos veremos en los túneles... si es que lo cruzas."),
    };

    static void CrearEscenaNivel3(Prefabs p)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefabAlfarero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabAlfarero);
        var prefabMinero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEsqueleto);
        GameObject jugador = CrearBaseNivel(prefabAlfarero, SegmentosNivel3, out float izquierda, out float derecha);

        // Al guardar prefabs Unity puede invalidar las referencias en memoria: se recargan de disco
        p.revolver = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Revolver.prefab");
        p.rifle = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Rifle.prefab");
        p.escopeta = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Escopeta.prefab");
        p.machete = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Machete.prefab");
        IABruja bruja = Cargar<IABruja>(RutaPrefabBruja);

        var obstaculos = new GameObject("Obstaculos").transform;
        var enemigos = new GameObject("Enemigos").transform;
        var recogibles = new GameObject("Recogibles").transform;
        float altoHuacal = Spr("Obstacles/huacal_00.png").bounds.size.y * 0.9f;   // para apilar

        // Tramo 1: entrada del panteón. Los muertos se levantan
        Poner(p.huacalMonedas, -2f, YSuelo, obstaculos);
        Minero(prefabMinero, 1f, enemigos);
        Minero(prefabMinero, 3f, enemigos);
        Poner(p.huacalVidriado, 6.5f, YSuelo, obstaculos);
        Poner(p.huacalMonedas, 6.5f, YSuelo + altoHuacal, obstaculos);
        PonerBandido(p.rifle, 8.5f, -1, enemigos);               // Se cubre detrás de los huacales
        Minero(prefabMinero, 10f, enemigos);

        // Tramo 2: nichos y charco. La primera bruja vigila desde arriba
        Poner(bruja.gameObject, 15f, YSuelo + 3.5f, enemigos);
        Minero(prefabMinero, 18f, enemigos);
        Minero(prefabMinero, 19.5f, enemigos);
        Poner(p.huacalMonedas, 21f, YSuelo, obstaculos);
        PonerBandido(p.escopeta, 23f, -1, enemigos);
        Poner(p.huacalNegro, 27f, YSuelo, obstaculos);           // Barro Negro para la emboscada de las capillas

        // Tramo 3: capillas. Emboscada de 3 oleadas, más grandes que en la bocamina de Pozos
        CrearEmboscada(33f, new[]
        {
            new[] { (p.machete, true, 0f), (p.machete, false, 0.5f), (p.revolver, true, 0.8f), (p.revolver, false, 0.8f) },
            new[] { (p.rifle, false, 0f), (p.escopeta, true, 0.7f), (p.machete, false, 0.8f), (p.machete, true, 0.8f) },
            new[] { (p.escopeta, true, 0f), (p.escopeta, false, 0.6f), (p.rifle, true, 0.7f), (p.machete, false, 0.7f) },
        });

        // Tramo 4: muelle y capilla del río. Dos brujas y lo último antes de la orilla
        Minero(prefabMinero, 42f, enemigos);
        PonerBandido(p.machete, 44f, -1, enemigos);
        Poner(bruja.gameObject, 46f, YSuelo + 3.5f, enemigos);
        PonerBandido(p.rifle, 48f, -1, enemigos);
        Minero(prefabMinero, 50f, enemigos);
        PonerBandido(p.revolver, 57f, -1, enemigos);
        Poner(bruja.gameObject, 58f, YSuelo + 4f, enemigos);
        PonerBandido(p.escopeta, 60f, -1, enemigos);
        Poner(p.huacalCaguama, 61.5f, YSuelo, obstaculos);       // Lo único para preparar la pelea
        Poner(p.guacamaya, 63.5f, YSuelo + 0.7f, recogibles);

        PonerSecretosNivel3(p, altoHuacal, obstaculos, recogibles);

        // Tramo 5: la orilla del río queda libre como arena de La Llorona
        float inicioArena = derecha - ArenaLargo;
        var arena = new GameObject("Arena_Llorona");
        arena.transform.position = new Vector3(inicioArena + ArenaLargo / 2f, YSuelo + 2f, 0f);

        // Interfaz
        CrearEventSystem();
        var hud = CrearHud(jugador, out BannerMision banner);

        // Jefa: La Llorona aparece sobre el agua al llegar a la orilla; liberarla completa el nivel
        EncuentroLlorona encuentro = CrearEncuentroLlorona(inicioArena, derecha, CrearBarraJefe(hud.transform), enemigos);
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
        so.FindProperty("musica").enumValueIndex = (int)Musica.Nivel3;
        so.FindProperty("objetivoRecorrido").stringValue = "LLEGA A LA ORILLA DEL RIO";
        so.FindProperty("xInicioRecorrido").floatValue = jugador.transform.position.x;
        so.FindProperty("xFinRecorrido").floatValue = encuentro.transform.position.x;   // Donde arranca la jefa
        so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(hud, "gestor", gestor);
        CrearDialogoInicio(DialogoPanteon, 3.2f);

        EditorSceneManager.SaveScene(escena, RutaNivel3);
    }

    // 3 exvotos: detrás del muro agrietado, en la viga alta y encima de los huacales apilados
    static void PonerSecretosNivel3(Prefabs p, float altoHuacal, Transform obstaculos, Transform recogibles)
    {
        Poner(p.muroSecreto, -8.4f, YSuelo, obstaculos);
        PonerExvoto(p, 0, -10.35f, YSuelo + 0.9f, recogibles);

        PonerViga(p, 52f, 2.4f, obstaculos);
        for (int i = 0; i < 3; i++) Poner(p.moneda, 51.3f + i * 0.7f, YSuelo + 3f, recogibles);
        PonerViga(p, 55f, 4.6f, obstaculos);
        PonerExvoto(p, 1, 55f, YSuelo + 5.4f, recogibles);

        PonerExvoto(p, 2, 6.5f, YSuelo + altoHuacal * 2f + 0.8f, recogibles);
    }
}
