using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ConfigurarSprint1;

/// <summary>
/// El Charro Negro, jefe final (#89): el arte de sus dos fases y su fuego infernal. Pelea primero a caballo y, cuando
/// el caballo cae, sigue a pie con los ataques más difíciles del juego; la IA y la pelea van en la #90.
/// Hojas de Flow (Docs/Arte/Flow): charro_caballo_flow.jpg (4x4), charro_pie_flow.jpg (4x5) y charro_fuego_flow.jpg
/// (bolas de fuego y llamaradas), hechas con la referencia de la carpeta Modelos y recortadas con
/// Docs/Arte/recortar_hoja.py; los prompts y comandos están en Docs/Arte/PROMPT_CHARRO.md. El arte mira a la DERECHA,
/// como el del Jinete.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaPrefabCharro = "Assets/Prefabs/Bosses/Charro.prefab";
    const string RutaFuegoCharro = "Assets/Prefabs/Projectiles/Fuego_Charro.prefab";

    /// <summary>Estados de cada fase: la IA los llama con animator.Play, como los del Jinete y la Llorona.</summary>
    public static readonly string[] EstadosCharroCaballo = { "Reposo", "Galope", "Encabritar", "Lanzar", "Golpe", "Caida" };
    public static readonly string[] EstadosCharroPie = { "Reposo", "Caminar", "Espadazo", "Lanzar", "Furia", "Clavar", "Golpe", "Muerte" };

    static void CrearArteCharro()
    {
        Directory.CreateDirectory("Assets/Art/Animations/Charro");
        AssetDatabase.Refresh();
        RuntimeAnimatorController caballo = CrearAnimacionesCharroCaballo();
        RuntimeAnimatorController pie = CrearAnimacionesCharroPie();

        // Fuego infernal, efecto aparte: la ráfaga que vuela (el cuadro 00 no trae estela; solo sirve en la hoja) y la
        // llamarada que brota del suelo, que empieza con una grieta encendida como aviso
        Sprite[] bolas = Cuadros("Projectiles/charro_fuego", 5);
        RuntimeAnimatorController fuego = ControladorCharro("Fuego", "Volar",
            CrearClip("Charro/Fuego_Volar", new[] { bolas[1], bolas[2], bolas[3], bolas[4] }, 12f, true));
        RuntimeAnimatorController llamarada = ControladorCharro("Llamarada", "Brotar",
            CrearClip("Charro/Llamarada_Brotar", Cuadros("Projectiles/charro_llamarada", 5), 8f, false));

        GameObject prefab = CrearPrefabCharro(caballo, pie);
        CrearFuegoCharro(fuego);
        PonerGaleriaCharro(prefab, fuego, llamarada);
    }

    static RuntimeAnimatorController CrearAnimacionesCharroCaballo()
    {
        const string pre = "Bosses/charro_caballo";
        AnimationClip reposo = CrearClip("Charro/Caballo_Reposo", Cuadros(pre + "_reposo", 2), 3f, true);   // El caballo escarba
        AnimationClip galope = CrearClip("Charro/Caballo_Galope", Cuadros(pre + "_galope", 4), 10f, true);
        AnimationClip encabritar = CrearClip("Charro/Caballo_Encabritar", Cuadros(pre + "_encabritar", 1), 1f, true);  // Aviso de la embestida
        AnimationClip lanzar = CrearClip("Charro/Caballo_Lanzar", Cuadros(pre + "_lanzar", 4), 9f, false);
        AnimationClip golpe = CrearClip("Charro/Caballo_Golpe", Cuadros(pre + "_golpe", 1), 1f, true);
        AnimationClip caida = CrearClip("Charro/Caballo_Caida", Cuadros(pre + "_caida", 3), 2.5f, false);  // Tropieza, cae y se hace humo

        const string ruta = "Assets/Art/Animations/Charro/CharroCaballo.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Caido", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sReposo = sm.AddState("Reposo"); sReposo.motion = reposo;
        AnimatorState sGalope = sm.AddState("Galope"); sGalope.motion = galope;
        sm.AddState("Encabritar").motion = encabritar;
        sm.AddState("Lanzar").motion = lanzar;
        sm.AddState("Golpe").motion = golpe;
        AnimatorState sCaida = sm.AddState("Caida"); sCaida.motion = caida;
        sm.defaultState = sReposo;

        Transicion(sReposo, sGalope).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sGalope, sReposo).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        Transicion(sm, sCaida).AddCondition(AnimatorConditionMode.If, 0, "Caido");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static RuntimeAnimatorController CrearAnimacionesCharroPie()
    {
        const string pre = "Bosses/charro_pie";
        AnimationClip reposo = CrearClip("Charro/Pie_Reposo", Cuadros(pre + "_reposo", 1), 1f, true);
        AnimationClip caminar = CrearClip("Charro/Pie_Caminar", Cuadros(pre + "_caminar", 4), 8f, true);
        AnimationClip espadazo = CrearClip("Charro/Pie_Espadazo", Cuadros(pre + "_espadazo", 4), 10f, false);
        AnimationClip lanzar = CrearClip("Charro/Pie_Lanzar", Cuadros(pre + "_lanzar", 4), 9f, false);
        AnimationClip furia = CrearClip("Charro/Pie_Furia", Cuadros(pre + "_furia", 1), 1f, true);         // Ruge envuelto en llamas
        AnimationClip clavar = CrearClip("Charro/Pie_Clavar", Cuadros(pre + "_clavar", 3), 7f, false);     // Clava la espada: brotan llamaradas
        AnimationClip golpe = CrearClip("Charro/Pie_Golpe", Cuadros(pre + "_golpe", 1), 1f, true);
        AnimationClip muerte = CrearClip("Charro/Pie_Muerte", Cuadros(pre + "_muerte", 2), 2f, false);    // Cae de rodillas y arde en humo

        const string ruta = "Assets/Art/Animations/Charro/CharroPie.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sReposo = sm.AddState("Reposo"); sReposo.motion = reposo;
        AnimatorState sCaminar = sm.AddState("Caminar"); sCaminar.motion = caminar;
        sm.AddState("Espadazo").motion = espadazo;
        sm.AddState("Lanzar").motion = lanzar;
        sm.AddState("Furia").motion = furia;
        sm.AddState("Clavar").motion = clavar;
        sm.AddState("Golpe").motion = golpe;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = muerte;
        sm.defaultState = sReposo;

        Transicion(sReposo, sCaminar).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sCaminar, sReposo).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        Transicion(sm, sMuerte).AddCondition(AnimatorConditionMode.If, 0, "Muerto");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    /// <summary>Animator de un solo estado para los efectos de fuego.</summary>
    static RuntimeAnimatorController ControladorCharro(string nombre, string estado, AnimationClip clip)
    {
        string ruta = $"Assets/Art/Animations/Charro/{nombre}.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.layers[0].stateMachine.AddState(estado).motion = clip;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    /// <summary>
    /// Solo el aspecto del Charro: una cara por fase. "Pie" empieza apagado y la IA (#90) lo enciende cuando cae el
    /// caballo; ahí también se agregan su vida, colisiones y daño.
    /// </summary>
    static GameObject CrearPrefabCharro(RuntimeAnimatorController caballo, RuntimeAnimatorController pie)
    {
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        var go = new GameObject("Charro") { layer = CapaEnemy };
        HijoAnimado(go.transform, "Caballo", "Bosses/charro_caballo_reposo_00.png", caballo, 6);
        HijoAnimado(go.transform, "Pie", "Bosses/charro_pie_reposo_00.png", pie, 6).SetActive(false);
        return Guardar(go, RutaPrefabCharro);
    }

    /// <summary>La ráfaga de fuego infernal: una BalaEnemiga más (se recicla y solo choca con Mateo y el suelo).</summary>
    static BalaEnemiga CrearFuegoCharro(RuntimeAnimatorController fuego)
    {
        var go = new GameObject("Fuego_Charro") { layer = CapaProyectilEnemigo };
        GameObject visual = HijoAnimado(go.transform, "Visual", "Projectiles/charro_fuego_01.png", fuego, 20);   // Va a la derecha, hacia "transform.right"
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;
        // En la bola, no en la estela: la bola va pegada a la orilla derecha del cuadro
        col.offset = new Vector2(visual.GetComponent<SpriteRenderer>().sprite.bounds.extents.x - 0.3f, 0f);
        var bala = go.AddComponent<BalaEnemiga>();
        var so = new SerializedObject(bala);
        so.FindProperty("tiempoVida").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, RutaFuegoCharro).GetComponent<BalaEnemiga>();
    }

    /// <summary>
    /// Mientras no exista el Nivel 4, las pruebas revisan al Charro y su fuego en el Sandbox: quedan apagados y lejos
    /// de Mateo para no estorbar a las demás pruebas de esa escena.
    /// </summary>
    static void PonerGaleriaCharro(GameObject prefab, RuntimeAnimatorController fuego, RuntimeAnimatorController llamarada)
    {
        Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        var galeria = new GameObject("Galeria_Charro");
        galeria.transform.position = new Vector3(80f, YSuelo, 0f);
        PrefabUtility.InstantiatePrefab(prefab, galeria.transform);
        HijoAnimado(galeria.transform, "Fuego", "Projectiles/charro_fuego_01.png", fuego, 20).transform.localPosition = new Vector3(8f, 2f, 0f);
        HijoAnimado(galeria.transform, "Llamarada", "Projectiles/charro_llamarada_00.png", llamarada, 18).transform.localPosition = new Vector3(12f, 0f, 0f);
        galeria.SetActive(false);
        EditorSceneManager.SaveScene(escena);
    }

    static GameObject HijoAnimado(Transform padre, string nombre, string sprite, RuntimeAnimatorController ctrl, int orden)
    {
        var hijo = new GameObject(nombre) { layer = padre.gameObject.layer };
        hijo.transform.SetParent(padre, false);
        var sr = hijo.AddComponent<SpriteRenderer>();
        sr.sprite = Spr(sprite);
        sr.sortingOrder = orden;
        hijo.AddComponent<Animator>().runtimeAnimatorController = ctrl;
        return hijo;
    }
}
