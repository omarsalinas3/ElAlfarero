using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// La Llorona, jefa del Nivel 3 (#59): animaciones y prefab con su Animator. Su pelea (IA) es del Sprint 4.
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

    /// <summary>Prefab visual de la Llorona (sin IA todavía): flota sobre el agua del panteón.</summary>
    static void CrearPrefabLlorona()
    {
        RuntimeAnimatorController ctrl = CrearAnimacionesLlorona();
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        var go = new GameObject("Llorona") { layer = CapaEnemy };

        var visual = new GameObject("Visual") { layer = CapaEnemy };
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, 0.6f, 0f);   // No toca el suelo: flota
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Bosses/llorona_flotar_00.png");
        sr.sortingOrder = 6;
        visual.AddComponent<Animator>().runtimeAnimatorController = ctrl;

        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2.2f, 3.8f);
        col.offset = new Vector2(0f, 2.5f);
        go.AddComponent<Health>();

        PrefabUtility.SaveAsPrefabAsset(go, RutaPrefabLlorona);
        Object.DestroyImmediate(go);
    }
}
