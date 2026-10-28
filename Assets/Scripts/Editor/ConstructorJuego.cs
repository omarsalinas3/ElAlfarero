using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static ConfigurarSprint1;

/// <summary>
/// Menú Alfarero > Construir juego completo.
/// Sobre lo del Sprint 1 genera: biblioteca de audio, obstáculos y recogibles, el Nivel 1 jugable (HUD, pausa,
/// game over, victoria), el menú principal y la intro. Se puede repetir: sobrescribe lo generado.
/// En consola: Unity -batchmode -quit -executeMethod ConstructorJuego.Construir
/// </summary>
public static class ConstructorJuego
{
    const string RutaNivel1 = "Assets/Scenes/Nivel1_Dolores.unity";
    const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";
    const string RutaIntro = "Assets/Scenes/Intro.unity";
    const string RutaFuente = "Assets/Art/Fonts/PressStart2P-Regular.ttf";
    const string CarpetaObstaculos = "Assets/Prefabs/Obstacles/";
    const string CarpetaRecogibles = "Assets/Prefabs/Pickups/";
    const string CarpetaEfectos = "Assets/Prefabs/Effects/";
    const string RutaPrefabEsqueleto = "Assets/Prefabs/Enemies/Minero_Esqueleto.prefab";
    const string RutaPrefabAlebrije = "Assets/Prefabs/Bosses/Alebrije.prefab";
    const int CapaProyectilEnemigo = 10;

    static readonly Color Crema = new Color(1f, 0.94f, 0.8f);
    static readonly Color Dorado = new Color(1f, 0.8f, 0.25f);
    static readonly Color AzulTalavera = new Color(0.3f, 0.5f, 1f);
    static readonly Color CafeOscuro = new Color(0.25f, 0.1f, 0.04f);

    static Font fuente;
    static Sprite sprBoton, sprBotonResaltado, sprPanel;

    class Prefabs
    {
        public GameObject huacalMonedas, huacalGuacamaya, huacalCaguama, hornoCaguama, hornoMonedas;
        public GameObject escombros, maceta, guacamaya, caguama, moneda;
        public IABandido revolver, rifle, escopeta, machete;
        public JefeAlebrije alebrije;
    }

