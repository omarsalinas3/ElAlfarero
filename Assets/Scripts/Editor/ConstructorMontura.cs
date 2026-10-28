using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// El Alebrije liberado como montura de Mateo (#50). Suelto usa los cuadros y el Animator del jefe, más chico; montado
/// cambia al Animator "Montado", con los cuadros de Flow donde Mateo va sentado en él (Docs/Arte/Flow/montado_flow.jpg).
/// Sin collider propio: va pegado a Mateo y los golpes que recibe Mateo se los lleva él (Health.Protector).
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaPrefabMontura = "Assets/Prefabs/Allies/Alebrije_Montura.prefab";
    const float EscalaMontura = 0.7f;

    static MonturaAlebrije CrearPrefabMontura()
    {
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Allies");
        var go = new GameObject("Alebrije_Montura");
        go.transform.localScale = new Vector3(EscalaMontura, EscalaMontura, 1f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Bosses/alebrije_caminar_00.png");
        sr.sortingOrder = 4;                                     // Detrás de Mateo, que va sentado encima
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Alebrije/Alebrije.controller");

        var salud = go.AddComponent<Health>();
        var soS = new SerializedObject(salud);
        soS.FindProperty("vidaMaxima").intValue = 60;          // De 4 a 6 golpes de bandido
        soS.FindProperty("destruirAlMorir").boolValue = false;
        soS.ApplyModifiedPropertiesWithoutUndo();
        Asignar(go.AddComponent<ParpadeoDano>(), "sprite", sr);

        var montura = go.AddComponent<MonturaAlebrije>();
        var so = new SerializedObject(montura);
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("capasEnemigo").FindPropertyRelative("m_Bits").intValue = 1 << CapaEnemy;
        so.FindProperty("animadorMontado").objectReferenceValue = CrearAnimadorMontado();
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, RutaPrefabMontura).GetComponent<MonturaAlebrije>();
    }

    /// <summary>Mismos estados que el Animator del alebrije (así MonturaAlebrije los pide igual) más el golpe.</summary>
    static RuntimeAnimatorController CrearAnimadorMontado()
    {
        System.IO.Directory.CreateDirectory("Assets/Art/Animations/Montado");
        AssetDatabase.Refresh();
        const string pre = "Allies/montado";
        AnimationClip reposo = CrearClip("Montado/Montado_Reposo", Cuadros(pre + "_reposo", 1), 1f, true);
        AnimationClip galope = CrearClip("Montado/Montado_Galope", Cuadros(pre + "_galope", 4), 9f, true);
        AnimationClip salto = CrearClip("Montado/Montado_Salto", Cuadros(pre + "_salto", 1), 1f, true);
        AnimationClip zarpazo = CrearClip("Montado/Montado_Zarpazo", Cuadros(pre + "_zarpazo", 4), 14f, false);
        AnimationClip golpe = CrearClip("Montado/Montado_Golpe", Cuadros(pre + "_golpe", 1), 1f, true);

        const string ruta = "Assets/Art/Animations/Montado/Montado.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sReposo = sm.AddState("Reposo"); sReposo.motion = reposo;
        AnimatorState sCaminar = sm.AddState("Caminar"); sCaminar.motion = galope;
        sm.AddState("Salto").motion = salto;
        sm.AddState("Zarpazo").motion = zarpazo;
        sm.AddState("Golpe").motion = golpe;
        sm.defaultState = sReposo;
        Transicion(sReposo, sCaminar).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sCaminar, sReposo).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    /// <summary>Pone la montura (apagada) en la escena y se la da al jefe; llegar montado a xMeta completa el nivel.</summary>
    static void PonerMontura(JefeAlebrije jefe, float xMeta, Transform padre)
    {
        MonturaAlebrije prefab = CrearPrefabMontura();
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, padre);
        go.transform.position = jefe.transform.position;
        var montura = go.GetComponent<MonturaAlebrije>();
        var so = new SerializedObject(montura);
        so.FindProperty("xMeta").floatValue = xMeta;
        so.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(false);                                     // Aparece cuando se rompe el hechizo
        Asignar(jefe, "montura", montura);
    }
}
