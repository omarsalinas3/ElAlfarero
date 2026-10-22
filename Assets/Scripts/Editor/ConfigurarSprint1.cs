using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menú Alfarero > Sprint 1 > Construir Sandbox.
/// Genera de forma reproducible todo lo del Sprint 1 a partir de los sprites en Assets/Art/Sprites:
/// capas, material sin fricción, animaciones de Mateo, prefabs (Alfarero, Proyectil Talavera, Impacto, Minero dummy),
/// el ScriptableObject Ceramica_Talavera y la escena Sandbox.
/// Se puede volver a ejecutar cuando cambien los sprites (sobrescribe los assets generados).
/// En consola: Unity -batchmode -quit -executeMethod ConfigurarSprint1.Construir
/// </summary>
public static class ConfigurarSprint1
{
    // Capas (índices fijos para que coincidan entre máquinas)
    internal const int CapaGround = 6, CapaPlayer = 7, CapaEnemy = 8, CapaProjectile = 9;

    // Mundo: el fondo mide 21.5 x 12 unidades; el suelo (adoquín) queda en y = -4.5
    const float PixelesPorUnidadFondo = 128f;
    const float AnchoFondo = 2752f / PixelesPorUnidadFondo;   // 21.5
    // Segmentos del Nivel 1 en orden. solapePx = píxeles (de la imagen de 2752 de ancho) que cada uno se traslapa
    // sobre el anterior; coincide con el degradado de transparencia que lleva su borde izquierdo.
    static readonly (string archivo, int solapePx)[] Segmentos =
    {
        ("lvl1_dolores_noche.png", 0),
        ("lvl1_seg02.png", 320),
        ("lvl1_seg03.png", 324),
        ("lvl1_seg04.png", 384),
        ("lvl1_seg05.png", 828),
    };
    internal const float YSuelo = -4.5f;

    internal const string Sprites = "Assets/Art/Sprites/";
    internal const string RutaEscena = "Assets/Scenes/Sandbox.unity";
    internal const string RutaPrefabAlfarero = "Assets/Prefabs/Player/Alfarero.prefab";
    internal const string RutaPrefabMinero = "Assets/Prefabs/Enemies/Minero_Dummy.prefab";

    [MenuItem("Alfarero/Sprint 1/Construir Sandbox")]
    public static void Construir()
    {
        ConfigurarCapas();
        CrearCarpetas();
        GenerarPixelBlanco();
        ReimportarSprites();

        PhysicsMaterial2D sinFriccion = CrearMaterialSinFriccion();
        RuntimeAnimatorController animador = CrearAnimacionesMateo();

        SpriteAnimadoTemporal impacto = CrearPrefabImpacto();
        Projectile proyectil = CrearPrefabProyectil();
        CeramicaData talavera = CrearCeramicaTalavera(proyectil, impacto);
        GameObject alfarero = CrearPrefabAlfarero(sinFriccion, animador, talavera);
        GameObject dummy = CrearPrefabDummy();

        CrearEscenaSandbox(alfarero, dummy);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Sprint 1: sandbox construido en " + RutaEscena);
    }

    // ------------------------------------------------------------------ Proyecto
    static void ConfigurarCapas()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty capas = tagManager.FindProperty("layers");
        capas.GetArrayElementAtIndex(CapaGround).stringValue = "Ground";
        capas.GetArrayElementAtIndex(CapaPlayer).stringValue = "Player";
        capas.GetArrayElementAtIndex(CapaEnemy).stringValue = "Enemy";
        capas.GetArrayElementAtIndex(CapaProjectile).stringValue = "Projectile";
        tagManager.ApplyModifiedPropertiesWithoutUndo();

