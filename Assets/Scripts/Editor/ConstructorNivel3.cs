using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConfigurarSprint1;

/// <summary>
/// Nivel 3: Panteón junto al río (noche de neblina). Lo arma "Alfarero > Construir juego completo" junto con el resto.
/// Recorrido (3 tramos): entrada del panteón → criptas con
/// emboscada de 3 oleadas → mausoleos, cuyo patio queda libre como arena de La Llorona (#59).
/// Más difícil que Pozos: tres Brujas de Fuego, oleadas más grandes y una sola curación antes del patio.
/// Mientras no exista La Llorona, llegar al patio completa el nivel (MetaNivel). Cuando exista, quitar Meta_Panteon,
/// que la pelea cambie a Musica.Llorona (ya registrada) y
/// que la jefa llame a GestorNivel.Completar() al morir, como el Jinete.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaNivel3 = "Assets/Scenes/Nivel3_Panteon.unity";

    // El traslape coincide con el degradado que se les puso a los tramos (12 %, como en Pozos)
    static readonly (string archivo, int solapePx)[] SegmentosNivel3 =
    {
        ("lvl3_seg01.png", 0),
        ("lvl3_seg02.png", 330),
        ("lvl3_seg03.png", 330),
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

        // Tramo 1: entrada del panteón. Los muertos se levantan y la primera bruja vigila desde arriba
        Poner(p.huacalMonedas, -2f, YSuelo, obstaculos);
        Minero(prefabMinero, 1f, enemigos);
        Minero(prefabMinero, 3f, enemigos);
        Poner(bruja.gameObject, 5f, YSuelo + 3.5f, enemigos);
        Poner(p.huacalVidriado, 6.5f, YSuelo, obstaculos);
        Poner(p.huacalMonedas, 6.5f, YSuelo + altoHuacal, obstaculos);
        PonerBandido(p.rifle, 8.5f, -1, enemigos);               // Se cubre detrás de los huacales
        Minero(prefabMinero, 10f, enemigos);

        // Tramo 2: criptas. Emboscada de 3 oleadas, más grandes que en la bocamina de Pozos
        CrearEmboscada(14f, new[]
        {
            new[] { (p.machete, true, 0f), (p.machete, false, 0.5f), (p.revolver, true, 0.8f), (p.revolver, false, 0.8f) },
            new[] { (p.rifle, false, 0f), (p.escopeta, true, 0.7f), (p.machete, false, 0.8f), (p.machete, true, 0.8f) },
            new[] { (p.escopeta, true, 0f), (p.escopeta, false, 0.6f), (p.rifle, true, 0.7f), (p.machete, false, 0.7f) },
        });
        Poner(p.huacalNegro, 18f, YSuelo, obstaculos);           // Barro Negro para lo que falta
        Minero(prefabMinero, 20f, enemigos);
        Poner(bruja.gameObject, 22f, YSuelo + 3.5f, enemigos);
        PonerBandido(p.escopeta, 24f, -1, enemigos);
        Poner(bruja.gameObject, 25f, YSuelo + 4f, enemigos);
        Poner(p.guacamaya, 26f, YSuelo + 0.7f, recogibles);      // La única curación antes del patio

        PonerSecretosNivel3(p, altoHuacal, obstaculos, recogibles);

        // Tramo 3: patio de los mausoleos, libre como arena de La Llorona (#59)
        float inicioArena = derecha - ArenaLargo;
        var arena = new GameObject("Arena_Llorona");
        arena.transform.position = new Vector3(inicioArena + ArenaLargo / 2f, YSuelo + 2f, 0f);

        // Provisional mientras no exista la jefa: llegar al patio completa el nivel
        var meta = new GameObject("Meta_Panteon");
        meta.transform.position = new Vector3(inicioArena + 2f, YSuelo + 3f, 0f);
        var colMeta = meta.AddComponent<BoxCollider2D>();
        colMeta.isTrigger = true;
        colMeta.size = new Vector2(1f, 8f);
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
        so.FindProperty("musica").enumValueIndex = (int)Musica.Nivel3;
        so.FindProperty("objetivoRecorrido").stringValue = "CRUZA EL PANTEON";
        so.FindProperty("xInicioRecorrido").floatValue = jugador.transform.position.x;
        so.FindProperty("xFinRecorrido").floatValue = meta.transform.position.x;
        so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(hud, "gestor", gestor);

        EditorSceneManager.SaveScene(escena, RutaNivel3);
    }

    // 3 exvotos: detrás del muro agrietado, en la viga alta y encima de los huacales apilados
    static void PonerSecretosNivel3(Prefabs p, float altoHuacal, Transform obstaculos, Transform recogibles)
    {
        Poner(p.muroSecreto, -8.4f, YSuelo, obstaculos);
        PonerExvoto(p, 0, -10.35f, YSuelo + 0.9f, recogibles);

        PonerViga(p, 19.5f, 2.4f, obstaculos);
        for (int i = 0; i < 3; i++) Poner(p.moneda, 18.8f + i * 0.7f, YSuelo + 3f, recogibles);
        PonerViga(p, 22.5f, 4.6f, obstaculos);
        PonerExvoto(p, 1, 22.5f, YSuelo + 5.4f, recogibles);

        PonerExvoto(p, 2, 6.5f, YSuelo + altoHuacal * 2f + 0.8f, recogibles);
    }
}
