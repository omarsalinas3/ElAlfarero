using UnityEditor;
using UnityEngine;
using static ConfigurarSprint1;

/// <summary>
/// Armas de alfarería de la Unidad 1 (#33): Barro Vidriado (abanico de 5), Barro Negro (granada en arco que explota) y
/// Maceta Escudo (detiene 5 balas de frente). Arma sus CeramicaData, proyectiles, la explosión, las cajas que las dan
/// (dentro de huacales), y le pone a Mateo el ArsenalJugador y la maceta (EscudoMaceta). La Talavera la sigue armando
/// ConfigurarSprint1. Arte: hojas de Flow de la carpeta "Modelos" (Docs/Arte/Flow/arma_*_flow.jpg).
/// </summary>
public static partial class ConstructorJuego
{
    const string CarpetaCeramicas = "Assets/ScriptableObjects/Ceramicas/";

    static void CrearArmas(Prefabs p, Sprite[] huacal, SpriteAnimadoTemporal restosHuacal, SpriteAnimadoTemporal restosMaceta,
                           SpriteAnimadoTemporal chispa)
    {
        var talavera = AssetDatabase.LoadAssetAtPath<CeramicaData>(CarpetaCeramicas + "Ceramica_Talavera.asset");
        var impacto = AssetDatabase.LoadAssetAtPath<SpriteAnimadoTemporal>("Assets/Prefabs/Projectiles/Impacto_Talavera.prefab");
        int capasImpacto = (1 << CapaGround) | (1 << CapaEnemy);

        // Barro Vidriado: cinco jarritos en abanico, de poco alcance (para grupos cercanos)
        CeramicaData vidriado = NuevaCeramica("Ceramica_BarroVidriado", "Barro Vidriado", CeramicaData.TipoCeramica.BarroVidriado);
        vidriado.icono = Spr("Projectiles/vidriado_00.png");
        vidriado.prefabProyectil = PrefabProyectil("Proyectil_Vidriado", Spr("Projectiles/vidriado_01.png"), 0.2f, 0f);
        vidriado.prefabImpacto = impacto;
        vidriado.dano = 8;
        vidriado.velocidadProyectil = 15f;
        vidriado.tiempoVida = 0.6f;
        vidriado.capasImpacto = capasImpacto;
        vidriado.disparosPorSegundo = 2.5f;
        vidriado.proyectilesPorDisparo = 5;
        vidriado.aperturaAbanico = 40f;
        vidriado.dispersionGrados = 2f;
        vidriado.sonidoLanzar = Sfx.LanzarVidriado;
        vidriado.municionMaxima = 60;
        vidriado.municionPorCaja = 20;

        // Barro Negro: granada en arco; explota al tocar suelo o enemigo (o al acabarse la mecha)
        SpriteAnimadoTemporal explosion = Efecto("Explosion_BarroNegro", Cuadros("Projectiles/negro_explosion", 3), 10f, false, 1f);
        CeramicaData negro = NuevaCeramica("Ceramica_BarroNegro", "Barro Negro", CeramicaData.TipoCeramica.BarroNegro);
        negro.icono = Spr("Projectiles/negro_bomba_00.png");
        Sprite bomba = Spr("Projectiles/negro_bomba_00.png");
        negro.prefabProyectil = PrefabProyectil("Proyectil_BarroNegro", bomba, 0.28f, bomba.bounds.extents.x * 0.3f);   // La bola va al frente, el humo atrás
        negro.dano = 0;
        negro.velocidadProyectil = 11f;
        negro.tiempoVida = 1.8f;
        negro.capasImpacto = capasImpacto;
        negro.disparosPorSegundo = 1.2f;
        negro.dispersionGrados = 0f;
        negro.gravedad = 20f;
        negro.elevacionGrados = 35f;
        negro.radioExplosion = 2.2f;
        negro.danoExplosion = 45;                       // Tumba a cualquier bandido de un golpe
        negro.prefabExplosion = explosion;
        negro.municionMaxima = 15;
        negro.municionPorCaja = 5;

        // Maceta Escudo: no se lanza; su "munición" son los golpes que aguanta
        CeramicaData maceta = NuevaCeramica("Ceramica_MacetaEscudo", "Maceta Escudo", CeramicaData.TipoCeramica.MacetaEscudo);
        maceta.icono = Spr("Obstacles/maceta.png");
        maceta.municionMaxima = 5;
        maceta.municionPorCaja = 5;
        foreach (var c in new[] { vidriado, negro, maceta }) EditorUtility.SetDirty(c);

        // Cajas (recogibles) y los huacales que las sueltan
        GameObject cajaVidriado = CajaArma("Caja_Vidriado", vidriado, Cuadros("Items/caja_vidriado", 3), chispa);
        GameObject cajaNegro = CajaArma("Caja_BarroNegro", negro, new[] { Spr("Items/caja_negro_00.png") }, chispa);
        GameObject cajaMaceta = CajaArma("Caja_Maceta", maceta, new[] { Spr("Items/caja_maceta_00.png") }, chispa);
        p.huacalVidriado = Rompible("Huacal_Vidriado", huacal, 30, 1f, restosHuacal, Sfx.RomperHuacal, new[] { cajaVidriado }, 0.85f, 0.9f);
        p.huacalNegro = Rompible("Huacal_BarroNegro", huacal, 30, 1f, restosHuacal, Sfx.RomperHuacal, new[] { cajaNegro }, 0.85f, 0.9f);
        p.huacalMaceta = Rompible("Huacal_Maceta", huacal, 30, 1f, restosHuacal, Sfx.RomperHuacal, new[] { cajaMaceta }, 0.85f, 0.9f);

        PonerArsenal(new[] { talavera, vidriado, negro, maceta }, restosMaceta);
    }

