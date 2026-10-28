using UnityEditor;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// Prefabs de la Bruja de Fuego (#54) y de su bola de fuego. Los usa el Nivel 2 (ConstructorNivel2).
/// Cuadros recortados de la hoja de Flow (Docs/Arte/Flow/bruja_fuego_flow.jpg); el arte mira a la izquierda.
/// </summary>
public static partial class ConstructorJuego
{
    const string RutaPrefabBruja = "Assets/Prefabs/Enemies/Bruja_Fuego.prefab";

    static IABruja CrearPrefabBruja()
    {
        BalaEnemiga bola = CrearBolaFuego();

        var go = new GameObject("Bruja_Fuego") { layer = CapaEnemy };
        var visual = new GameObject("Visual") { layer = CapaEnemy };
        visual.transform.SetParent(go.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Enemies/bruja_volar_00.png");
        sr.sortingOrder = 6;                                   // Delante de los enemigos del suelo
        var anim = visual.AddComponent<Animator>();
        anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Art/Animations/Bruja/Bruja.controller");

        go.AddComponent<Rigidbody2D>().interpolation = RigidbodyInterpolation2D.Interpolate;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1.6f, 1.1f);                    // El cuerpo y la capucha, sin la cola de fuego
        col.offset = new Vector2(-0.45f, 0.1f);

        var salud = go.AddComponent<Health>();
        var soS = new SerializedObject(salud);
        soS.FindProperty("vidaMaxima").intValue = 35;
        soS.FindProperty("destruirAlMorir").boolValue = false;   // Primero cae y se hace cenizas
        soS.ApplyModifiedPropertiesWithoutUndo();
        Asignar(go.AddComponent<ParpadeoDano>(), "sprite", sr);
        var basico = go.AddComponent<EnemigoBasico>();
        var soB = new SerializedObject(basico);
        soB.FindProperty("sonidoMuerte").enumValueIndex = (int)Sfx.Golpe;
        soB.FindProperty("puntos").intValue = 200;
        soB.ApplyModifiedPropertiesWithoutUndo();

        var dano = go.AddComponent<DanoPorContacto>();
        var soD = new SerializedObject(dano);
        soD.FindProperty("dano").intValue = 12;
        soD.FindProperty("empuje").floatValue = 6f;
        soD.FindProperty("capasObjetivo").FindPropertyRelative("m_Bits").intValue = 1 << CapaPlayer;
        soD.ApplyModifiedPropertiesWithoutUndo();

        var ia = go.AddComponent<IABruja>();
        var so = new SerializedObject(ia);
        so.FindProperty("prefabBola").objectReferenceValue = bola;
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("ySuelo").floatValue = YSuelo;
        so.FindProperty("alturaCadaver").floatValue = sr.sprite.bounds.size.y / 2f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return Guardar(go, RutaPrefabBruja).GetComponent<IABruja>();
    }

    /// <summary>Bola de fuego: una BalaEnemiga más (se recicla y solo choca con Mateo y el suelo).</summary>
    static BalaEnemiga CrearBolaFuego()
    {
        var go = new GameObject("Bola_Fuego") { layer = CapaProyectilEnemigo };
        // El sprite va con la cabeza a la izquierda; volteado, la cabeza queda hacia "transform.right", que es hacia
        // donde BalaEnemiga apunta la bala
        var visual = new GameObject("Visual") { layer = CapaProyectilEnemigo };
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = new Vector3(-1f, 1f, 1f);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Enemies/bruja_bola_00.png");
        sr.sortingOrder = 20;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.22f;
        col.offset = new Vector2(sr.sprite.bounds.extents.x * 0.6f, 0f);   // En la cabeza de la bola, no en la estela
        var bala = go.AddComponent<BalaEnemiga>();
        var so = new SerializedObject(bala);
        so.FindProperty("tiempoVida").floatValue = 3f;
        so.ApplyModifiedPropertiesWithoutUndo();
        return Guardar(go, "Assets/Prefabs/Projectiles/Bola_Fuego.prefab").GetComponent<BalaEnemiga>();
    }
}
