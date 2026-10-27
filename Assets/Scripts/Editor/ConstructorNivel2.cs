using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConfigurarSprint1;

/// <summary>
/// Nivel 2: Mineral de Pozos (pueblo fantasma y ruinas mineras de noche con tormenta). Lo arma
/// "Alfarero > Construir juego completo" junto con el resto; reutiliza los helpers de ConstructorJuego.
///
/// Recorrido (5 tramos de Flow): hornos de Santa Brígida → calle del pueblo → bocamina con emboscada →
/// cañada (reservada para la estampida de nahuales, #53) → capilla en ruinas (arena del Jinete sin Cabeza, #56).
/// Más difícil que Dolores: más esqueletos y bandidos, emboscada de 3 oleadas y menos curación.
/// Mientras no exista el Jinete, llegar a la puerta de la capilla completa el nivel (MetaNivel).
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaNivel2 = "Assets/Scenes/Nivel2_Pozos.unity";

    // Tramos en orden. solapePx medido entre bordes al preparar las imágenes (el 5.º usa el 12 % pedido a Flow)
    static readonly (string archivo, int solapePx)[] SegmentosNivel2 =
    {
        ("lvl2_seg01.png", 0),
        ("lvl2_seg02.png", 336),
        ("lvl2_seg03.png", 608),
        ("lvl2_seg04.png", 596),
        ("lvl2_seg05.png", 330),
    };

    // Zonas que el recorrido deja libres para el trabajo de Noé (ver la guía del Nivel 2)
    const float InicioEstampida = 45f, FinEstampida = 60f;
    const float ArenaLargo = 21f;                    // Como la plaza de la parroquia en Dolores

    static void CrearEscenaNivel2(Prefabs p)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefabAlfarero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabAlfarero);
        var prefabMinero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEsqueleto);
        GameObject jugador = CrearBaseNivel(prefabAlfarero, SegmentosNivel2, out _, out float derecha);

        // Al guardar prefabs Unity puede invalidar las referencias en memoria: se recargan de disco
        p.revolver = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Revolver.prefab");
        p.rifle = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Rifle.prefab");
        p.escopeta = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Escopeta.prefab");
        p.machete = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Machete.prefab");

        var obstaculos = new GameObject("Obstaculos").transform;
        var enemigos = new GameObject("Enemigos").transform;
        var recogibles = new GameObject("Recogibles").transform;
        float altoHuacal = Spr("Obstacles/huacal_00.png").bounds.size.y * 0.9f;   // para apilar

        // Tramo 1: hornos de Santa Brígida. Los mineros salen de a dos
        Poner(p.huacalMonedas, -2f, YSuelo, obstaculos);
        Minero(prefabMinero, 2f, enemigos);
        Minero(prefabMinero, 5f, enemigos);
        PonerBandido(p.revolver, 8.5f, -1, enemigos);
        Poner(p.maceta, 10.5f, 0f, obstaculos);

        // Tramo 2: calle del pueblo fantasma. Bandidos cubiertos y mineros en la calle
        Poner(p.escombros, 12.5f, YSuelo, obstaculos);
        Minero(prefabMinero, 14.5f, enemigos);
        Poner(p.huacalMonedas, 16f, YSuelo, obstaculos);
        PonerBandido(p.rifle, 17.5f, -1, enemigos);              // Se cubre detrás del huacal
        Poner(p.maceta, 19.5f, 0f, obstaculos);
        PonerBandido(p.machete, 21f, -1, enemigos);
        Poner(p.hornoMonedas, 23f, YSuelo, obstaculos);
        PonerBandido(p.escopeta, 25f, -1, enemigos);
        Minero(prefabMinero, 27f, enemigos);
        Minero(prefabMinero, 28.5f, enemigos);
        Poner(p.huacalMonedas, 30.5f, YSuelo, obstaculos);
        Poner(p.huacalGuacamaya, 30.5f, YSuelo + altoHuacal, obstaculos);   // La única curación antes de la emboscada

        // Tramo 3: bocamina y castillete. Emboscada de 3 oleadas (una más que en Dolores)
        CrearEmboscada(35f, new[]
        {
            new[] { (p.machete, true, 0f), (p.machete, false, 0.6f), (p.revolver, true, 1f) },
            new[] { (p.rifle, false, 0f), (p.escopeta, true, 0.8f), (p.machete, false, 1f) },
            new[] { (p.escopeta, true, 0f), (p.rifle, false, 0.7f), (p.revolver, true, 0.7f), (p.machete, false, 0.8f) },
        });
        Minero(prefabMinero, 40f, enemigos);
        Poner(p.hornoCaguama, 42.5f, YSuelo, obstaculos);       // Caguama antes de la cañada

        // Tramo 4: la cañada queda plana y sin obstáculos para la estampida de nahuales (#53)
        var estampida = new GameObject("Zona_Estampida");
        estampida.transform.position = new Vector3((InicioEstampida + FinEstampida) / 2f, YSuelo + 2f, 0f);
        Poner(p.guacamaya, FinEstampida - 1.5f, YSuelo + 0.7f, recogibles);

        // Tramo 5: patio de la capilla, libre como arena del Jinete sin Cabeza (#56)
        float inicioArena = derecha - ArenaLargo;
        var arena = new GameObject("Arena_Jinete");
        arena.transform.position = new Vector3(inicioArena + ArenaLargo / 2f, YSuelo + 2f, 0f);

        // Provisional hasta que exista el Jinete: llegar a la puerta de la capilla completa el nivel
        var meta = new GameObject("Meta_Capilla");
        meta.transform.position = new Vector3(derecha - 6f, YSuelo + 2f, 0f);
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
        so.FindProperty("numeroNivel").intValue = 2;
        so.FindProperty("tituloMision").stringValue = "MISION 2";
        so.FindProperty("nombreNivel").stringValue = "MINERAL DE POZOS";
        so.FindProperty("musica").enumValueIndex = (int)Musica.Nivel2;
        so.FindProperty("objetivoRecorrido").stringValue = "CRUZA EL PUEBLO FANTASMA";
        so.FindProperty("xInicioRecorrido").floatValue = jugador.transform.position.x;
        so.FindProperty("xFinRecorrido").floatValue = meta.transform.position.x;
        so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(hud, "gestor", gestor);

        EditorSceneManager.SaveScene(escena, RutaNivel2);
    }
}