    static CeramicaData NuevaCeramica(string archivo, string nombre, CeramicaData.TipoCeramica tipo)
    {
        string ruta = CarpetaCeramicas + archivo + ".asset";
        AssetDatabase.DeleteAsset(ruta);
        var c = ScriptableObject.CreateInstance<CeramicaData>();
        c.nombre = nombre;
        c.tipo = tipo;
        AssetDatabase.CreateAsset(c, ruta);
        return c;
    }

    static Projectile PrefabProyectil(string nombre, Sprite sprite, float radio, float adelante)
    {
        var go = new GameObject(nombre) { layer = CapaProjectile };
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 20;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = radio;
        col.offset = new Vector2(adelante, 0f);
        go.AddComponent<Projectile>();
        return Guardar(go, "Assets/Prefabs/Projectiles/" + nombre + ".prefab").GetComponent<Projectile>();
    }

    static GameObject CajaArma(string nombre, CeramicaData arma, Sprite[] cuadros, SpriteAnimadoTemporal chispa)
    {
        GameObject caja = Recogible(nombre, TipoRecogible.Arma, arma.municionPorCaja, cuadros, 1f, chispa);
        var r = caja.GetComponent<Recogible>();
        var so = new SerializedObject(r);
        so.FindProperty("arma").objectReferenceValue = arma;
        so.FindProperty("cuadrosPorSegundo").floatValue = 6f;
        so.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.SavePrefabAsset(caja);
        return caja;
    }

    /// <summary>Le pone a Mateo su arsenal y la maceta que sostiene al frente (oculta hasta que se equipa).</summary>
    static void PonerArsenal(CeramicaData[] armas, SpriteAnimadoTemporal restosMaceta)
    {
        GameObject raiz = PrefabUtility.LoadPrefabContents(RutaPrefabAlfarero);
        var arsenal = raiz.GetComponent<ArsenalJugador>();
        if (arsenal == null) arsenal = raiz.AddComponent<ArsenalJugador>();
        var so = new SerializedObject(arsenal);
        ArregloObjetos(so.FindProperty("armas"), armas);
        so.FindProperty("tirador").objectReferenceValue = raiz.GetComponent<PlayerShooter>();
        so.ApplyModifiedPropertiesWithoutUndo();

        Transform viejo = raiz.transform.Find("Escudo");
        if (viejo != null) Object.DestroyImmediate(viejo.gameObject);
        var escudoGo = new GameObject("Escudo") { layer = CapaPlayer };
        escudoGo.transform.SetParent(raiz.transform, false);
        escudoGo.transform.localPosition = new Vector3(0.55f, 0f, 0f);     // Al frente, apoyada en el suelo
        var visual = new GameObject("Maceta") { layer = CapaPlayer };
        visual.transform.SetParent(escudoGo.transform, false);
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Spr("Player/escudo_maceta_00.png");
        sr.sortingOrder = 11;                                                // Delante de Mateo
        sr.enabled = false;
        var escudo = escudoGo.AddComponent<EscudoMaceta>();
        var soE = new SerializedObject(escudo);
        soE.FindProperty("arsenal").objectReferenceValue = arsenal;
        soE.FindProperty("jugador").objectReferenceValue = raiz.GetComponent<PlayerController>();
        soE.FindProperty("sprite").objectReferenceValue = sr;
        ArregloObjetos(soE.FindProperty("estados"), Cuadros("Player/escudo_maceta", 4));
        soE.FindProperty("prefabRestos").objectReferenceValue = restosMaceta;
        soE.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(raiz, RutaPrefabAlfarero);
        PrefabUtility.UnloadPrefabContents(raiz);
    }
}
