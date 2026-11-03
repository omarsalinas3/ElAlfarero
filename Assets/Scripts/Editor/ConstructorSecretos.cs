using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static ConfigurarSprint1;

/// <summary>
/// Lugares secretos y exvotos (#63). Arte: Docs/Arte/Flow/objetos_secretos_flow.jpg y lvl2_cuarto_secreto_flow.jpg.
/// - Muro agrietado (Destructible) que esconde un exvoto detrás.
/// - Vigas altas de un solo sentido (se sube brincando desde abajo) con monedas y exvotos.
/// - Tiro de mina del Nivel 2: abajo encima baja a un cuarto subterráneo (en la misma escena, lejos a la derecha) con
///   cofre, otro muro y un exvoto; la escalera del cuarto regresa al recorrido.
/// Cada nivel tiene 3 exvotos (id 0..2); la victoria muestra SECRETOS n/3.
/// </summary>
public static partial class ConstructorJuego
{
    const float AnchoCuarto = 21.5f;          // Una pantalla, como cada tramo de fondo

    static void CrearPrefabsSecretos(Prefabs p, SpriteAnimadoTemporal chispa)
    {
        // Muro agrietado: sólido como los huacales; al romperse quedan los escombros (el sprite del muro roto)
        var restosMuro = Efecto("Restos_Muro", new[] { Spr("Obstacles/muro_agrietado_01.png") }, 1f, true, 1f);
        p.muroSecreto = Rompible("Muro_Agrietado", new[] { Spr("Obstacles/muro_agrietado_00.png") }, 60, 1f, restosMuro, Sfx.RomperHorno,
                                 new GameObject[0], 0.85f, 0.97f);

        // Viga de un solo sentido
        var viga = new GameObject("Viga") { layer = CapaGround };
        Sprite sViga = Spr("Obstacles/viga_00.png");
        var srViga = viga.AddComponent<SpriteRenderer>();
        srViga.sprite = sViga;
        srViga.sortingOrder = 2;
        var colViga = viga.AddComponent<BoxCollider2D>();
        colViga.size = new Vector2(sViga.bounds.size.x * 0.95f, 0.3f);
        colViga.offset = new Vector2(0f, sViga.bounds.size.y - 0.15f);
        colViga.usedByEffector = true;
        var efector = viga.AddComponent<PlatformEffector2D>();
        efector.useOneWay = true;
        efector.surfaceArc = 160f;
        p.viga = Guardar(viga, CarpetaObstaculos + "Viga.prefab");

        // Exvoto (detrás de los muros: se dibuja debajo de ellos)
        var exvoto = new GameObject("Exvoto");
        var colEx = exvoto.AddComponent<CircleCollider2D>();
        colEx.isTrigger = true;
        colEx.radius = 0.5f;
        var visual = new GameObject("Visual");
        visual.transform.SetParent(exvoto.transform, false);
        var srEx = visual.AddComponent<SpriteRenderer>();
        srEx.sprite = Spr("Items/exvoto_00.png");
        srEx.sortingOrder = 2;
        var ex = exvoto.AddComponent<Exvoto>();
        var soEx = new SerializedObject(ex);
        soEx.FindProperty("sprite").objectReferenceValue = srEx;
        soEx.FindProperty("efectoRecoger").objectReferenceValue = chispa;
        soEx.ApplyModifiedPropertiesWithoutUndo();
        p.exvoto = Guardar(exvoto, CarpetaRecogibles + "Exvoto.prefab");

        // Cofre del cuarto secreto
        var cofre = new GameObject("Cofre");
        Sprite sCofre = Spr("Obstacles/cofre_00.png");
        var srCofre = cofre.AddComponent<SpriteRenderer>();
        srCofre.sprite = sCofre;
        srCofre.sortingOrder = 3;
        var colCofre = cofre.AddComponent<BoxCollider2D>();
        colCofre.isTrigger = true;
        colCofre.size = sCofre.bounds.size;
        colCofre.offset = new Vector2(0f, sCofre.bounds.extents.y);
        var c = cofre.AddComponent<Cofre>();
        var soC = new SerializedObject(c);
        soC.FindProperty("sprite").objectReferenceValue = srCofre;
        soC.FindProperty("abierto").objectReferenceValue = Spr("Obstacles/cofre_01.png");
        ArregloObjetos(soC.FindProperty("botin"), new Object[]
        {
            p.moneda.GetComponent<Recogible>(), p.moneda.GetComponent<Recogible>(), p.guacamaya.GetComponent<Recogible>(),
            p.moneda.GetComponent<Recogible>(), p.moneda.GetComponent<Recogible>(),
        });
        soC.ApplyModifiedPropertiesWithoutUndo();
        p.cofre = Guardar(cofre, CarpetaObstaculos + "Cofre.prefab");
    }

