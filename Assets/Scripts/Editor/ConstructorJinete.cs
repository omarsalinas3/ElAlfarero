using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// Jefe del Nivel 2 (#55, #56): el Jinete sin Cabeza, su Animator y el encuentro en el patio de la capilla.
/// Cuadros de la hoja de Flow (Docs/Arte/Flow/jinete_flow.jpg → Art/Sprites/Bosses/jinete_*). Mientras no existan,
/// usa provisionalmente al bandido del machete oscurecido, para poder probar la pelea.
/// Animator: Reposo / Galope por "Velocidad", "Muerto" como bool; Sablazo, Lanzar y Golpe se reproducen desde la IA.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaPrefabJinete = "Assets/Prefabs/Bosses/Jinete.prefab";

    static bool HayArteJinete => File.Exists("Assets/Art/Sprites/Bosses/jinete_galope_00.png");

    static RuntimeAnimatorController CrearAnimacionesJinete()
    {
        Directory.CreateDirectory("Assets/Art/Animations/Jinete");
        AssetDatabase.Refresh();
        Sprite[] galope, sablazo, lanzar, golpe, muerte;
        if (HayArteJinete)
        {
            const string pre = "Bosses/jinete";
            galope = Cuadros(pre + "_galope", 4);
            sablazo = Cuadros(pre + "_sablazo", 4);
            lanzar = Cuadros(pre + "_lanzar", 4);
            golpe = Cuadros(pre + "_golpe", 1);
            muerte = Cuadros(pre + "_muerte", 3);
        }
        else
        {
            const string pre = "Enemies/bandido_machete";
            galope = Cuadros(pre + "_correr", 4);
            sablazo = lanzar = Cuadros(pre + "_ataque", 3);
            golpe = Cuadros(pre + "_golpe", 1);
            muerte = Cuadros(pre + "_muerte", 4);
        }

        AnimationClip reposo = CrearClip("Jinete/Jinete_Reposo", new[] { galope[0] }, 1f, true);
        AnimationClip cGalope = CrearClip("Jinete/Jinete_Galope", galope, 10f, true);
        AnimationClip cSablazo = CrearClip("Jinete/Jinete_Sablazo", sablazo, 9f, false);
        AnimationClip cLanzar = CrearClip("Jinete/Jinete_Lanzar", lanzar, 9f, false);
        AnimationClip cGolpe = CrearClip("Jinete/Jinete_Golpe", golpe, 1f, true);
        AnimationClip cMuerte = CrearClip("Jinete/Jinete_Muerte", muerte, 2.5f, false);   // Se derrumba despacio

        const string ruta = "Assets/Art/Animations/Jinete/Jinete.controller";
        AssetDatabase.DeleteAsset(ruta);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        ctrl.AddParameter("Velocidad", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Muerto", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;
        AnimatorState sReposo = sm.AddState("Reposo"); sReposo.motion = reposo;
        AnimatorState sGalope = sm.AddState("Galope"); sGalope.motion = cGalope;
        sm.AddState("Sablazo").motion = cSablazo;
        sm.AddState("Lanzar").motion = cLanzar;
        sm.AddState("Golpe").motion = cGolpe;
        AnimatorState sMuerte = sm.AddState("Muerte"); sMuerte.motion = cMuerte;
        sm.defaultState = sReposo;

        Transicion(sReposo, sGalope).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Velocidad");
        Transicion(sGalope, sReposo).AddCondition(AnimatorConditionMode.Less, 0.1f, "Velocidad");
        Transicion(sm, sMuerte).AddCondition(AnimatorConditionMode.If, 0, "Muerto");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static JefeJinete CrearPrefabJinete(BalaEnemiga calavera)
    {
        RuntimeAnimatorController ctrl = CrearAnimacionesJinete();
        Directory.CreateDirectory("Assets/Prefabs/Bosses");
        var go = new GameObject("Jinete") { layer = CapaEnemy };
        if (!HayArteJinete) go.transform.localScale = new Vector3(1.3f, 1.3f, 1f);

        var visual = new GameObject("Visual") { layer = CapaEnemy };
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = HayArteJinete ? Spr("Bosses/jinete_galope_00.png") : Spr("Enemies/bandido_machete_correr_00.png");
        sr.sortingOrder = 6;
        if (!HayArteJinete) sr.color = new Color(0.55f, 0.45f, 0.7f);   // Provisional: un bandido "fantasma"
        visual.AddComponent<Animator>().runtimeAnimatorController = ctrl;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.useFullKinematicContacts = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Caballo y torso del jinete: a esta altura el salto de Mateo alcanza a librarlo en las cargas
        float escala = go.transform.localScale.y;
        Vector2 tam = sr.sprite.bounds.size;
        col.size = new Vector2(tam.x * 0.7f, 2.4f / escala);
        col.offset = new Vector2(0f, 1.2f / escala);

        var salud = go.AddComponent<Health>();
        var soS = new SerializedObject(salud);
        soS.FindProperty("vidaMaxima").intValue = 500;          // Más que el alebrije (400): es el segundo jefe
        soS.FindProperty("destruirAlMorir").boolValue = false;
        soS.ApplyModifiedPropertiesWithoutUndo();
        Asignar(go.AddComponent<ParpadeoDano>(), "sprite", sr);
        var basico = go.AddComponent<EnemigoBasico>();
        var soB = new SerializedObject(basico);
        soB.FindProperty("sonidoMuerte").enumValueIndex = (int)Sfx.RugidoAlebrije;
        soB.FindProperty("puntos").intValue = 3000;
        soB.ApplyModifiedPropertiesWithoutUndo();

        var dano = go.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 18;
        soD.FindProperty("empuje").floatValue = 10f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var jefe = go.AddComponent<JefeJinete>();
        var so = new SerializedObject(jefe);
        so.FindProperty("animator").objectReferenceValue = visual.GetComponent<Animator>();
        so.FindProperty("danoContacto").objectReferenceValue = dano;
        so.FindProperty("prefabCalavera").objectReferenceValue = calavera;
        so.FindProperty("capaJugador").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, RutaPrefabJinete).GetComponent<JefeJinete>();
    }

    /// <summary>Jinete dormido fuera de la pantalla y el disparador de su cinemática al entrar al patio.</summary>
    static EncuentroJinete CrearEncuentroJinete(float inicioArena, float derecha, BarraJefe barra, Transform enemigos)
    {
        BalaEnemiga calavera = Cargar<BalaEnemiga>("Assets/Prefabs/Projectiles/Bola_Fuego.prefab");
        JefeJinete prefab = CrearPrefabJinete(calavera);
        var jinete = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, enemigos);
        jinete.transform.position = new Vector3(derecha + 3f, YSuelo, 0f);

        var go = new GameObject("Encuentro_Jinete");
        go.transform.position = new Vector3(inicioArena + 2f, YSuelo + 2f, 0f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 8f);
        var enc = go.AddComponent<EncuentroJinete>();
        var so = new SerializedObject(enc);
        so.FindProperty("jefe").objectReferenceValue = jinete.GetComponent<JefeJinete>();
        so.FindProperty("barraJefe").objectReferenceValue = barra;
        so.FindProperty("xAparicion").floatValue = derecha + 3f;
        so.FindProperty("xLlegada").floatValue = derecha - 5f;
        so.FindProperty("ySuelo").floatValue = YSuelo;
        so.ApplyModifiedPropertiesWithoutUndo();
        return enc;
    }
}