        // Los proyectiles no chocan entre sí ni con el jugador
        Physics2D.IgnoreLayerCollision(CapaProjectile, CapaProjectile, true);
        Physics2D.IgnoreLayerCollision(CapaProjectile, CapaPlayer, true);
    }

    static void CrearCarpetas()
    {
        foreach (string c in new[]
        {
            "Art/Animations", "Prefabs/Player", "Prefabs/Enemies", "Prefabs/Projectiles",
            "ScriptableObjects/Ceramicas", "Scenes", "Settings"
        })
            Directory.CreateDirectory(Path.Combine(Application.dataPath, c));
        AssetDatabase.Refresh();
    }

    static void GenerarPixelBlanco()
    {
        string ruta = Sprites + "Environment/pixel_blanco.png";
        if (File.Exists(ruta)) return;
        var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var colores = new Color32[16];
        for (int i = 0; i < 16; i++) colores[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(colores);
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);
    }

    static void ReimportarSprites()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Art/Sprites" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }

    static PhysicsMaterial2D CrearMaterialSinFriccion()
    {
        const string ruta = "Assets/Settings/SinFriccion.physicsMaterial2D";
        var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(ruta);
        if (mat == null)
        {
            mat = new PhysicsMaterial2D("SinFriccion") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, ruta);
        }
        return mat;
    }

    // ------------------------------------------------------------------ Animaciones
    static RuntimeAnimatorController CrearAnimacionesMateo()
    {
        Sprite[] run = new Sprite[5];
        for (int i = 0; i < 5; i++) run[i] = Spr($"Player/mateo_run_{i:00}.png");

        // Reposo y muerte salen de la hoja de Flow (mateo_idle_XX / mateo_muerte_XX)
        AnimationClip idle = CrearClip("Mateo_Idle", Cuadros("Player/mateo_idle", 4), 5f, true);
        AnimationClip morir = CrearClip("Mateo_Muerte", Cuadros("Player/mateo_muerte", 4), 7f, false);
        AnimationClip correr = CrearClip("Mateo_Run", run, 12f, true);
        AnimationClip saltar = CrearClip("Mateo_Jump", new[] { run[2] }, 1f, true);
        AnimationClip evadir = CrearClip("Mateo_Evadir", new[] { run[0] }, 1f, true);

        const string ruta = "Assets/Art/Animations/Mateo.controller";
        AssetDatabase.DeleteAsset(ruta);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("EnSuelo", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Evadiendo", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sIdle = sm.AddState("Idle"); sIdle.motion = idle;
        AnimatorState sRun = sm.AddState("Run"); sRun.motion = correr;
        AnimatorState sJump = sm.AddState("Jump"); sJump.motion = saltar;
        AnimatorState sEvadir = sm.AddState("Evadir"); sEvadir.motion = evadir;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = morir;
        sm.defaultState = sIdle;

        Transicion(sIdle, sRun).AddCondition(AnimatorConditionMode.Greater, 0.3f, "Velocidad");
        Transicion(sRun, sIdle).AddCondition(AnimatorConditionMode.Less, 0.3f, "Velocidad");

        AnimatorStateTransition aJump = Transicion(sm, sJump);
        aJump.AddCondition(AnimatorConditionMode.IfNot, 0, "EnSuelo");
        aJump.AddCondition(AnimatorConditionMode.IfNot, 0, "Evadiendo");
        Transicion(sJump, sIdle).AddCondition(AnimatorConditionMode.If, 0, "EnSuelo");

        Transicion(sm, sEvadir).AddCondition(AnimatorConditionMode.If, 0, "Evadiendo");
        Transicion(sEvadir, sIdle).AddCondition(AnimatorConditionMode.IfNot, 0, "Evadiendo");

        // Muerte gana sobre todo lo demás (y las transiciones de arriba no aplican mientras esté muerto)
        Transicion(sm, sMuerte).AddCondition(AnimatorConditionMode.If, 0, "Muerto");
        foreach (var t in sm.anyStateTransitions)
            if (t.destinationState != sMuerte) t.AddCondition(AnimatorConditionMode.IfNot, 0, "Muerto");

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    internal static Sprite[] Cuadros(string prefijo, int n)
    {
        var cuadros = new Sprite[n];
        for (int i = 0; i < n; i++) cuadros[i] = Spr($"{prefijo}_{i:00}.png");
        return cuadros;
    }

    internal static AnimatorStateTransition Transicion(AnimatorState desde, AnimatorState hasta)
    {
        AnimatorStateTransition t = desde.AddTransition(hasta);
        t.hasExitTime = false; t.duration = 0f;
        return t;
    }

    internal static AnimatorStateTransition Transicion(AnimatorStateMachine sm, AnimatorState hasta)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(hasta);
        t.hasExitTime = false; t.duration = 0f; t.canTransitionToSelf = false;
        return t;
    }

    internal static AnimationClip CrearClip(string nombre, Sprite[] cuadros, float fps, bool loop)
    {
        string ruta = $"Assets/Art/Animations/{nombre}.anim";
        AssetDatabase.DeleteAsset(ruta);

        var clip = new AnimationClip { frameRate = fps };
        EditorCurveBinding binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var llaves = new ObjectReferenceKeyframe[cuadros.Length];
        for (int i = 0; i < cuadros.Length; i++)
            llaves[i] = new ObjectReferenceKeyframe { time = i / fps, value = cuadros[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, llaves);

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        AssetDatabase.CreateAsset(clip, ruta);
        return clip;
    }

    // ------------------------------------------------------------------ Prefabs
    static SpriteAnimadoTemporal CrearPrefabImpacto()
    {
        var go = new GameObject("Impacto_Talavera");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 25;
        var fx = go.AddComponent<SpriteAnimadoTemporal>();

        var cuadros = new Sprite[7];
        for (int i = 0; i < 7; i++) cuadros[i] = Spr($"Projectiles/talavera_impact_{i:00}.png");
        var so = new SerializedObject(fx);
        so.FindProperty("cuadros").arraySize = cuadros.Length;
        for (int i = 0; i < cuadros.Length; i++) so.FindProperty("cuadros").GetArrayElementAtIndex(i).objectReferenceValue = cuadros[i];
        so.FindProperty("cuadrosPorSegundo").floatValue = 20f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, "Assets/Prefabs/Projectiles/Impacto_Talavera.prefab").GetComponent<SpriteAnimadoTemporal>();
    }

    static Projectile CrearPrefabProyectil()
    {
        var go = new GameObject("Proyectil_Talavera") { layer = CapaProjectile };
        Sprite s = Spr("Projectiles/talavera_shot_00.png");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 20;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.22f;
        col.offset = new Vector2(s.bounds.extents.x * 0.55f, 0f);   // La vasija va al frente, la estela atrás

        go.AddComponent<Projectile>();
        return Guardar(go, "Assets/Prefabs/Projectiles/Proyectil_Talavera.prefab").GetComponent<Projectile>();
    }

    static CeramicaData CrearCeramicaTalavera(Projectile proyectil, SpriteAnimadoTemporal impacto)
    {
        const string ruta = "Assets/ScriptableObjects/Ceramicas/Ceramica_Talavera.asset";
        AssetDatabase.DeleteAsset(ruta);
        var c = ScriptableObject.CreateInstance<CeramicaData>();
        c.nombre = "Talavera de Dolores";
        c.tipo = CeramicaData.TipoCeramica.Talavera;
        c.icono = Spr("Projectiles/talavera_piece_00.png");
        c.prefabProyectil = proyectil;
        c.prefabImpacto = impacto;
        c.dano = 10;
        c.velocidadProyectil = 18f;
        c.tiempoVida = 1.5f;
        c.capasImpacto = (1 << CapaGround) | (1 << CapaEnemy);
        c.disparosPorSegundo = 8f;
        c.proyectilesPorDisparo = 1;
        c.dispersionGrados = 2f;
        AssetDatabase.CreateAsset(c, ruta);
        return c;
    }

    static GameObject CrearPrefabAlfarero(PhysicsMaterial2D mat, RuntimeAnimatorController animador, CeramicaData ceramica)
    {
        var raiz = new GameObject("Alfarero") { layer = CapaPlayer };

        var rb = raiz.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var col = raiz.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.9f, 2.3f);
        col.offset = new Vector2(0f, 1.15f);
        col.sharedMaterial = mat;

        raiz.AddComponent<Health>();
        raiz.AddComponent<ParpadeoDano>();
        var controlador = raiz.AddComponent<PlayerController>();
        var tirador = raiz.AddComponent<PlayerShooter>();
        raiz.AddComponent<InventarioJugador>();
        raiz.AddComponent<ReaccionDanoJugador>();

        // Visual: sprite + animator (pivote en los pies)
        var visual = new GameObject("Visual") { layer = CapaPlayer };
        visual.transform.SetParent(raiz.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Player/mateo_run_04.png");
        sr.sortingOrder = 10;
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = animador;
        visual.AddComponent<PlayerAnimator>();

        var puntoSuelo = new GameObject("PuntoSuelo");
        puntoSuelo.transform.SetParent(raiz.transform, false);
        puntoSuelo.transform.localPosition = new Vector3(0f, -0.08f, 0f);

        var puntoDisparo = new GameObject("PuntoDisparo");
        puntoDisparo.transform.SetParent(raiz.transform, false);
        puntoDisparo.transform.localPosition = new Vector3(0.8f, 1.45f, 0f);   // Donde sostiene la vasija

        var soCtrl = new SerializedObject(controlador);
        soCtrl.FindProperty("puntoSuelo").objectReferenceValue = puntoSuelo.transform;
        soCtrl.FindProperty("capaSuelo").FindPropertyRelative("m_Bits").intValue = 1 << CapaGround;
        soCtrl.ApplyModifiedPropertiesWithoutUndo();

        var soTir = new SerializedObject(tirador);
        soTir.FindProperty("ceramicaActual").objectReferenceValue = ceramica;
        soTir.FindProperty("puntoDisparo").objectReferenceValue = puntoDisparo.transform;
        soTir.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(raiz, RutaPrefabAlfarero);
    }

    static GameObject CrearPrefabDummy()
    {
        var go = new GameObject("Minero_Dummy") { layer = CapaEnemy };
        Sprite s = Spr("Enemies/minero_esqueleto.png");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.sortingOrder = 5;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(s.bounds.size.x * 0.6f, s.bounds.size.y * 0.95f);
        col.offset = new Vector2(0f, s.bounds.size.y * 0.5f);

        var salud = go.AddComponent<Health>();
        var so = new SerializedObject(salud);
        so.FindProperty("vidaMaxima").intValue = 100;
        so.FindProperty("destruirAlMorir").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        go.AddComponent<ParpadeoDano>();
        go.AddComponent<EnemigoBasico>();

        // Sin IA todavía: lastima al tocarlo
        var dano = go.AddComponent<DanoPorContacto>();
        var soDano = new SerializedObject(dano);
        soDano.FindProperty("dano").intValue = 10;
        soDano.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soDano.ApplyModifiedPropertiesWithoutUndo();

        go.transform.localScale = Vector3.one * 0.55f;  // El sprite del minero sale más grande que el de Mateo (~2 unidades de alto)
        return Guardar(go, RutaPrefabMinero);
    }

    // ------------------------------------------------------------------ Escena
    static void CrearEscenaSandbox(GameObject prefabAlfarero, GameObject prefabDummy)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Sprite pixel = Spr("Environment/pixel_blanco.png");
        CrearBaseNivel(prefabAlfarero, out _, out _);

        // Plataformas de una vía (se atraviesan desde abajo)
        CrearPlataforma("Plataforma_A", 12f, YSuelo + 2.4f, 5f, pixel);
        CrearPlataforma("Plataforma_B", 18f, YSuelo + 4.5f, 4f, pixel);
        CrearPlataforma("Plataforma_C", 46f, YSuelo + 2.4f, 5f, pixel);
        CrearPlataforma("Plataforma_D", 70f, YSuelo + 2.4f, 5f, pixel);

        // Dummies para probar el disparo
        Dummy(prefabDummy, 4f, YSuelo);
        Dummy(prefabDummy, 12f, YSuelo + 2.4f);
        Dummy(prefabDummy, 24f, YSuelo);
        Dummy(prefabDummy, 40f, YSuelo);
        Dummy(prefabDummy, 52f, YSuelo);
        Dummy(prefabDummy, 64f, YSuelo);
        Dummy(prefabDummy, 78f, YSuelo);

        EditorSceneManager.SaveScene(escena, RutaEscena);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(RutaEscena, true) };
    }

    /// <summary>
    /// Lo común a todo nivel del Nivel 1: fondo por segmentos, suelo, muros, Mateo, cámara que lo sigue y luz.
    /// Devuelve la instancia del jugador y los límites izquierdo/derecho del mundo.
    /// </summary>
    internal static GameObject CrearBaseNivel(GameObject prefabAlfarero, out float izquierda, out float derecha)
    {
        // Fondo: segmentos distintos en orden. Cada uno (salvo el 1.º) trae el borde izquierdo con degradado
        // de transparencia y se superpone Solape unidades sobre el anterior, así la unión no se nota.
        var fondos = new GameObject("Fondo");
        float xCentro = 0f;
        for (int i = 0; i < Segmentos.Length; i++)
        {
            if (i > 0) xCentro += AnchoFondo - Segmentos[i].solapePx / PixelesPorUnidadFondo;
            var f = new GameObject($"Fondo_{i + 1}");
            f.transform.SetParent(fondos.transform, false);
            f.transform.position = new Vector3(xCentro, 0f, 0f);
            var sr = f.AddComponent<SpriteRenderer>();
            sr.sprite = Spr("Environment/" + Segmentos[i].archivo);
            sr.sortingOrder = -100 + i;
        }

        izquierda = -AnchoFondo / 2f;
        derecha = xCentro + AnchoFondo / 2f;
        float largo = derecha - izquierda;

        // Suelo invisible a la altura del adoquín + muros en los extremos
        var suelo = new GameObject("Suelo") { layer = CapaGround };
        suelo.transform.position = new Vector3((izquierda + derecha) / 2f, YSuelo - 1f, 0f);
        suelo.AddComponent<BoxCollider2D>().size = new Vector2(largo, 2f);
        CrearMuro("Muro_Izquierdo", izquierda - 0.5f);
        CrearMuro("Muro_Derecho", derecha + 0.5f);

        // Jugador
        var alfarero = (GameObject)PrefabUtility.InstantiatePrefab(prefabAlfarero);
        alfarero.transform.position = new Vector3(-6f, YSuelo + 0.02f, 0f);

        // Cámara
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        camGo.AddComponent<AudioListener>();
        var seguimiento = camGo.AddComponent<CameraFollow>();
        var soCam = new SerializedObject(seguimiento);
        soCam.FindProperty("objetivo").objectReferenceValue = alfarero.transform;
        soCam.FindProperty("limiteIzquierdo").floatValue = izquierda;
        soCam.FindProperty("limiteDerecho").floatValue = derecha;
        soCam.ApplyModifiedPropertiesWithoutUndo();

        CrearLuzGlobalSiHayURP();

        // El HUD de depuración (HudSandbox) ya no se agrega: tapaba la pantalla. Si se necesita, añadir el
        // componente HudSandbox a un objeto vacío y asignar jugador, salud y tirador.
        return alfarero;
    }

    /// <summary>
    /// Con URP 2D los sprites usan el material "Lit": sin una Light 2D global se ven negros.
    /// Se crea por reflexión para que el proyecto compile igual sin URP.
    /// </summary>
    internal static void CrearLuzGlobalSiHayURP()
    {
        System.Type tipoLuz = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime")
                            ?? System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
        if (tipoLuz == null) return;

        var go = new GameObject("Global Light 2D");
        Component luz = go.AddComponent(tipoLuz);
        System.Type tipoEnum = tipoLuz.GetNestedType("LightType");
        tipoLuz.GetProperty("lightType").SetValue(luz, System.Enum.Parse(tipoEnum, "Global"));
        tipoLuz.GetProperty("intensity").SetValue(luz, 1f);
    }

    static void CrearMuro(string nombre, float x)
    {
        var muro = new GameObject(nombre) { layer = CapaGround };
        muro.transform.position = new Vector3(x, 0f, 0f);
        muro.AddComponent<BoxCollider2D>().size = new Vector2(1f, 30f);
    }

    static void CrearPlataforma(string nombre, float x, float yTope, float ancho, Sprite pixel)
    {
        const float grosor = 0.4f;
        var p = new GameObject(nombre) { layer = CapaGround };
        p.transform.position = new Vector3(x, yTope - grosor / 2f, 0f);
        p.transform.localScale = new Vector3(ancho, grosor, 1f);
        var sr = p.AddComponent<SpriteRenderer>();
        sr.sprite = pixel;
        sr.color = new Color(0.35f, 0.2f, 0.1f);
        sr.sortingOrder = 1;
        var col = p.AddComponent<BoxCollider2D>();
        col.usedByEffector = true;
        var efector = p.AddComponent<PlatformEffector2D>();
        efector.useOneWay = true;
        efector.surfaceArc = 160f;
    }

    static void Dummy(GameObject prefab, float x, float y)
    {
        var d = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        d.transform.position = new Vector3(x, y, 0f);
        Vector3 e = d.transform.localScale;
        d.transform.localScale = new Vector3(-e.x, e.y, e.z);   // Mira hacia el jugador (a la izquierda)
    }

    // ------------------------------------------------------------------ Utilidades
    internal static Sprite Spr(string rutaRelativa)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + rutaRelativa);
        if (s == null) throw new FileNotFoundException("No se pudo cargar el sprite: " + Sprites + rutaRelativa);
        return s;
    }

    internal static GameObject Guardar(GameObject go, string ruta)
    {
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, ruta);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