    static GameObject PonerExvoto(Prefabs p, int id, float x, float y, Transform padre)
    {
        GameObject go = Poner(p.exvoto, x, y, padre);
        go.name = "Exvoto_" + id;
        var so = new SerializedObject(go.GetComponent<Exvoto>());
        so.FindProperty("id").intValue = id;
        so.ApplyModifiedPropertiesWithoutUndo();
        return go;
    }

    /// <summary>Viga cuya superficie queda a altoSobreSuelo del piso.</summary>
    static void PonerViga(Prefabs p, float x, float altoSobreSuelo, Transform padre)
    {
        float alto = Spr("Obstacles/viga_00.png").bounds.size.y;
        Poner(p.viga, x, YSuelo + altoSobreSuelo - alto, padre);
    }

    // ------------------------------------------------------------------ Nivel 1: 2 secretos y 3 exvotos
    static void PonerSecretosNivel1(Prefabs p, float altoHuacal, Transform obstaculos, Transform recogibles)
    {
        // 1) Muro agrietado a la izquierda del inicio: quien regresa a explorar encuentra el primer exvoto
        Poner(p.muroSecreto, -8.4f, YSuelo, obstaculos);
        PonerExvoto(p, 0, -10.35f, YSuelo + 0.9f, recogibles);

        // 2) Vigas sobre la plaza de la emboscada: monedas en la baja y un exvoto en la alta
        PonerViga(p, 47.5f, 2.4f, obstaculos);
        for (int i = 0; i < 3; i++) Poner(p.moneda, 46.8f + i * 0.7f, YSuelo + 3f, recogibles);
        PonerViga(p, 50.5f, 4.6f, obstaculos);
        PonerExvoto(p, 1, 50.5f, YSuelo + 5.4f, recogibles);

        // 3) Encima de los huacales apilados de la calle
        PonerExvoto(p, 2, 23f, YSuelo + altoHuacal * 2f + 0.8f, recogibles);
    }

    // ------------------------------------------------------------------ Nivel 2: tiro de mina, muro, vigas, 3 exvotos
    static void PonerSecretosNivel2(Prefabs p, float derecha, float altoHuacal, Transform obstaculos, Transform recogibles)
    {
        // 1) Tiro de mina entre los hornos de Santa Brígida → cuarto subterráneo con cofre, muro y exvoto
        const float xTiro = -4f;
        float centroCuarto = derecha + 40f;
        var salida = new GameObject("Salida_Tiro").transform;
        salida.position = new Vector3(xTiro + 1.6f, YSuelo + 0.02f, 0f);
        Transform llegada = CrearCuartoSecreto(p, centroCuarto, salida, recogibles);
        CrearPaso("Tiro_Mina", xTiro, Spr("Obstacles/tiro_mina_00.png"), false, llegada,
                  new Vector2(centroCuarto - AnchoCuarto / 2f, centroCuarto + AnchoCuarto / 2f), "BAJAR: ABAJO", obstaculos);

        // 2) Vigas sobre los mineros del tramo 1 con el segundo exvoto arriba
        PonerViga(p, 3.5f, 2.4f, obstaculos);
        for (int i = 0; i < 3; i++) Poner(p.moneda, 2.8f + i * 0.7f, YSuelo + 3f, recogibles);
        PonerViga(p, 0.5f, 4.6f, obstaculos);
        PonerExvoto(p, 1, 0.5f, YSuelo + 5.4f, recogibles);

        // 3) Encima de los huacales apilados antes de la emboscada
        PonerExvoto(p, 2, 30.5f, YSuelo + altoHuacal * 2f + 0.8f, recogibles);
    }

