using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// La Llorona, jefa del Nivel 3 (#59): animaciones, prefab con su IA (JefeLlorona), la onda sónica, los chorros de agua
/// y el disparador de su cinemática (EncuentroLlorona).
/// Cuadros de la hoja de Flow Docs/Arte/Flow/llorona_flow.jpg (regenerada sin la pierna de Mateo que traía la
/// referencia de la carpeta Modelos), recortados con Docs/Arte/recortar_hoja.py; el arte mira a la izquierda.
/// En la hoja original los dos últimos cuadros del ataque de agua miraban a la derecha y se reflejaron al recortar.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaPrefabLlorona = "Assets/Prefabs/Bosses/Llorona.prefab";

    /// <summary>Estados del Animator (se llaman con animator.Play, como los del Jinete).</summary>
    public static readonly string[] EstadosLlorona = { "Flotar", "Grito", "Agua", "Golpe", "Desvanecer" };

    static RuntimeAnimatorController CrearAnimacionesLlorona()
    {
        Directory.CreateDirectory("Assets/Art/Animations/Llorona");
        AssetDatabase.Refresh();
        const string pre = "Bosses/llorona";
        Sprite[] flotar = Cuadros(pre + "_flotar", 4);
        Sprite[] grito = Cuadros(pre + "_grito", 4);
        Sprite[] agua = Cuadros(pre + "_agua", 4);
        Sprite[] golpe = Cuadros(pre + "_golpe", 1);
        Sprite[] desvanecer = Cuadros(pre + "_desvanecer", 2);

        AnimationClip cFlotar = CrearClip("Llorona/Llorona_Flotar", flotar, 6f, true);           // Vaivén lento de fantasma
        AnimationClip cGrito = CrearClip("Llorona/Llorona_Grito", grito, 8f, false);
        AnimationClip cAgua = CrearClip("Llorona/Llorona_Agua", agua, 7f, false);
        AnimationClip cGolpe = CrearClip("Llorona/Llorona_Golpe", golpe, 1f, true);
        AnimationClip cDesvanecer = CrearClip("Llorona/Llorona_Desvanecer", desvanecer, 2f, false);

        const string ruta = "Assets/Art/Animations/Llorona/Llorona.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sFlotar = sm.AddState("Flotar"); sFlotar.motion = cFlotar;
        sm.AddState("Grito").motion = cGrito;
        sm.AddState("Agua").motion = cAgua;
        sm.AddState("Golpe").motion = cGolpe;
        AnimatorState sDesvanecer = sm.AddState("Desvanecer"); sDesvanecer.motion = cDesvanecer;
        sm.defaultState = sFlotar;
        Transicion(sm, sDesvanecer).AddCondition(AnimatorConditionMode.If, 0, "Muerto");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    /// <summary>Prefab de la Llorona: flota sobre el agua, cuerpo cinemático con collider trigger, como el Jinete.</summary>
    static void CrearPrefabLlorona()
    {
        RuntimeAnimatorController ctrl = CrearAnimacionesLlorona();
        BalaEnemiga onda = CrearOndaSonica();
        ChorroAgua chorro = CrearChorroAgua();
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        var go = new GameObject("Llorona") { layer = CapaEnemy };

        var visual = new GameObject("Visual") { layer = CapaEnemy };
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.6f, 0f);   // No toca el suelo: flota
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Bosses/llorona_flotar_00.png");
        sr.sortingOrder = 6;
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.2f, 3.8f);
        col.offset = new Vector2(0f, 2.5f);

        var salud = go.AddComponent<Health>();
        var soS = new SerializedObject(salud);
        // El doble que el Jinete (500): con 600 la Talavera (~80 de daño por segundo) la tumbaba en segundos y en el
        // Avance 3 pidieron una pelea más difícil
        soS.FindProperty("vidaMaxima").intValue = 1200;
        soS.FindProperty("destruirAlMorir").boolValue = false;
        soS.ApplyModifiedPropertiesWithoutUndo();
        Asignar(go.AddComponent<ParpadeoDano>(), "sprite", sr);
        var basico = go.AddComponent<EnemigoBasico>();
        var soB = new SerializedObject(basico);
        soB.FindProperty("sonidoMuerte").enumValueIndex = (int)Sfx.GritoLlorona;
        soB.FindProperty("puntos").intValue = 3500;
        soB.ApplyModifiedPropertiesWithoutUndo();

        var dano = go.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 15;
        soD.FindProperty("empuje").floatValue = 9f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var jefa = go.AddComponent<JefeLlorona>();
        var so = new SerializedObject(jefa);
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("danoContacto").objectReferenceValue = dano;
        so.FindProperty("prefabOnda").objectReferenceValue = onda;
        so.FindProperty("prefabChorro").objectReferenceValue = chorro;
        so.FindProperty("ySuelo").floatValue = YSuelo;
        so.ApplyModifiedPropertiesWithoutUndo();

        Guardar(go, RutaPrefabLlorona);
    }

    /// <summary>La onda sónica del grito: una BalaEnemiga más (se recicla y solo choca con Mateo y el suelo).</summary>
    static BalaEnemiga CrearOndaSonica()
    {
        var go = new GameObject("Onda_Sonica") { layer = CapaProyectilEnemigo };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Projectiles/onda_sonica_00.png");       // Los arcos abren hacia la derecha, hacia "transform.right"
        sr.sortingOrder = 20;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.45f;
        var bala = go.AddComponent<BalaEnemiga>();
        var so = new SerializedObject(bala);
        so.FindProperty("tiempoVida").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, "Assets/Prefabs/Projectiles/Onda_Sonica.prefab").GetComponent<BalaEnemiga>();
    }

    static ChorroAgua CrearChorroAgua()
    {
        var go = new GameObject("Chorro_Agua");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Projectiles/chorro_aviso_00.png");
        sr.sortingOrder = 18;
        var chorro = go.AddComponent<ChorroAgua>();
        var so = new SerializedObject(chorro);
        so.FindProperty("sprite").objectReferenceValue = sr;
        ArregloObjetos(so.FindProperty("cuadrosAviso"), Cuadros("Projectiles/chorro_aviso", 2));
        ArregloObjetos(so.FindProperty("cuadrosChorro"), Cuadros("Projectiles/chorro_agua", 3));
        so.FindProperty("capaJugador").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, "Assets/Prefabs/Projectiles/Chorro_Agua.prefab").GetComponent<ChorroAgua>();
    }

    /// <summary>Llorona dormida y el disparador de su cinemática al llegar a la orilla del río.</summary>
    static EncuentroLlorona CrearEncuentroLlorona(float inicioArena, float derecha, BarraJefe barra, Transform enemigos)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabLlorona);
        var llorona = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemigos);
        llorona.transform.position = new Vector3(derecha - 2f, YSuelo + 0.4f, 0f);

        var go = new GameObject("Encuentro_Llorona");
        go.transform.position = new Vector3(inicioArena + 2f, YSuelo + 2f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 8f);
        var enc = go.AddComponent<EncuentroLlorona>();
        var so = new SerializedObject(enc);
        so.FindProperty("jefa").objectReferenceValue = llorona.GetComponent<JefeLlorona>();
        so.FindProperty("barraJefe").objectReferenceValue = barra;
        so.FindProperty("xAparicion").floatValue = derecha - 2f;
        so.FindProperty("xLlegada").floatValue = derecha - 6f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return enc;
    }
}
