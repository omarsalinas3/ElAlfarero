using UnityEditor;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// El Alebrije liberado como montura de Mateo (#50). Usa los mismos cuadros y Animator del jefe, más chico para que
/// Mateo quepa encima. Sin collider propio: va pegado a Mateo y los golpes que recibe Mateo se los lleva él (Health.Protector).
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
        // Mateo se sienta a media altura del lomo
        so.FindProperty("asiento").vector2Value = new Vector2(0.15f, sr.sprite.bounds.size.y * EscalaMontura * 0.42f);
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, RutaPrefabMontura).GetComponent<MonturaAlebrije>();
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