    /// <summary>Cuarto bajo el tiro: fondo propio, piso y paredes, cofre, muro con el exvoto 0 y escalera de regreso.</summary>
    static Transform CrearCuartoSecreto(Prefabs p, float centro, Transform salida, Transform recogibles)
    {
        var cuarto = new GameObject("Cuarto_Secreto").transform;
        cuarto.position = new Vector3(centro, 0f, 0f);
        var fondo = new GameObject("Fondo_Cuarto").AddComponent<SpriteRenderer>();
        fondo.transform.SetParent(cuarto, false);
        fondo.sprite = Spr("Environment/lvl2_cuarto_secreto.png");
        fondo.sortingOrder = -100;

        var suelo = new GameObject("Suelo_Cuarto") { layer = CapaGround };
        suelo.transform.SetParent(cuarto, false);
        suelo.transform.position = new Vector3(centro, YSuelo - 1f, 0f);
        suelo.AddComponent<BoxCollider2D>().size = new Vector2(AnchoCuarto, 2f);
        foreach (float lado in new[] { -1f, 1f })
        {
            var muro = new GameObject("Pared_Cuarto") { layer = CapaGround };
            muro.transform.SetParent(cuarto, false);
            muro.transform.position = new Vector3(centro + lado * (AnchoCuarto / 2f + 0.5f), 0f, 0f);
            muro.AddComponent<BoxCollider2D>().size = new Vector2(1f, 30f);
        }

        var llegada = new GameObject("Llegada_Cuarto").transform;
        llegada.SetParent(cuarto, false);
        llegada.position = new Vector3(centro - 5.4f, YSuelo + 0.02f, 0f);

        // La escalera ya viene dibujada en el fondo (a la izquierda): solo se pone su zona para subir
        CrearPaso("Escalera_Cuarto", centro - 6.6f, null, true, salida, Vector2.zero, "SUBIR: ARRIBA", cuarto);
        Poner(p.cofre, centro - 1.5f, YSuelo, cuarto);
        Poner(p.muroSecreto, centro + 3.8f, YSuelo, cuarto);
        PonerExvoto(p, 0, centro + 7.6f, YSuelo + 1.1f, recogibles);    // En el altar, detrás del muro
        return llegada;
    }

    static PasoSecreto CrearPaso(string nombre, float x, Sprite sprite, bool conArriba, Transform destino, Vector2 limites,
                                 string textoAviso, Transform padre)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(padre, false);
        go.transform.position = new Vector3(x, YSuelo, 0f);
        if (sprite != null)
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = -5;                                  // Detrás de Mateo y de los enemigos
        }
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1.6f, 3f);
        col.offset = new Vector2(0f, 1.5f);

        // Aviso flotante en el mundo (parpadea mientras Mateo está encima)
        var canvasGo = new GameObject("Aviso") { layer = 5 };
        canvasGo.transform.SetParent(go.transform, false);
        canvasGo.transform.localPosition = new Vector3(0f, 3.6f, 0f);
        canvasGo.transform.localScale = Vector3.one * 0.01f;
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 30;
        ((RectTransform)canvasGo.transform).sizeDelta = new Vector2(500f, 60f);
        Text aviso = Texto(canvasGo.transform, "Texto", textoAviso, 28, Dorado, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(500f, 60f));
        aviso.gameObject.AddComponent<TextoParpadeante>();

        var paso = go.AddComponent<PasoSecreto>();
        var so = new SerializedObject(paso);
        so.FindProperty("conArriba").boolValue = conArriba;
        so.FindProperty("destino").objectReferenceValue = destino;
        so.FindProperty("limitesCamara").vector2Value = limites;
        so.FindProperty("aviso").objectReferenceValue = canvasGo;
        so.ApplyModifiedPropertiesWithoutUndo();
        return paso;
    }
}