    [MenuItem("Alfarero/Construir juego completo")]
    public static void Construir()
    {
        ConfigurarSprint1.Construir();

        Directory.CreateDirectory(CarpetaObstaculos);
        Directory.CreateDirectory(CarpetaRecogibles);
        Directory.CreateDirectory(CarpetaEfectos);
        Directory.CreateDirectory("Assets/Resources");
        AssetDatabase.Refresh();

        ConfigurarFuente();
        ConfigurarRender();
        sprBoton = Spr("UI/boton.png");
        sprBotonResaltado = Spr("UI/boton_resaltado.png");
        sprPanel = Spr("UI/panel.png");

        CrearBibliotecaAudio();
        ConstructorAnimaciones.Construir();
        Prefabs p = CrearPrefabs();

        CrearEscenaNivel1(p);
        CrearEscenaMenu();
        CrearEscenaIntro();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(RutaMenu, true),
            new EditorBuildSettingsScene(RutaIntro, true),
            new EditorBuildSettingsScene(RutaNivel1, true),
            new EditorBuildSettingsScene(RutaEscena, true),   // Sandbox de pruebas
        };
        PlayerSettings.productName = "El Alfarero de Dolores";

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Juego construido: menú, intro y Nivel 1.");
    }

    // ================================================================== Recursos
    static void ConfigurarFuente()
    {
        var imp = (TrueTypeFontImporter)AssetImporter.GetAtPath(RutaFuente);
        imp.fontRenderingMode = FontRenderingMode.HintedRaster;   // Bordes nítidos para fuente pixel
        imp.fontTextureCase = FontTextureCase.Dynamic;
        imp.SaveAndReimport();
        fuente = AssetDatabase.LoadAssetAtPath<Font>(RutaFuente);
    }

    /// <summary>
    /// Un juego 2D sin efectos de luz intensos no necesita HDR: apagarlo usa búferes de color más ligeros
    /// (menos ancho de banda de memoria, que es lo que más sufre una GPU integrada).
    /// </summary>
    static void ConfigurarRender()
    {
        var urp = AssetDatabase.LoadMainAssetAtPath("Assets/Settings/UniversalRP.asset");
        if (urp == null) return;
        var so = new SerializedObject(urp);
        var hdr = so.FindProperty("m_SupportsHDR");
        if (hdr != null) hdr.boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(urp);
    }

    static void CrearBibliotecaAudio()
    {
        const string ruta = "Assets/Resources/BibliotecaAudio.asset";
        var b = ScriptableObject.CreateInstance<BibliotecaAudio>();

        (Sfx id, string[] archivos, float vol)[] efectos =
        {
            (Sfx.Salto, new[] { "salto" }, 0.6f),
            (Sfx.Evadir, new[] { "evadir" }, 0.7f),
            (Sfx.Lanzar, new[] { "lanzar" }, 0.45f),
            (Sfx.ImpactoCeramica, new[] { "impacto_ceramica_1", "impacto_ceramica_2", "impacto_ceramica_3" }, 0.5f),
            (Sfx.Golpe, new[] { "golpe" }, 0.6f),
            (Sfx.DanoJugador, new[] { "dano_jugador" }, 0.9f),
            (Sfx.MuerteEsqueleto, new[] { "muerte_esqueleto" }, 0.8f),
            (Sfx.RomperHuacal, new[] { "romper_huacal" }, 0.85f),
            (Sfx.RomperHorno, new[] { "romper_horno" }, 0.9f),
            (Sfx.RecogerGuacamaya, new[] { "recoger_guacamaya" }, 0.8f),
            (Sfx.Moneda, new[] { "moneda" }, 0.6f),
            (Sfx.RecogerCaguama, new[] { "recoger_caguama" }, 0.8f),
            (Sfx.BeberCaguama, new[] { "beber_caguama" }, 0.9f),
            (Sfx.MenuMover, new[] { "menu_mover" }, 0.5f),
            (Sfx.MenuAceptar, new[] { "menu_aceptar" }, 0.7f),
            (Sfx.Pausa, new[] { "pausa" }, 0.7f),
            (Sfx.JingleGameOver, new[] { "jingle_game_over" }, 0.85f),
            (Sfx.JingleVictoria, new[] { "jingle_victoria" }, 0.85f),
            (Sfx.JingleMision, new[] { "jingle_mision" }, 0.75f),
            (Sfx.Disparo, new[] { "disparo" }, 0.5f),
            (Sfx.Escopetazo, new[] { "escopetazo" }, 0.6f),
            (Sfx.Machetazo, new[] { "machetazo" }, 0.55f),
            (Sfx.RugidoAlebrije, new[] { "rugido_alebrije" }, 0.95f),
        };
        b.efectos = efectos.Select(e => new BibliotecaAudio.EntradaSfx
        {
            id = e.id,
            volumen = e.vol,
            clips = e.archivos.Select(a => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/SFX/{a}.wav")).Where(c => c != null).ToArray(),
        }).ToArray();

        // Música: se toma lo que haya en Assets/Audio/Music (menu, intro, nivel1 en .ogg/.mp3/.wav)
        (Musica id, string nombre, float vol)[] pistas = { (Musica.Menu, "menu", 0.55f), (Musica.Intro, "intro", 0.55f), (Musica.Nivel1, "nivel1", 0.5f) };
        b.musicas = pistas.Select(m => new BibliotecaAudio.EntradaMusica { id = m.id, volumen = m.vol, clip = BuscarMusica(m.nombre) })
                          .Where(m => m.clip != null).ToArray();

        AssetDatabase.DeleteAsset(ruta);
        AssetDatabase.CreateAsset(b, ruta);
    }

    static AudioClip BuscarMusica(string nombre)
    {
        foreach (string ext in new[] { ".ogg", ".mp3", ".wav" })
        {
            string ruta = "Assets/Audio/Music/" + nombre + ext;
            if (!File.Exists(ruta)) continue;
            var imp = (AudioImporter)AssetImporter.GetAtPath(ruta);
            var ajustes = imp.defaultSampleSettings;
            ajustes.loadType = AudioClipLoadType.Streaming;
            ajustes.compressionFormat = AudioCompressionFormat.Vorbis;
            ajustes.quality = 0.7f;
            imp.defaultSampleSettings = ajustes;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<AudioClip>(ruta);
        }
        return null;
    }

    // ================================================================== Prefabs
    static Prefabs CrearPrefabs()
    {
        var p = new Prefabs();

        // Efectos
        var chispa = Efecto("Chispa_Recoger", Cuadros("Items/recoger_chispa", 3), 14f, false, 0.6f);
        var restosMaceta = Efecto("Restos_Maceta", Cuadros("Obstacles/maceta_rota", 7), 18f, false, 1f);
        var restosHuacal = Efecto("Restos_Huacal", Cuadros("Obstacles/huacal", 5).Skip(2).ToArray(), 12f, true, 1f);
        var restosHorno = Efecto("Restos_Horno", Cuadros("Obstacles/horno", 4).Skip(2).ToArray(), 8f, true, 0.9f);

        // Recogibles
        p.guacamaya = Recogible("Guacamaya", TipoRecogible.Guacamaya, 35, Cuadros("Items/guacamaya", 4), 0.55f, chispa);
        p.caguama = Recogible("Caguama", TipoRecogible.Caguama, 1, new[] { Spr("Items/caguama_pickup.png") }, 0.22f, chispa);
        p.moneda = Recogible("Moneda", TipoRecogible.Moneda, 1, Cuadros("Items/moneda", 9), 0.5f, chispa);

        // Huacales y hornos (rompibles, sólidos: se puede uno parar encima)
        Sprite[] huacal = Cuadros("Obstacles/huacal", 2);
        p.huacalMonedas = Rompible("Huacal_Monedas", huacal, 30, 1f, restosHuacal, Sfx.RomperHuacal, new[] { p.moneda, p.moneda, p.moneda }, 0.85f, 0.9f);
        p.huacalGuacamaya = Rompible("Huacal_Guacamaya", huacal, 30, 1f, restosHuacal, Sfx.RomperHuacal, new[] { p.guacamaya }, 0.85f, 0.9f);
        p.huacalCaguama = Rompible("Huacal_Caguama", huacal, 30, 1f, restosHuacal, Sfx.RomperHuacal, new[] { p.caguama }, 0.85f, 0.9f);
        Sprite[] horno = Cuadros("Obstacles/horno", 2);
        p.hornoCaguama = Rompible("Horno_Caguama", horno, 60, 0.9f, restosHorno, Sfx.RomperHorno, new[] { p.moneda, p.caguama, p.moneda }, 0.8f, 0.55f);
        p.hornoMonedas = Rompible("Horno_Monedas", horno, 60, 0.9f, restosHorno, Sfx.RomperHorno, new[] { p.moneda, p.moneda, p.moneda, p.moneda }, 0.8f, 0.55f);

        p.escombros = Escombros();
        p.maceta = Maceta(restosMaceta);

        // El minero esqueleto deja polvo de barro al morir
        var minero = PrefabUtility.LoadPrefabContents(RutaPrefabMinero);
        Asignar(minero.GetComponent<EnemigoBasico>(), "efectoMuerte", restosMaceta);
        PrefabUtility.SaveAsPrefabAsset(minero, RutaPrefabMinero);
        PrefabUtility.UnloadPrefabContents(minero);

        CrearPrefabEsqueleto();

        ConfigurarCapaBalasEnemigas();
        BalaEnemiga bala = CrearBala("Bala_Bandido", 2f);
        BalaEnemiga perdigon = CrearBala("Perdigon_Bandido", 0.45f);
        p.revolver = CrearBandido("revolver", IABandido.Arma.Revolver, 30, 10, 6f, 0.45f, 1.6f, 1, 0f, 11f, bala, new Vector2(1.3f, 1.67f));
        p.rifle = CrearBandido("rifle", IABandido.Arma.Rifle, 30, 15, 8.5f, 0.7f, 2.4f, 1, 0f, 16f, bala, new Vector2(1.4f, 1.68f));
        p.escopeta = CrearBandido("escopeta", IABandido.Arma.Escopeta, 35, 8, 4f, 0.5f, 2f, 3, 12f, 10f, perdigon, new Vector2(1.2f, 1.66f));
        p.machete = CrearBandido("machete", IABandido.Arma.Machete, 40, 15, 1.2f, 0.35f, 1.1f, 0, 0f, 0f, null, Vector2.zero);
        p.alebrije = CrearPrefabAlebrije();
        return p;
    }

    /// <summary>
    /// Minero_Esqueleto: el dummy del Sprint 1 con IA (patrulla, persigue y embiste). El dummy se queda quieto
    /// para la escena Sandbox y sus pruebas; el Nivel 1 usa este.
    /// </summary>
    static void CrearPrefabEsqueleto()
    {
        var go = PrefabUtility.LoadPrefabContents(RutaPrefabMinero);
        go.name = "Minero_Esqueleto";

        // El sprite pasa a un hijo animado (caminar, golpe con el pico, muerte); el collider se queda en la raíz
        var srRaiz = go.GetComponent<SpriteRenderer>();
        var visual = new GameObject("Visual") { layer = go.layer };
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Enemies/esqueleto_caminar_00.png");
        sr.sortingOrder = srRaiz.sortingOrder;
        Object.DestroyImmediate(srRaiz);
        Asignar(go.GetComponent<ParpadeoDano>(), "sprite", sr);
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Esqueleto/Esqueleto.controller");

        // Primero se ve cómo se desarma; la IA lo desvanece y lo destruye después
        var soSalud = new SerializedObject(go.GetComponent<Health>());
        soSalud.FindProperty("destruirAlMorir").boolValue = false;
        soSalud.ApplyModifiedPropertiesWithoutUndo();

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 5f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        go.GetComponent<Collider2D>().sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/SinFriccion.physicsMaterial2D");

        var ia = go.AddComponent<IAEsqueleto>();
        var so = new SerializedObject(ia);
        so.FindProperty("capaSuelo").FindPropertyRelative("m_Bits").intValue = 1 << CapaGround;
        so.FindProperty("visual").objectReferenceValue = visual.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(go, RutaPrefabEsqueleto);
        PrefabUtility.UnloadPrefabContents(go);

        // Los esqueletos no se estorban entre sí al patrullar zonas cercanas
        Physics2D.IgnoreLayerCollision(CapaEnemy, CapaEnemy, true);
    }

    static Sprite[] Cuadros(string prefijo, int n) =>
        Enumerable.Range(0, n).Select(i => Spr($"{prefijo}_{i:00}.png")).ToArray();

    static SpriteAnimadoTemporal Efecto(string nombre, Sprite[] cuadros, float fps, bool mantener, float escala)
    {
        var go = new GameObject(nombre);
        go.transform.localScale = Vector3.one * escala;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = cuadros[0];
        sr.sortingOrder = mantener ? 4 : 25;
        var fx = go.AddComponent<SpriteAnimadoTemporal>();
        var so = new SerializedObject(fx);
        ArregloObjetos(so.FindProperty("cuadros"), cuadros);
        so.FindProperty("cuadrosPorSegundo").floatValue = fps;
        so.FindProperty("mantenerUltimo").boolValue = mantener;
        so.FindProperty("segundosUltimo").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, CarpetaEfectos + nombre + ".prefab").GetComponent<SpriteAnimadoTemporal>();
    }

    static GameObject Recogible(string nombre, TipoRecogible tipo, int cantidad, Sprite[] cuadros, float escala, SpriteAnimadoTemporal efecto)
    {
        var go = new GameObject("Recogible_" + nombre);
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * escala;
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = cuadros[0];
        sr.sortingOrder = 15;

        var r = go.AddComponent<Recogible>();
        var so = new SerializedObject(r);
        so.FindProperty("tipo").enumValueIndex = (int)tipo;
        so.FindProperty("cantidad").intValue = cantidad;
        so.FindProperty("sprite").objectReferenceValue = sr;
        ArregloObjetos(so.FindProperty("cuadros"), cuadros);
        so.FindProperty("efectoRecoger").objectReferenceValue = efecto;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, CarpetaRecogibles + nombre + ".prefab");
    }

    static GameObject Rompible(string nombre, Sprite[] estados, int vida, float escala, SpriteAnimadoTemporal restos, Sfx sonido,
                               GameObject[] botin, float anchoCol, float altoCol)
    {
        var go = new GameObject(nombre) { layer = CapaGround };   // Ground: se puede pisar y los proyectiles lo golpean
        go.transform.localScale = Vector3.one * escala;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = estados[0];
        sr.sortingOrder = 3;

        Vector2 tam = estados[0].bounds.size;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(tam.x * anchoCol, tam.y * altoCol);
        col.offset = new Vector2(0f, tam.y * altoCol * 0.5f);

        var salud = go.AddComponent<Health>();
        var soSalud = new SerializedObject(salud);
        soSalud.FindProperty("vidaMaxima").intValue = vida;
        soSalud.FindProperty("destruirAlMorir").boolValue = true;
        soSalud.ApplyModifiedPropertiesWithoutUndo();
        go.AddComponent<ParpadeoDano>();

        var d = go.AddComponent<Destructible>();
        var so = new SerializedObject(d);
        so.FindProperty("sprite").objectReferenceValue = sr;
        ArregloObjetos(so.FindProperty("estados"), estados);
        so.FindProperty("prefabRestos").objectReferenceValue = restos;
        so.FindProperty("sonidoRomper").enumValueIndex = (int)sonido;
        so.FindProperty("puntos").intValue = sonido == Sfx.RomperHorno ? 100 : 50;
        ArregloObjetos(so.FindProperty("botin"), botin.Select(b => (Object)b.GetComponent<Recogible>()).ToArray());
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, CarpetaObstaculos + nombre + ".prefab");
    }

    static GameObject Escombros()
    {
        var go = new GameObject("Escombros_Ardientes");
        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one * 0.85f;
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Obstacles/horno_03.png");
        sr.sortingOrder = 3;

        Vector2 tam = sr.sprite.bounds.size * 0.85f;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(tam.x * 0.7f, tam.y * 0.3f);
        col.offset = new Vector2(0f, tam.y * 0.15f);

        var dano = go.AddComponent<DanoPorContacto>();
        var so = new SerializedObject(dano);
        so.FindProperty("dano").intValue = 15;
        so.FindProperty("empuje").floatValue = 9f;
        so.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        Asignar(go.AddComponent<ParpadeoBrasas>(), "sprite", sr);
        return Guardar(go, CarpetaObstaculos + "Escombros_Ardientes.prefab");
    }

    static GameObject Maceta(SpriteAnimadoTemporal restos)
    {
        var go = new GameObject("Maceta_Que_Cae");
        var zona = go.AddComponent<BoxCollider2D>();          // Zona de activación (el jugador llega por la izquierda)
        zona.isTrigger = true;
        zona.offset = new Vector2(-1.2f, YSuelo + 2.5f);
        zona.size = new Vector2(3.2f, 5f);

        var maceta = new GameObject("Maceta");
        maceta.transform.SetParent(go.transform, false);
        maceta.transform.localPosition = new Vector3(0f, 5.3f, 0f);
        var sr = maceta.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Obstacles/maceta.png");
        sr.sortingOrder = 12;
        var col = maceta.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.32f;
        var dano = maceta.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 15;
        soD.FindProperty("empuje").floatValue = 5f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var m = go.AddComponent<MacetaQueCae>();
        var so = new SerializedObject(m);
        so.FindProperty("maceta").objectReferenceValue = maceta.transform;
        so.FindProperty("ySuelo").floatValue = YSuelo;
        so.FindProperty("prefabRota").objectReferenceValue = restos;
        so.FindProperty("capaJugador").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, CarpetaObstaculos + "Maceta_Que_Cae.prefab");
    }

    // ------------------------------------------------------------------ Bandidos y jefe
    /// <summary>Capa de las balas enemigas: solo chocan con Mateo y con el escenario.</summary>
    static void ConfigurarCapaBalasEnemigas()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tagManager.FindProperty("layers").GetArrayElementAtIndex(CapaProyectilEnemigo).stringValue = "EnemyProjectile";
        tagManager.ApplyModifiedPropertiesWithoutUndo();
        for (int i = 0; i < 32; i++)
            Physics2D.IgnoreLayerCollision(CapaProyectilEnemigo, i, i != CapaPlayer && i != CapaGround);
    }

    static BalaEnemiga CrearBala(string nombre, float tiempoVida)
    {
        var go = new GameObject(nombre) { layer = CapaProyectilEnemigo };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Projectiles/bala_bandido.png");
        sr.sortingOrder = 20;
        go.transform.localScale = Vector3.one * 2.5f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.04f;
        col.offset = new Vector2(0.06f, 0f);
        var bala = go.AddComponent<BalaEnemiga>();
        var so = new SerializedObject(bala);
        so.FindProperty("tiempoVida").floatValue = tiempoVida;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, "Assets/Prefabs/Projectiles/" + nombre + ".prefab").GetComponent<BalaEnemiga>();
    }

    static IABandido CrearBandido(string arma, IABandido.Arma tipo, int vida, int dano, float distancia, float preparacion,
                                  float enfriamiento, int balas, float dispersion, float velocidadBala, BalaEnemiga bala, Vector2 boca)
    {
        string nombre = "Bandido_" + char.ToUpper(arma[0]) + arma.Substring(1);
        var go = new GameObject(nombre) { layer = CapaEnemy };

        var visual = new GameObject("Visual") { layer = CapaEnemy };
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr($"Enemies/bandido_{arma}_ataque_00.png");
        sr.sortingOrder = 5;
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>($"Assets/Art/Animations/Bandidos/{nombre}.controller");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.8f, 2.1f);
        col.offset = new Vector2(0f, 1.05f);
        col.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/SinFriccion.physicsMaterial2D");

        var salud = go.AddComponent<Health>();
        var soS = new SerializedObject(salud);
        soS.FindProperty("vidaMaxima").intValue = vida;
        soS.FindProperty("destruirAlMorir").boolValue = false;   // Primero se ve la animación de muerte
        soS.ApplyModifiedPropertiesWithoutUndo();
        Asignar(go.AddComponent<ParpadeoDano>(), "sprite", sr);
        var basico = go.AddComponent<EnemigoBasico>();
        var soB = new SerializedObject(basico);
        soB.FindProperty("sonidoMuerte").enumValueIndex = (int)Sfx.Golpe;
        soB.FindProperty("puntos").intValue = 150;
        soB.ApplyModifiedPropertiesWithoutUndo();

        var ia = go.AddComponent<IABandido>();
        var so = new SerializedObject(ia);
        so.FindProperty("arma").enumValueIndex = (int)tipo;
        so.FindProperty("dano").intValue = dano;
        so.FindProperty("distanciaPreferida").floatValue = distancia;
        so.FindProperty("preparacion").floatValue = preparacion;
        so.FindProperty("enfriamiento").floatValue = enfriamiento;
        so.FindProperty("balasPorDisparo").intValue = balas;
        so.FindProperty("dispersionGrados").floatValue = dispersion;
        so.FindProperty("velocidadBala").floatValue = velocidadBala;
        so.FindProperty("prefabBala").objectReferenceValue = bala;
        so.FindProperty("puntoDisparo").vector2Value = boca;
        so.FindProperty("velocidad").floatValue = tipo == IABandido.Arma.Machete ? 3.8f : 3.2f;
        so.FindProperty("capaSuelo").FindPropertyRelative("m_Bits").intValue = 1 << CapaGround;
        so.FindProperty("animator").objectReferenceValue = anim;
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, $"Assets/Prefabs/Enemies/{nombre}.prefab").GetComponent<IABandido>();
    }

    static JefeAlebrije CrearPrefabAlebrije()
    {
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        var go = new GameObject("Alebrije") { layer = CapaEnemy };

        var visual = new GameObject("Visual") { layer = CapaEnemy };
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Bosses/alebrije_caminar_00.png");
        sr.sortingOrder = 6;
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Alebrije/Alebrije.controller");

        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.mass = 50f;                                   // Mateo no lo puede empujar
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4.2f, 3.2f);              // El cuerpo; alas y cola no cuentan
        col.offset = new Vector2(-0.3f, 1.6f);
        col.sharedMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>("Assets/Settings/SinFriccion.physicsMaterial2D");

        var salud = go.AddComponent<Health>();
        var soS = new SerializedObject(salud);
        soS.FindProperty("vidaMaxima").intValue = 400;
        soS.FindProperty("destruirAlMorir").boolValue = false;
        soS.ApplyModifiedPropertiesWithoutUndo();
        Asignar(go.AddComponent<ParpadeoDano>(), "sprite", sr);
        var basico = go.AddComponent<EnemigoBasico>();
        var soB = new SerializedObject(basico);
        soB.FindProperty("sonidoMuerte").enumValueIndex = (int)Sfx.RugidoAlebrije;
        soB.FindProperty("puntos").intValue = 2000;
        soB.ApplyModifiedPropertiesWithoutUndo();

        var dano = go.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 15;
        soD.FindProperty("empuje").floatValue = 9f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var jefe = go.AddComponent<JefeAlebrije>();
        var so = new SerializedObject(jefe);
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("danoContacto").objectReferenceValue = dano;
        so.FindProperty("capaSuelo").FindPropertyRelative("m_Bits").intValue = 1 << CapaGround;
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, RutaPrefabAlebrije).GetComponent<JefeAlebrije>();
    }

    static T Cargar<T>(string ruta) where T : Component => AssetDatabase.LoadAssetAtPath<GameObject>(ruta).GetComponent<T>();

    static IABandido PonerBandido(IABandido prefab, float x, int mirada, Transform padre)
    {
        var b = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, padre);
        b.transform.position = new Vector3(x, YSuelo, 0f);
        Vector3 e = b.transform.localScale;
        b.transform.localScale = new Vector3(Mathf.Abs(e.x) * mirada, e.y, e.z);
        return b.GetComponent<IABandido>();
    }

    static Emboscada CrearEmboscada(Prefabs p, float x)
    {
        var go = new GameObject("Emboscada");
        go.transform.position = new Vector3(x, YSuelo + 2f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 8f);
        var e = go.AddComponent<Emboscada>();

        // Oleada 1: machetes por la derecha y un pistolero por la izquierda. Oleada 2: entran los de rifle y escopeta
        (IABandido prefab, bool derecha, float retraso)[][] oleadas =
        {
            new[] { (p.machete, true, 0f), (p.revolver, false, 0.8f), (p.machete, true, 1.2f) },
            new[] { (p.escopeta, true, 0f), (p.rifle, false, 1f), (p.revolver, true, 1f), (p.machete, false, 0.8f) },
        };
        var so = new SerializedObject(e);
        var lista = so.FindProperty("oleadas");
        lista.arraySize = oleadas.Length;
        for (int i = 0; i < oleadas.Length; i++)
        {
            var bandidos = lista.GetArrayElementAtIndex(i).FindPropertyRelative("bandidos");
            bandidos.arraySize = oleadas[i].Length;
            for (int j = 0; j < oleadas[i].Length; j++)
            {
                var entrada = bandidos.GetArrayElementAtIndex(j);
                entrada.FindPropertyRelative("prefab").objectReferenceValue = oleadas[i][j].prefab;
                entrada.FindPropertyRelative("porLaDerecha").boolValue = oleadas[i][j].derecha;
                entrada.FindPropertyRelative("retraso").floatValue = oleadas[i][j].retraso;
            }
        }
        so.FindProperty("ySuelo").floatValue = YSuelo;
        so.ApplyModifiedPropertiesWithoutUndo();
        return e;
    }


    // ================================================================== Nivel 1
    static void CrearEscenaNivel1(Prefabs p)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var prefabAlfarero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabAlfarero);
        var prefabMinero = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabEsqueleto);
        GameObject jugador = CrearBaseNivel(prefabAlfarero, out _, out float derecha);

        // Al guardar prefabs y cambiar capas Unity puede invalidar las referencias en memoria: se recargan de disco
        p.revolver = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Revolver.prefab");
        p.rifle = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Rifle.prefab");
        p.escopeta = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Escopeta.prefab");
        p.machete = Cargar<IABandido>("Assets/Prefabs/Enemies/Bandido_Machete.prefab");
        p.alebrije = Cargar<JefeAlebrije>(RutaPrefabAlebrije);

        var obstaculos = new GameObject("Obstaculos").transform;
        var enemigos = new GameObject("Enemigos").transform;
        var recogibles = new GameObject("Recogibles").transform;
        float altoHuacal = Spr("Obstacles/huacal_00.png").bounds.size.y * 0.9f;   // para apilar

        // Recorrido: Dolores -> calle del horno -> plaza -> callejón -> parroquia
        Poner(p.caguama, -1f, YSuelo + 0.7f, recogibles);
        Poner(p.huacalMonedas, 3f, YSuelo, obstaculos);
        Minero(prefabMinero, 7f, enemigos);
        Poner(p.maceta, 10.5f, 0f, obstaculos);
        PonerBandido(p.machete, 12f, -1, enemigos);
        Poner(p.hornoCaguama, 14f, YSuelo, obstaculos);
        PonerBandido(p.revolver, 16.5f, -1, enemigos);
        Poner(p.escombros, 18.5f, YSuelo, obstaculos);
        Poner(p.huacalMonedas, 23f, YSuelo, obstaculos);
        Poner(p.huacalGuacamaya, 23f, YSuelo + altoHuacal, obstaculos);
        Minero(prefabMinero, 27f, enemigos);
        Poner(p.maceta, 31f, 0f, obstaculos);
        Poner(p.hornoMonedas, 35f, YSuelo, obstaculos);
        PonerBandido(p.rifle, 37f, -1, enemigos);                // Se cubre detrás del horno
        Minero(prefabMinero, 39f, enemigos);
        Minero(prefabMinero, 42f, enemigos);
        Poner(p.escombros, 46f, YSuelo, obstaculos);
        Poner(p.guacamaya, 48.5f, YSuelo + 0.7f, recogibles);
        Poner(p.huacalCaguama, 51f, YSuelo, obstaculos);
        Poner(p.maceta, 54.5f, 0f, obstaculos);
        Poner(p.maceta, 57.5f, 0f, obstaculos);
        Poner(p.hornoCaguama, 60.5f, YSuelo, obstaculos);
        Poner(p.guacamaya, 62.5f, YSuelo + 0.7f, recogibles);   // Antes del jefe

        // Evento especial: emboscada de los bandidos del Charro en la plaza
        CrearEmboscada(p, 45f);

        // Jefe: la plaza de la parroquia queda libre como arena; el alebrije sale de entre las torres
        float puerta = Mathf.Min(74.5f, derecha - 5f);
        var alebrije = (GameObject)PrefabUtility.InstantiatePrefab(p.alebrije.gameObject, enemigos);
        alebrije.transform.position = new Vector3(puerta + 2f, YSuelo, 0f);

        // Interfaz
        CrearEventSystem();
        var hud = CrearHud(jugador, out BannerMision banner);
        BarraJefe barraJefe = CrearBarraJefe(hud.transform);

        var encuentro = new GameObject("Encuentro_Alebrije");
        encuentro.transform.position = new Vector3(derecha - 19f, YSuelo + 2f, 0f);
        var colEnc = encuentro.AddComponent<BoxCollider2D>();
        colEnc.isTrigger = true;
        colEnc.size = new Vector2(1f, 8f);
        var enc = encuentro.AddComponent<EncuentroAlebrije>();
        var soE = new SerializedObject(enc);
        soE.FindProperty("jefe").objectReferenceValue = alebrije.GetComponent<JefeAlebrije>();
        soE.FindProperty("barraJefe").objectReferenceValue = barraJefe;
        soE.FindProperty("puntoTorres").vector2Value = new Vector2(puerta, 2.5f);
        soE.FindProperty("xAterrizaje").floatValue = puerta + 2f;
        soE.FindProperty("ySuelo").floatValue = YSuelo;
        soE.ApplyModifiedPropertiesWithoutUndo();
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
        so.FindProperty("xInicioRecorrido").floatValue = jugador.transform.position.x;
        so.FindProperty("xFinRecorrido").floatValue = encuentro.transform.position.x;   // Donde arranca el jefe
        so.ApplyModifiedPropertiesWithoutUndo();
        Asignar(hud, "gestor", gestor);

        EditorSceneManager.SaveScene(escena, RutaNivel1);
    }

    static GameObject Poner(GameObject prefab, float x, float y, Transform padre)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, padre);
        go.transform.position = new Vector3(x, y, 0f);
        return go;
    }

    static void Minero(GameObject prefab, float x, Transform padre)
    {
        var d = Poner(prefab, x, YSuelo, padre);
        Vector3 e = d.transform.localScale;
        d.transform.localScale = new Vector3(-e.x, e.y, e.z);   // Mira hacia Mateo
    }

    // ------------------------------------------------------------------ Barra del jefe
    static BarraJefe CrearBarraJefe(Transform canvas)
    {
        var abajo = new Vector2(0.5f, 0f);
        var grupo = Hijo(canvas, "BarraJefe", abajo, abajo, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(900f, 130f));
        Text nombre = Texto(grupo, "Nombre", "ALEBRIJE GIGANTE", 30, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, 45f), new Vector2(900f, 50f));
        Image marco = Imagen(grupo, "Marco", Spr("UI/hud_barra_marco.png"), new Vector2(0f, -15f), new Vector2(860f, 80f));
        Image retardo = Imagen(marco.transform, "Retardo", Spr("UI/hud_barra_vida.png"), Vector2.zero, new Vector2(860f, 80f));
        retardo.color = new Color(1f, 0.92f, 0.75f, 0.9f);
        Rellenar(retardo);
        Image barra = Imagen(marco.transform, "Vida", Spr("UI/hud_barra_vida.png"), Vector2.zero, new Vector2(860f, 80f));
        barra.color = new Color(0.55f, 0.75f, 1f);                     // Azul cobalto, como el alebrije
        Rellenar(barra);

        var b = grupo.gameObject.AddComponent<BarraJefe>();
        var so = new SerializedObject(b);
        so.FindProperty("nombre").objectReferenceValue = nombre;
        so.FindProperty("barra").objectReferenceValue = barra;
        so.FindProperty("barraRetardo").objectReferenceValue = retardo;
        so.ApplyModifiedPropertiesWithoutUndo();
        grupo.gameObject.SetActive(false);
        return b;
    }


    // ------------------------------------------------------------------ HUD
    static HudJuego CrearHud(GameObject jugador, out BannerMision banner)
    {
        Canvas canvas = CrearCanvas("HUD", 10);
        Transform c = canvas.transform;
        var arribaIzq = new Vector2(0f, 1f);
        var arribaDer = new Vector2(1f, 1f);

        // Vida: jarrito + barra
        Image jarrito = Imagen(c, "Jarrito", Spr("UI/hud_jarrito_lleno.png"), new Vector2(95f, -100f), new Vector2(140f, 150f), arribaIzq);
        Image marco = Imagen(c, "BarraMarco", Spr("UI/hud_barra_marco.png"), new Vector2(380f, -100f), new Vector2(470f, 100f), arribaIzq);
        Image retardo = Imagen(marco.transform, "BarraRetardo", Spr("UI/hud_barra_vida.png"), Vector2.zero, new Vector2(470f, 100f));
        retardo.color = new Color(1f, 0.92f, 0.75f, 0.9f);
        Rellenar(retardo);
        Image vida = Imagen(marco.transform, "BarraVida", Spr("UI/hud_barra_vida.png"), Vector2.zero, new Vector2(470f, 100f));
        Rellenar(vida);
        jarrito.transform.SetAsLastSibling();

        // Caguamas: 3 corcholatas + temporizador dorado
        var iconos = new Image[3];
        for (int i = 0; i < 3; i++)
            iconos[i] = Imagen(c, "Caguama_" + i, Spr("UI/hud_caguama.png"), new Vector2(70f + i * 72f, -215f), new Vector2(64f, 70f), arribaIzq);
        var grupo = Hijo(c, "TemporizadorCaguama", arribaIzq, arribaIzq, new Vector2(0f, 0.5f), new Vector2(285f, -215f), new Vector2(420f, 70f));
        Image marcoCag = Imagen(grupo, "Marco", Spr("UI/hud_barra_marco.png"), new Vector2(150f, 0f), new Vector2(300f, 64f));
        Image barraCag = Imagen(marcoCag.transform, "Barra", Spr("UI/hud_barra_dorada.png"), Vector2.zero, new Vector2(300f, 64f));
        Rellenar(barraCag);
        Text textoCag = Texto(grupo, "Segundos", "5s", 26, Dorado, TextAnchor.MiddleLeft, new Vector2(370f, 0f), new Vector2(120f, 50f));
        Texto(grupo, "Etiqueta", "INVENCIBLE", 16, Crema, TextAnchor.MiddleCenter, new Vector2(150f, 48f), new Vector2(300f, 30f));

        // Munición (azulejo) y monedas
        Imagen(c, "MarcoMunicion", Spr("UI/hud_marco_azulejo.png"), new Vector2(-150f, -95f), new Vector2(235f, 158f), arribaDer);
        Text municion = Texto(c, "Municion", "∞", 56, Dorado, TextAnchor.MiddleCenter, new Vector2(-115f, -95f), new Vector2(120f, 80f), arribaDer);
        Imagen(c, "IconoMoneda", Spr("UI/hud_moneda.png"), new Vector2(-235f, -220f), new Vector2(60f, 60f), arribaDer);
        Text monedas = Texto(c, "Monedas", "x00", 34, Crema, TextAnchor.MiddleLeft, new Vector2(-120f, -220f), new Vector2(170f, 60f), arribaDer);

        // Marcador al centro: puntaje, cronómetro y objetivo con barra de avance
        var arriba = new Vector2(0.5f, 1f);
        Text puntaje = Texto(c, "Puntaje", "PUNTOS 000000", 30, Crema, TextAnchor.MiddleCenter, new Vector2(0f, -48f), new Vector2(620f, 46f), arriba);
        Text tiempo = Texto(c, "Tiempo", "00:00", 26, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, -92f), new Vector2(300f, 40f), arriba);
        Text objetivo = Texto(c, "Objetivo", "", 18, Crema, TextAnchor.MiddleCenter, new Vector2(0f, -132f), new Vector2(620f, 34f), arriba);
        var grupoObjetivo = Hijo(c, "BarraObjetivo", arriba, arriba, new Vector2(0.5f, 0.5f), new Vector2(0f, -166f), new Vector2(360f, 30f));
        Imagen(grupoObjetivo, "Marco", Spr("UI/hud_barra_marco.png"), Vector2.zero, new Vector2(360f, 30f)).preserveAspect = false;
        Image barraObj = Imagen(grupoObjetivo, "Avance", Spr("UI/hud_barra_dorada.png"), Vector2.zero, new Vector2(360f, 30f));
        barraObj.preserveAspect = false;
        Rellenar(barraObj);
        barraObj.fillAmount = 0f;

        Texto(c, "Ayuda", "ESPACIO LANZAR   ARRIBA SALTAR   E CAGUAMA   ESC PAUSA", 16, new Color(1f, 1f, 1f, 0.75f), TextAnchor.LowerRight,
              new Vector2(-500f, 30f), new Vector2(960f, 40f), new Vector2(1f, 0f));

        var hud = canvas.gameObject.AddComponent<HudJuego>();
        var so = new SerializedObject(hud);
        so.FindProperty("salud").objectReferenceValue = jugador.GetComponent<Health>();
        so.FindProperty("inventario").objectReferenceValue = jugador.GetComponent<InventarioJugador>();
        so.FindProperty("jarrito").objectReferenceValue = jarrito;
        so.FindProperty("jarritoLleno").objectReferenceValue = Spr("UI/hud_jarrito_lleno.png");
        so.FindProperty("jarritoMedio").objectReferenceValue = Spr("UI/hud_jarrito_medio.png");
        so.FindProperty("jarritoRoto").objectReferenceValue = Spr("UI/hud_jarrito_roto.png");
        so.FindProperty("barraVida").objectReferenceValue = vida;
        so.FindProperty("barraVidaRetardo").objectReferenceValue = retardo;
        ArregloObjetos(so.FindProperty("iconosCaguama"), iconos);
        so.FindProperty("grupoTemporizador").objectReferenceValue = grupo.gameObject;
        so.FindProperty("barraCaguama").objectReferenceValue = barraCag;
        so.FindProperty("textoCaguama").objectReferenceValue = textoCag;
        so.FindProperty("textoMonedas").objectReferenceValue = monedas;
        so.FindProperty("textoMunicion").objectReferenceValue = municion;
        so.FindProperty("textoPuntaje").objectReferenceValue = puntaje;
        so.FindProperty("textoTiempo").objectReferenceValue = tiempo;
        so.FindProperty("textoObjetivo").objectReferenceValue = objetivo;
        so.FindProperty("grupoBarraObjetivo").objectReferenceValue = grupoObjetivo.gameObject;
        so.FindProperty("barraObjetivo").objectReferenceValue = barraObj;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Letrero de inicio de misión
        var rtBanner = Estirar(c, "BannerMision");
        Text l1 = Texto(rtBanner, "Linea1", "MISION 1", 64, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, 150f), new Vector2(1600f, 90f));
        Text l2 = Texto(rtBanner, "Linea2", "DOLORES HIDALGO", 46, Crema, TextAnchor.MiddleCenter, new Vector2(0f, 60f), new Vector2(1600f, 70f));
        Text l3 = Texto(rtBanner, "Linea3", "¡A LA CARGA!", 76, new Color(1f, 0.35f, 0.2f), TextAnchor.MiddleCenter, new Vector2(0f, -60f), new Vector2(1600f, 110f));
        banner = rtBanner.gameObject.AddComponent<BannerMision>();
        var soB = new SerializedObject(banner);
        soB.FindProperty("linea1").objectReferenceValue = l1;
        soB.FindProperty("linea2").objectReferenceValue = l2;
        soB.FindProperty("linea3").objectReferenceValue = l3;
        soB.ApplyModifiedPropertiesWithoutUndo();
        return hud;
    }

    static void Rellenar(Image img)
    {
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 1f;
    }

    // ------------------------------------------------------------------ Pausa
    static MenuPausa CrearMenuPausa()
    {
        Canvas canvas = CrearCanvas("MenuPausa", 30);
        var raiz = Estirar(canvas.transform, "Raiz");
        raiz.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        Panel principal = CrearPanel(raiz, "PanelPausa", Vector2.zero, new Vector2(640f, 640f));
        Texto(principal.transform, "Titulo", "PAUSA", 56, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, 230f), new Vector2(560f, 80f));
        Button continuar = Boton(principal.transform, "Continuar", "CONTINUAR", new Vector2(0f, 110f), new Vector2(480f, 80f));
        Button reiniciar = Boton(principal.transform, "Reiniciar", "REINICIAR", new Vector2(0f, 10f), new Vector2(480f, 80f));
        Button opciones = Boton(principal.transform, "Opciones", "OPCIONES", new Vector2(0f, -90f), new Vector2(480f, 80f));
        Button menu = Boton(principal.transform, "Menu", "MENU PRINCIPAL", new Vector2(0f, -190f), new Vector2(480f, 80f));
        Asignar(principal, "primerSeleccionado", continuar.gameObject);

        Panel panelOpciones = CrearPanelOpciones(raiz, out Button volver);

        var mp = raiz.gameObject.AddComponent<MenuPausa>();
        var so = new SerializedObject(mp);
        so.FindProperty("panelPrincipal").objectReferenceValue = principal;
        so.FindProperty("panelOpciones").objectReferenceValue = panelOpciones;
        so.FindProperty("botonContinuar").objectReferenceValue = continuar;
        so.FindProperty("botonReiniciar").objectReferenceValue = reiniciar;
        so.FindProperty("botonOpciones").objectReferenceValue = opciones;
        so.FindProperty("botonMenu").objectReferenceValue = menu;
        so.FindProperty("botonVolverOpciones").objectReferenceValue = volver;
        so.ApplyModifiedPropertiesWithoutUndo();
        return mp;
    }

    /// <summary>Volumen de música y efectos. Desde el menú principal (conBorrarPartida) también deja borrar la partida guardada.</summary>
    static Panel CrearPanelOpciones(Transform padre, out Button volver, bool conBorrarPartida = false)
    {
        // Con el botón de borrar, el panel crece hacia abajo y lo demás se recorre hacia arriba
        float sube = conBorrarPartida ? 45f : 0f;
        Panel panel = CrearPanel(padre, "PanelOpciones", Vector2.zero, new Vector2(800f, conBorrarPartida ? 620f : 560f));
        Texto(panel.transform, "Titulo", "OPCIONES", 48, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, 200f + sube), new Vector2(700f, 70f));
        Text tMusica = Texto(panel.transform, "TextoMusica", "MUSICA", 26, Crema, TextAnchor.MiddleCenter, new Vector2(0f, 100f + sube), new Vector2(700f, 50f));
        Slider sMusica = CrearSlider(panel.transform, "SliderMusica", new Vector2(0f, 45f + sube));
        Text tEfectos = Texto(panel.transform, "TextoEfectos", "EFECTOS", 26, Crema, TextAnchor.MiddleCenter, new Vector2(0f, -40f + sube), new Vector2(700f, 50f));
        Slider sEfectos = CrearSlider(panel.transform, "SliderEfectos", new Vector2(0f, -95f + sube));
        if (conBorrarPartida)
        {
            Button borrar = Boton(panel.transform, "BorrarPartida", "BORRAR PARTIDA", new Vector2(0f, -150f), new Vector2(620f, 76f));
            var bp = borrar.gameObject.AddComponent<BotonBorrarPartida>();
            Asignar(bp, "texto", borrar.GetComponentInChildren<Text>());
        }
        volver = Boton(panel.transform, "Volver", "VOLVER", new Vector2(0f, conBorrarPartida ? -245f : -195f), new Vector2(400f, 76f));
        Asignar(panel, "primerSeleccionado", sMusica.gameObject);

        var op = panel.gameObject.AddComponent<OpcionesAudio>();
        var so = new SerializedObject(op);
        so.FindProperty("sliderMusica").objectReferenceValue = sMusica;
        so.FindProperty("sliderEfectos").objectReferenceValue = sEfectos;
        so.FindProperty("textoMusica").objectReferenceValue = tMusica;
        so.FindProperty("textoEfectos").objectReferenceValue = tEfectos;
        so.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    // ------------------------------------------------------------------ Game over / Victoria
    static PantallaFinal CrearPantallaFinal(string nombre, Color colorTitulo, Color tinte, string textoReintentar, int orden, bool conRecord = false)
    {
        Canvas canvas = CrearCanvas(nombre, orden);
        var raiz = Estirar(canvas.transform, "Raiz");
        raiz.gameObject.AddComponent<Image>().color = tinte;

        Panel panel = CrearPanel(raiz, "Panel", Vector2.zero, new Vector2(1000f, 680f));
        Text titulo = Texto(panel.transform, "Titulo", "GAME OVER", 80, colorTitulo, TextAnchor.MiddleCenter, new Vector2(0f, 220f), new Vector2(900f, 110f));
        titulo.resizeTextForBestFit = true;      // Títulos largos ("¡MISION CUMPLIDA!") se encogen para caber en una línea
        titulo.resizeTextMinSize = 36;
        titulo.resizeTextMaxSize = 80;
        Text sub = Texto(panel.transform, "Subtitulo", "", 22, Crema, TextAnchor.MiddleCenter, new Vector2(0f, 115f), new Vector2(880f, 70f));
        // Alto para 6 renglones: en la victoria se agregan RECORD y MEJOR (tiempo)
        Text resumen = Texto(panel.transform, "Resumen", "", 26, Crema, TextAnchor.MiddleLeft, new Vector2(-40f, -38f), new Vector2(520f, 230f));
        Button reintentar = Boton(panel.transform, "Reintentar", textoReintentar, new Vector2(-235f, -235f), new Vector2(430f, 84f));
        Button menu = Boton(panel.transform, "Menu", "MENU PRINCIPAL", new Vector2(235f, -235f), new Vector2(430f, 84f));
        Asignar(panel, "primerSeleccionado", reintentar.gameObject);

        GameObject avisoRecord = null;
        if (conRecord)
        {
            Text aviso = Texto(panel.transform, "NuevoRecord", "¡NUEVO\nRECORD!", 30, Dorado, TextAnchor.MiddleCenter, new Vector2(340f, -10f), new Vector2(260f, 110f));
            aviso.gameObject.AddComponent<TextoParpadeante>();
            aviso.gameObject.SetActive(false);
            avisoRecord = aviso.gameObject;
        }

        var pf = raiz.gameObject.AddComponent<PantallaFinal>();
        var so = new SerializedObject(pf);
        so.FindProperty("panel").objectReferenceValue = panel;
        so.FindProperty("titulo").objectReferenceValue = titulo;
        so.FindProperty("subtitulo").objectReferenceValue = sub;
        so.FindProperty("resumen").objectReferenceValue = resumen;
        so.FindProperty("botonReintentar").objectReferenceValue = reintentar;
        so.FindProperty("botonMenu").objectReferenceValue = menu;
        so.FindProperty("avisoNuevoRecord").objectReferenceValue = avisoRecord;
        so.ApplyModifiedPropertiesWithoutUndo();
        return pf;
    }

    // ================================================================== Menú principal
    static void CrearEscenaMenu()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearCamara();
        CrearEventSystem();
        Canvas canvas = CrearCanvas("Menu", 0);
        Transform c = canvas.transform;

        // Portada: si existe UI/portada.png se usa; si no, el fondo nocturno de Dolores
        Sprite portada = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "UI/portada.png") ?? Spr("Environment/lvl1_dolores_noche.png");
        Image fondo = Imagen(c, "Fondo", portada, Vector2.zero, new Vector2(2040f, 1148f));
        fondo.raycastTarget = false;
        var oscuro = Estirar(c, "Oscurecer").gameObject.AddComponent<Image>();
        oscuro.color = new Color(0f, 0f, 0f, 0.35f);
        oscuro.raycastTarget = false;

        var titulo = Estirar(c, "Titulo");
        Texto(titulo, "Linea1", "EL ALFARERO", 104, Crema, TextAnchor.MiddleCenter, new Vector2(0f, 330f), new Vector2(1800f, 140f), null, CafeOscuro);
        Texto(titulo, "Linea2", "DE DOLORES", 72, AzulTalavera, TextAnchor.MiddleCenter, new Vector2(0f, 205f), new Vector2(1800f, 100f), null, Color.white);
        Imagen(titulo, "JarraIzq", Spr("Projectiles/talavera_piece_00.png"), new Vector2(-700f, 290f), new Vector2(120f, 120f));
        Imagen(titulo, "JarraDer", Spr("Projectiles/talavera_piece_09.png"), new Vector2(700f, 290f), new Vector2(120f, 120f));
        Text presiona = Texto(c, "Presiona", "PRESIONA CUALQUIER TECLA", 30, Crema, TextAnchor.MiddleCenter, new Vector2(0f, -360f), new Vector2(1400f, 60f));
        presiona.gameObject.AddComponent<TextoParpadeante>();
        Texto(c, "Creditos", "OMAR SALINAS  ·  NOE CHAVERO  ·  2026", 16, new Color(1f, 1f, 1f, 0.6f), TextAnchor.MiddleCenter,
              new Vector2(0f, 30f), new Vector2(1400f, 40f), new Vector2(0.5f, 0f));

        // Menú
        Panel panelMenu = CrearPanel(c, "PanelMenu", new Vector2(0f, -150f), new Vector2(560f, 440f));
        Button jugar = Boton(panelMenu.transform, "Jugar", "JUGAR", new Vector2(0f, 140f), new Vector2(440f, 78f));
        Button controles = Boton(panelMenu.transform, "Controles", "CONTROLES", new Vector2(0f, 48f), new Vector2(440f, 78f));
        Button opciones = Boton(panelMenu.transform, "Opciones", "OPCIONES", new Vector2(0f, -44f), new Vector2(440f, 78f));
        Button salir = Boton(panelMenu.transform, "Salir", "SALIR", new Vector2(0f, -136f), new Vector2(440f, 78f));
        Asignar(panelMenu, "primerSeleccionado", jugar.gameObject);

        // Controles
        Panel panelControles = CrearPanel(c, "PanelControles", new Vector2(0f, -140f), new Vector2(1180f, 600f));
        Texto(panelControles.transform, "Titulo", "CONTROLES", 44, Dorado, TextAnchor.MiddleCenter, new Vector2(0f, 235f), new Vector2(1000f, 70f));
        Texto(panelControles.transform, "Lista",
              "MOVER ............ A / D o FLECHAS\n" +
              "SALTAR ........... FLECHA ARRIBA o W\n" +
              "EVADIR ........... SHIFT\n" +
              "APUNTAR .......... MOUSE o FLECHAS\n" +
              "LANZAR CERAMICA .. ESPACIO o CLIC\n" +
              "BEBER CAGUAMA .... E o Q\n" +
              "PAUSA ............ ESC o P",
              24, Crema, TextAnchor.MiddleLeft, new Vector2(0f, 10f), new Vector2(1000f, 340f));
        Button volverControles = Boton(panelControles.transform, "Volver", "VOLVER", new Vector2(0f, -225f), new Vector2(400f, 76f));
        Asignar(panelControles, "primerSeleccionado", volverControles.gameObject);

        Panel panelOpciones = CrearPanelOpciones(c, out Button volverOpciones, true);
        ((RectTransform)panelOpciones.transform).anchoredPosition = new Vector2(0f, -170f);   // Más alto por BORRAR PARTIDA: sin tapar el título ni los créditos

        var mp = canvas.gameObject.AddComponent<MenuPrincipal>();
        var so = new SerializedObject(mp);
        so.FindProperty("grupoTitulo").objectReferenceValue = titulo.gameObject;
        so.FindProperty("textoPresiona").objectReferenceValue = presiona.gameObject;
        so.FindProperty("panelMenu").objectReferenceValue = panelMenu;
        so.FindProperty("panelControles").objectReferenceValue = panelControles;
        so.FindProperty("panelOpciones").objectReferenceValue = panelOpciones;
        so.FindProperty("fondo").objectReferenceValue = fondo.rectTransform;
        so.FindProperty("botonJugar").objectReferenceValue = jugar;
        so.FindProperty("botonControles").objectReferenceValue = controles;
        so.FindProperty("botonOpciones").objectReferenceValue = opciones;
        so.FindProperty("botonSalir").objectReferenceValue = salir;
        so.FindProperty("botonVolverControles").objectReferenceValue = volverControles;
        so.FindProperty("botonVolverOpciones").objectReferenceValue = volverOpciones;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, RutaMenu);
    }

    // ================================================================== Intro
    static void CrearEscenaIntro()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearCamara();
        CrearEventSystem();
        Canvas canvas = CrearCanvas("Intro", 0);
        Transform c = canvas.transform;
        Estirar(c, "Negro").gameObject.AddComponent<Image>().color = Color.black;

        Image imagen = Imagen(c, "Ilustracion", Spr("Intro/intro_01.png"), new Vector2(0f, 110f), new Vector2(1500f, 844f));
        imagen.preserveAspect = true;
        var grupo = imagen.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        var caja = Centro(c, "CajaTexto", new Vector2(0f, -400f), new Vector2(1640f, 230f));
        var imgCaja = caja.gameObject.AddComponent<Image>();
        imgCaja.sprite = sprPanel;
        imgCaja.type = Image.Type.Sliced;
        imgCaja.pixelsPerUnitMultiplier = 0.5f;
        Text texto = Texto(caja, "Texto", "", 26, Crema, TextAnchor.UpperLeft, new Vector2(0f, -5f), new Vector2(1520f, 170f));
        Texto(c, "Ayuda", "ENTER SIGUIENTE   ESC SALTAR", 16, new Color(1f, 1f, 1f, 0.6f), TextAnchor.LowerRight,
              new Vector2(-280f, 18f), new Vector2(520f, 30f), new Vector2(1f, 0f));

        (string img, string txt)[] vinetas =
        {
            ("intro_01", "Dolores Hidalgo, Guanajuato.\nMateo, alfarero de manos firmes, vivía en paz moldeando el barro junto a su esposa Carmen."),
            ("intro_02", "Hasta que una noche la luna se tiñó de rojo...\ny del inframundo emergió EL CHARRO NEGRO."),
            ("intro_03", "El Charro se llevó a Carmen a lo más hondo de los túneles de Guanajuato. A su paso despertaron las leyendas del Bajío."),
            ("intro_04", "Mateo tomó sus piezas de Talavera consagradas. Para rescatar a Carmen y traer de vuelta el amanecer... tendrá que pelear."),
        };

        var intro = canvas.gameObject.AddComponent<SecuenciaIntro>();
        var so = new SerializedObject(intro);
        var lista = so.FindProperty("vinetas");
        lista.arraySize = vinetas.Length;
        for (int i = 0; i < vinetas.Length; i++)
        {
            var e = lista.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("imagen").objectReferenceValue = Spr($"Intro/{vinetas[i].img}.png");
            e.FindPropertyRelative("texto").stringValue = vinetas[i].txt;
        }
        so.FindProperty("imagen").objectReferenceValue = imagen;
        so.FindProperty("grupoImagen").objectReferenceValue = grupo;
        so.FindProperty("texto").objectReferenceValue = texto;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(escena, RutaIntro);
    }

    // ================================================================== Ayudantes de UI
    static void CrearCamara()
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        go.transform.position = new Vector3(0f, 0f, -10f);
        var cam = go.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        go.AddComponent<AudioListener>();
    }

    static void CrearEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    static Canvas CrearCanvas(string nombre, int orden)
    {
        var go = new GameObject(nombre) { layer = 5 };
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = orden;
        var escalador = go.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static RectTransform Hijo(Transform padre, string nombre, Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform)) { layer = 5 };
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = anclaMin;
        rt.anchorMax = anclaMax;
        rt.pivot = pivote;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static RectTransform Centro(Transform padre, string nombre, Vector2 pos, Vector2 tam) =>
        Hijo(padre, nombre, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, tam);

    static RectTransform Estirar(Transform padre, string nombre) =>
        Hijo(padre, nombre, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

    static Text Texto(Transform padre, string nombre, string contenido, int tam, Color color, TextAnchor alineacion,
                      Vector2 pos, Vector2 caja, Vector2? ancla = null, Color? contorno = null)
    {
        RectTransform rt = ancla.HasValue ? Hijo(padre, nombre, ancla.Value, ancla.Value, new Vector2(0.5f, 0.5f), pos, caja)
                                          : Centro(padre, nombre, pos, caja);
        var t = rt.gameObject.AddComponent<Text>();
        t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alineacion;
        t.text = contenido;
        t.lineSpacing = 1.35f;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var borde = rt.gameObject.AddComponent<Outline>();
        float d = Mathf.Max(2f, tam / 14f);
        borde.effectColor = contorno ?? new Color(0f, 0f, 0f, 0.9f);
        borde.effectDistance = new Vector2(d, -d);
        return t;
    }

    static Image Imagen(Transform padre, string nombre, Sprite sprite, Vector2 pos, Vector2 tam, Vector2? ancla = null)
    {
        RectTransform rt = ancla.HasValue ? Hijo(padre, nombre, ancla.Value, ancla.Value, new Vector2(0.5f, 0.5f), pos, tam)
                                          : Centro(padre, nombre, pos, tam);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
    }

    static Button Boton(Transform padre, string nombre, string etiqueta, Vector2 pos, Vector2 tam)
    {
        RectTransform rt = Centro(padre, nombre, pos, tam);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprBoton;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.5f;
        var b = rt.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.SpriteSwap;
        b.spriteState = new SpriteState { highlightedSprite = sprBotonResaltado, selectedSprite = sprBotonResaltado, pressedSprite = sprBotonResaltado };
        Texto(rt, "Texto", etiqueta, 28, Crema, TextAnchor.MiddleCenter, new Vector2(0f, -2f), tam);
        rt.gameObject.AddComponent<BotonConSonido>();
        return b;
    }

    static Panel CrearPanel(Transform padre, string nombre, Vector2 pos, Vector2 tam)
    {
        RectTransform rt = Centro(padre, nombre, pos, tam);
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprPanel;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 0.5f;
        img.color = new Color(1f, 1f, 1f, 0.97f);
        rt.gameObject.AddComponent<CanvasGroup>();
        return rt.gameObject.AddComponent<Panel>();
    }

    static Slider CrearSlider(Transform padre, string nombre, Vector2 pos)
    {
        var recursos = new DefaultControls.Resources { standard = sprBoton, background = sprBoton, knob = sprBotonResaltado };
        GameObject go = DefaultControls.CreateSlider(recursos);
        go.name = nombre;
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(560f, 34f);

        foreach (var img in go.GetComponentsInChildren<Image>())
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 0.5f;
            if (img.name == "Background") img.color = new Color(0.25f, 0.15f, 0.1f);
            else if (img.name == "Fill") img.color = Dorado;
            else if (img.name == "Handle")
            {
                img.rectTransform.sizeDelta = new Vector2(34f, 0f);
                img.color = Color.white;
            }
        }
        var s = go.GetComponent<Slider>();
        s.minValue = 0f;
        s.maxValue = 1f;
        s.value = 0.7f;
        return s;
    }

    // ================================================================== Serialización
    static void Asignar(Object objeto, string campo, Object valor)
    {
        var so = new SerializedObject(objeto);
        so.FindProperty(campo).objectReferenceValue = valor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void ArregloObjetos(SerializedProperty prop, Object[] valores)
    {
        prop.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
    }
}
