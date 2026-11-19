using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConfigurarSprint1;

/// <summary>
/// Nivel 4: Túneles de Guanajuato (#86, #87, #88). El último nivel: los túneles bajo la ciudad hasta la guarida del
/// Charro Negro. Lo arma "Alfarero > Construir juego completo" junto con el resto.
///
/// Recorrido (fondos de Flow, Docs/Arte/Flow/lvl4_*): boca del túnel bajo la ciudad → arcos de cantera con antorchas →
/// galería de vigas (derrumbe de mina, #88) → túneles bajo la luna roja con emboscada de 4 oleadas → caverna de la
/// guarida (arena del Charro, últimos ~21 u libres y planos para que galope, #90).
/// Los tramos 2 y 4 son los fondos de túneles de la carpeta Modelos (de noche y con el cielo rojo); 1, 3 y 5 se
/// pidieron a Flow con ellos de referencia. Se preparan con Docs/Arte/preparar_tramos.py.
/// El más difícil: más bandidos y mineros que el panteón, una oleada más en la emboscada, el derrumbe y casi nada de
/// curación antes del jefe. Mientras la pelea del Charro no esté (#90), llegar a la guarida completa el nivel.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaNivel4 = "Assets/Scenes/Nivel4_Tuneles.unity";
    const string RutaPrefabRoca = "Assets/Prefabs/Obstacles/Roca_Que_Cae.prefab";

    static readonly (string archivo, int solapePx)[] SegmentosNivel4 =
    {
        ("lvl4_seg01.png", 0),
        ("lvl4_seg02.png", 320),
        ("lvl4_seg03.png", 320),
        ("lvl4_seg04.png", 320),
        ("lvl4_seg05.png", 320),
    };

    // La galería de vigas: el derrumbe ocupa una pantalla sin obstáculos
    const float CentroDerrumbe = 40f;

    static readonly (string quien, string texto)[] DialogoTuneles =
    {
        (Carmen, "¡Mateo! ¡Estoy aquí abajo! El Charro dice que al amanecer ya será tarde..."),
        (Charro, "Bienvenido a mi casa, alfarero. Aquí abajo la luna roja nunca se pone."),
        (Charro, "Mis muchachos te esperan en cada galería. Y si ellos no pueden... la mina te va a tragar."),
    };

    static void CrearEscenaNivel4(Prefabs p)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefabAlfarero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabAlfarero);
        var prefabMinero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEsqueleto);
        GameObject jugador = CrearBaseNivel(prefabAlfarero, SegmentosNivel4, out float izquierda, out float derecha);

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

        // Tramo 1: boca del túnel bajo la ciudad. Los bandidos de guardia y los mineros salen juntos
        Poner(p.huacalMonedas, -2f, YSuelo, obstaculos);
        Minero(prefabMinero, 1f, enemigos);
        PonerBandido(p.revolver, 3f, -1, enemigos);
        Minero(prefabMinero, 4.5f, enemigos);
        Poner(p.huacalVidriado, 6.5f, YSuelo, obstaculos);
        Poner(p.huacalMonedas, 6.5f, YSuelo + altoHuacal, obstaculos);
        PonerBandido(p.rifle, 8.5f, -1, enemigos);               // Se cubre detrás de los huacales
        PonerBandido(p.machete, 10f, -1, enemigos);

        // Tramo 2: arcos de cantera. Una bruja entre los arcos y bandidos cubiertos
        Minero(prefabMinero, 13f, enemigos);
        Minero(prefabMinero, 14.5f, enemigos);
        Poner(bruja.gameObject, 17f, YSuelo + 3.5f, enemigos);
        Poner(p.hornoMonedas, 19f, YSuelo, obstaculos);
        PonerBandido(p.escopeta, 21f, -1, enemigos);
        Poner(p.maceta, 22.5f, 0f, obstaculos);
        PonerBandido(p.rifle, 24.5f, -1, enemigos);
        Minero(prefabMinero, 26f, enemigos);
        Poner(p.huacalNegro, 28f, YSuelo, obstaculos);           // Barro Negro para la emboscada
        Poner(p.huacalCaguama, 30f, YSuelo, obstaculos);         // La única caguama del nivel

        // Tramo 3: galería de vigas. El techo se viene abajo (#88): la pantalla queda libre de obstáculos
        CrearDerrumbe(CentroDerrumbe);

        // Tramo 4: túneles bajo la luna roja. Emboscada de 4 oleadas, una más que en el panteón
        CrearEmboscada(54f, new[]
        {
            new[] { (p.machete, true, 0f), (p.machete, false, 0.5f), (p.revolver, true, 0.7f), (p.revolver, false, 0.7f) },
            new[] { (p.rifle, false, 0f), (p.escopeta, true, 0.6f), (p.machete, false, 0.7f), (p.machete, true, 0.7f) },
            new[] { (p.escopeta, true, 0f), (p.escopeta, false, 0.6f), (p.rifle, true, 0.6f), (p.machete, false, 0.6f) },
            new[] { (p.machete, true, 0f), (p.machete, false, 0.4f), (p.rifle, true, 0.6f), (p.escopeta, false, 0.6f), (p.revolver, true, 0.6f) },
        });
        Minero(prefabMinero, 59f, enemigos);
        Poner(bruja.gameObject, 60f, YSuelo + 4f, enemigos);
        PonerBandido(p.escopeta, 62f, -1, enemigos);
        Poner(p.guacamaya, 64f, YSuelo + 0.7f, recogibles);        // Lo único para curarse antes del Charro

        PonerSecretosNivel4(p, altoHuacal, obstaculos, recogibles);

        // Tramo 5: la guarida queda libre y plana como arena del Charro Negro (#90): ahí galopa de lado a lado
        float inicioArena = derecha - ArenaLargo;
        var arena = new GameObject("Arena_Charro");
        arena.transform.position = new Vector3(inicioArena + ArenaLargo / 2f, YSuelo + 2f, 0f);
        var meta = new GameObject("Meta_Provisional");            // Hasta que entre la pelea (#90)
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
        so.FindProperty("numeroNivel").intValue = 4;
        so.FindProperty("tituloMision").stringValue = "MISION 4";
        so.FindProperty("nombreNivel").stringValue = "TUNELES DE GUANAJUATO";
        so.FindProperty("musica").enumValueIndex = (int)Musica.Nivel3;   // La música propia del nivel es la #93
        so.FindProperty("objetivoRecorrido").stringValue = "LLEGA A LA GUARIDA DEL CHARRO";
        so.FindProperty("xInicioRecorrido").floatValue = jugador.transform.position.x;
        so.FindProperty("xFinRecorrido").floatValue = meta.transform.position.x;
        so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(hud, "gestor", gestor);
        CrearDialogoInicio(DialogoTuneles, 3.2f);

        EditorSceneManager.SaveScene(escena, RutaNivel4);
    }

    // 3 exvotos: detrás del muro agrietado de la entrada, en la viga alta entre los arcos y en los huacales apilados
    static void PonerSecretosNivel4(Prefabs p, float altoHuacal, Transform obstaculos, Transform recogibles)
    {
        Poner(p.muroSecreto, -8.4f, YSuelo, obstaculos);
        PonerExvoto(p, 0, -10.35f, YSuelo + 0.9f, recogibles);

        PonerViga(p, 15.5f, 2.4f, obstaculos);
        for (int i = 0; i < 3; i++) Poner(p.moneda, 14.8f + i * 0.7f, YSuelo + 3f, recogibles);
        PonerViga(p, 18.5f, 4.6f, obstaculos);
        PonerExvoto(p, 1, 18.5f, YSuelo + 5.4f, recogibles);

        PonerExvoto(p, 2, 6.5f, YSuelo + altoHuacal * 2f + 0.8f, recogibles);
    }

    // ------------------------------------------------------------------ Derrumbe de mina (#88)
    /// <summary>
    /// Zona del derrumbe: 4 tandas de rocas cada vez más tupidas; algunas caen justo donde está Mateo para que no
    /// baste con quedarse quieto. Reutiliza la caída de las macetas (Caida) y la estructura de la estampida.
    /// </summary>
    static Derrumbe CrearDerrumbe(float centro)
    {
        var go = new GameObject("Derrumbe");
        go.transform.position = new Vector3(centro, YSuelo + 2f, 0f);   // Como la estampida: la cámara se fija aquí
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 8f);
        var d = go.AddComponent<Derrumbe>();

        (float x, float retraso, bool sobreMateo)[][] tandas =
        {
            new[] { (-5f, 0f, false), (2f, 0.5f, false), (7f, 0.5f, false) },
            new[] { (0f, 0f, true), (-7f, 0.4f, false), (4f, 0.3f, false), (-2f, 0.4f, false) },
            new[] { (0f, 0f, true), (-8f, 0.3f, false), (8f, 0.2f, false), (1.5f, 0.3f, true), (-4f, 0.3f, false) },
            new[] { (-6f, 0f, false), (-2f, 0.15f, false), (2f, 0.15f, false), (6f, 0.15f, false), (0f, 0.6f, true), (0f, 0.5f, true) },
        };
        var so = new SerializedObject(d);
        so.FindProperty("prefabRoca").objectReferenceValue = CrearPrefabRoca();
        so.FindProperty("ySuelo").floatValue = YSuelo;
        var lista = so.FindProperty("tandas");
        lista.arraySize = tandas.Length;
        for (int i = 0; i < tandas.Length; i++)
        {
            var rocas = lista.GetArrayElementAtIndex(i).FindPropertyRelative("rocas");
            rocas.arraySize = tandas[i].Length;
            for (int j = 0; j < tandas[i].Length; j++)
            {
                var r = rocas.GetArrayElementAtIndex(j);
                r.FindPropertyRelative("x").floatValue = tandas[i][j].x;
                r.FindPropertyRelative("retraso").floatValue = tandas[i][j].retraso;
                r.FindPropertyRelative("sobreMateo").boolValue = tandas[i][j].sobreMateo;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return d;
    }

    /// <summary>Roca que cae (se recicla): la roca con su collider y la sombra que avisa dónde va a caer.</summary>
    static RocaQueCae CrearPrefabRoca()
    {
        Sprite[] variantes = Enumerable.Range(0, 3).Select(i => Spr($"Obstacles/roca_{i:00}.png")).Where(s => s != null).ToArray();
        var go = new GameObject("Roca_Que_Cae");

        var roca = new GameObject("Roca");                       // Capa Default: las ceramicas no chocan con ella
        roca.transform.SetParent(go.transform, false);
        var sr = roca.AddComponent<SpriteRenderer>();
        sr.sprite = variantes.Length > 0 ? variantes[0] : null;
        sr.sortingOrder = 12;
        var col = roca.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.55f;

        var sombra = new GameObject("Sombra");
        sombra.transform.SetParent(go.transform, false);
        sombra.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        var srSombra = sombra.AddComponent<SpriteRenderer>();
        srSombra.sprite = Spr("Effects/sombra_roca.png");
        srSombra.sortingOrder = 2;
        sombra.SetActive(false);

        var dano = go.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 20;
        soD.FindProperty("empuje").floatValue = 6f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var r = go.AddComponent<RocaQueCae>();
        var so = new SerializedObject(r);
        so.FindProperty("roca").objectReferenceValue = roca.transform;
        so.FindProperty("sombra").objectReferenceValue = srSombra;
        ArregloObjetos(so.FindProperty("variantes"), variantes);
        so.FindProperty("capaJugador").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();
        roca.SetActive(false);

        return Guardar(go, RutaPrefabRoca).GetComponent<RocaQueCae>();
    }
}
