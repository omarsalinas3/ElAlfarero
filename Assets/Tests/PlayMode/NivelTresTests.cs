using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas del Nivel 3 (Panteón junto al río, #58 y #59): fondos en tramos, se llega desde la victoria de Pozos, más
/// difícil que Pozos, 3 exvotos, y la pelea con La Llorona (cinemática, ataques, furia y victoria guardada).
/// El nivel también se completa de principio a fin.
/// </summary>
public class NivelTresTests
{
    Keyboard teclado;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        InputSystem.RemoveDevice(teclado);
        GestorGuardado.Borrar();
    }

    IEnumerator CargarNivel3()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel3, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
    }

    /// <summary>Lleva a Mateo a la orilla y espera a que termine la cinemática de La Llorona.</summary>
    IEnumerator EmpezarPelea(EncuentroLlorona encuentro, PlayerController jugador)
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 pos = new Vector2(encuentro.transform.position.x, rb.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        float limite = Time.time + 12f;
        while (!encuentro.PeleaEmpezada && Time.time < limite) yield return null;
        Assert.IsTrue(encuentro.PeleaEmpezada, "Llegar a la orilla despierta a La Llorona");
    }

    [UnityTest]
    public IEnumerator Nivel3_CincoTramosUnidosSinHuecos()
    {
        yield return CargarNivel3();
        var fondos = GameObject.Find("Fondo").GetComponentsInChildren<SpriteRenderer>().OrderBy(s => s.transform.position.x).ToArray();
        Assert.AreEqual(5, fondos.Length, "El panteón se arma con 5 tramos");
        foreach (var f in fondos) StringAssert.StartsWith("lvl3_", f.sprite.name);
        for (int i = 1; i < fondos.Length; i++)
            Assert.Less(fondos[i].bounds.min.x, fondos[i - 1].bounds.max.x, $"El tramo {i + 1} se traslapa con el anterior (degradado)");
        Assert.Greater(fondos.Last().bounds.max.x - fondos.First().bounds.min.x, 80f, "Mide al menos lo que Dolores");
    }

    [UnityTest]
    public IEnumerator Nivel3_MateoEnElSueloYCamaraDentroDelNivel()
    {
        yield return CargarNivel3();
        var mateo = Object.FindAnyObjectByType<PlayerController>();
        float limite = Time.time + 2f;
        while (!mateo.EstaEnSuelo && Time.time < limite) yield return null;
        Assert.IsTrue(mateo.EstaEnSuelo, "Hay suelo bajo Mateo");
        Assert.IsNotNull(Camera.main.GetComponent<CameraFollow>(), "La cámara lo sigue");
        Assert.IsNotNull(GameObject.Find("Muro_Izquierdo"));
        Assert.IsNotNull(GameObject.Find("Muro_Derecho"));
        Assert.AreEqual(3, GestorNivel.Actual.NumeroNivel);
    }

    [UnityTest]
    public IEnumerator Llorona_TieneSusCincoAnimaciones()
    {
        yield return CargarNivel3();
        var llorona = Object.FindObjectsByType<JefeLlorona>(FindObjectsInactive.Include).First();
        Assert.IsFalse(llorona.gameObject.activeInHierarchy, "Nadie la ve hasta la cinemática");
        llorona.gameObject.SetActive(true);
        var animator = llorona.GetComponentInChildren<Animator>();
        Assert.IsNotNull(animator.runtimeAnimatorController);
        var clips = animator.runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();
        foreach (var c in new[] { "Llorona_Flotar", "Llorona_Grito", "Llorona_Agua", "Llorona_Golpe", "Llorona_Desvanecer" })
            CollectionAssert.Contains(clips, c);

        var sr = llorona.GetComponentInChildren<SpriteRenderer>();
        yield return new WaitForSeconds(0.2f);
        StringAssert.StartsWith("llorona_flotar", sr.sprite.name, "Empieza flotando");
        Assert.Greater(sr.bounds.size.y, 3f, "Es más alta que Mateo");
        animator.Play("Agua");
        yield return new WaitForSeconds(0.3f);
        StringAssert.StartsWith("llorona_agua", sr.sprite.name, "Ataque de agua");
    }

    [UnityTest]
    public IEnumerator Nivel2_LaVictoriaLlevaAlPanteon()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel2, LoadSceneMode.Single);
        yield return EsperaLlegada.Terminar();
        Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);
        GestorNivel.Actual.Completar();
        yield return new WaitForSeconds(0.6f);

        var siguiente = Object.FindObjectsByType<Button>().FirstOrDefault(b => b.name == "Siguiente");
        Assert.IsNotNull(siguiente, "La victoria de Pozos ofrece seguir al panteón");
        Assert.AreEqual(3, GestorGuardado.Datos.nivelDesbloqueado);
        siguiente.onClick.Invoke();
        float limite = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != Escenas.Nivel3 && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual(Escenas.Nivel3, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Nivel3_MasBrujasYOleadasQuePozosYTresExvotos()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel2, LoadSceneMode.Single);
        yield return null;
        int brujasPozos = Object.FindObjectsByType<IABruja>().Length;

        yield return CargarNivel3();
        Assert.Greater(Object.FindObjectsByType<IABruja>().Length, brujasPozos, "Más ataques desde el aire");
        Assert.IsNotNull(Object.FindFirstObjectByType<Emboscada>(), "Tiene emboscada");
        Assert.GreaterOrEqual(Object.FindObjectsByType<IAEsqueleto>().Length, 4);
        Assert.AreEqual(3, Object.FindObjectsByType<Exvoto>().Select(e => e.Id).Distinct().Count(), "3 exvotos");
        Assert.IsNull(GameObject.Find("Meta_Provisional"), "Llegar a la orilla ya no basta");
    }

    [UnityTest]
    public IEnumerator Llorona_AtacaConOndasYChorrosYLastimaAMateo()
    {
        yield return CargarNivel3();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        var encuentro = Object.FindFirstObjectByType<EncuentroLlorona>();
        yield return EmpezarPelea(encuentro, jugador);
        var jefa = Object.FindFirstObjectByType<JefeLlorona>();
        Assert.IsTrue(Object.FindObjectsByType<BarraJefe>().Any(), "Sale su barra de vida");

        bool vioOnda = false, vioChorro = false, gritoEstado = false, aguaEstado = false;
        int vidaInicial = salud.VidaActual;
        for (float t = 0f; t < 15f && !(vioOnda && vioChorro); t += Time.deltaTime)
        {
            gritoEstado |= jefa.EstadoActual == JefeLlorona.Estado.Grito;
            aguaEstado |= jefa.EstadoActual == JefeLlorona.Estado.Agua;
            vioOnda |= Object.FindObjectsByType<BalaEnemiga>().Any(b => b.name.StartsWith("Onda_Sonica"));
            vioChorro |= Object.FindObjectsByType<ChorroAgua>().Any();
            yield return null;
        }
        Assert.IsTrue(gritoEstado && vioOnda, "Grita y lanza la onda sónica");
        Assert.IsTrue(aguaEstado && vioChorro, "Saca chorros de agua");
        yield return new WaitForSeconds(2f);
        Assert.Less(salud.VidaActual, vidaInicial, "Sus ataques lastiman a Mateo si se queda quieto");
    }

    [UnityTest]
    public IEnumerator Llorona_SeDesvaneceSiMateoSeLePega()
    {
        yield return CargarNivel3();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        yield return EmpezarPelea(Object.FindFirstObjectByType<EncuentroLlorona>(), jugador);
        var jefa = Object.FindFirstObjectByType<JefeLlorona>();

        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 pegado = new Vector2(jefa.transform.position.x - 1.5f, rb.position.y + 0.1f);
        rb.position = pegado;
        jugador.transform.position = pegado;
        bool desvanecida = false;
        for (float t = 0f; t < 6f && !desvanecida; t += Time.deltaTime)
        {
            desvanecida = jefa.EstadoActual == JefeLlorona.Estado.Desvanecida;
            yield return null;
        }
        Assert.IsTrue(desvanecida, "Pegado a ella se desvanece");
        yield return new WaitForSeconds(1f);
        Assert.Greater(Mathf.Abs(jefa.transform.position.x - jugador.transform.position.x), 4f, "y reaparece lejos");
    }

    [UnityTest]
    public IEnumerator Llorona_SeEnfureceALaMitadYAlVencerlaSeCompletaElNivel()
    {
        yield return CargarNivel3();
        var gestor = GestorNivel.Actual;
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        yield return EmpezarPelea(Object.FindFirstObjectByType<EncuentroLlorona>(), jugador);
        var jefa = Object.FindFirstObjectByType<JefeLlorona>();
        Assert.AreNotEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual);

        // Si justo se desvaneció no recibe daño: se espera a que se pueda golpear
        float limite = Time.time + 5f;
        while (jefa.Salud.EsInvulnerable && Time.time < limite) yield return null;
        jefa.Salud.RecibirDano(jefa.Salud.VidaMaxima / 2 + 1);
        yield return new WaitForSeconds(1.5f);
        Assert.IsTrue(jefa.Furiosa, "A la mitad de la vida se enfurece");

        limite = Time.time + 5f;
        while (jefa.Salud.EsInvulnerable && Time.time < limite) yield return null;
        jefa.Salud.RecibirDano(100000);
        yield return new WaitForSeconds(3.5f);
        Assert.AreEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual, "Liberarla completa el nivel");
        Assert.IsNotNull(GestorGuardado.Datos.Record(3));
        Assert.AreEqual(4, GestorGuardado.Datos.nivelDesbloqueado);
    }

    /// <summary>Espera a que la jefa empiece un ataque desde el principio y lo termine; deja en resultado cuántas ondas y chorros lanzó.</summary>
    IEnumerator MedirAtaque(JefeLlorona jefa, JefeLlorona.Estado ataque, int[] resultado)
    {
        float limite = Time.time + 12f;
        while (jefa.EstadoActual == ataque && Time.time < limite) yield return null;      // Si ya iba a la mitad, el siguiente
        while (jefa.EstadoActual != ataque && Time.time < limite) yield return null;
        Assert.AreEqual(ataque, jefa.EstadoActual, $"Llega a usar el ataque {ataque}");
        int ondas = jefa.OndasLanzadas, chorros = jefa.ChorrosLanzados;
        while (jefa.EstadoActual == ataque && Time.time < limite) yield return null;
        resultado[0] = jefa.OndasLanzadas - ondas;
        resultado[1] = jefa.ChorrosLanzados - chorros;
    }

    [UnityTest]
    public IEnumerator Llorona_AguantaElDobleQueElJinete()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel2, LoadSceneMode.Single);
        yield return null;
        int vidaJinete = Object.FindObjectsByType<JefeJinete>(FindObjectsInactive.Include).First().GetComponent<Health>().VidaMaxima;

        yield return CargarNivel3();
        var llorona = Object.FindObjectsByType<JefeLlorona>(FindObjectsInactive.Include).First();
        Assert.GreaterOrEqual(llorona.GetComponent<Health>().VidaMaxima, vidaJinete * 2,
            "La Talavera no la tumba en segundos: aguanta al menos el doble que el jefe anterior");
    }

    [UnityTest]
    [Timeout(90000)]
    public IEnumerator Llorona_FuriosaGritaDosVecesSusChorrosPersiguenYCambiaDeLado()
    {
        yield return CargarNivel3();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        yield return EmpezarPelea(Object.FindFirstObjectByType<EncuentroLlorona>(), jugador);
        var jefa = Object.FindFirstObjectByType<JefeLlorona>();

        // Mateo quieto a media orilla, para que ningún chorro quede fuera de la arena
        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 centro = new Vector2(GameObject.Find("Arena_Llorona").transform.position.x, rb.position.y + 0.1f);
        rb.position = centro;
        jugador.transform.position = centro;

        var r = new int[2];
        yield return MedirAtaque(jefa, JefeLlorona.Estado.Grito, r);
        Assert.AreEqual(1, r[0], "Tranquila grita una sola onda");
        yield return MedirAtaque(jefa, JefeLlorona.Estado.Agua, r);
        Assert.AreEqual(3, r[1], "Tranquila saca tres chorros");

        float limite = Time.time + 5f;
        while (jefa.Salud.EsInvulnerable && Time.time < limite) yield return null;
        jefa.Salud.RecibirDano(jefa.Salud.VidaMaxima / 2 + 1);
        limite = Time.time + 3f;
        while (!jefa.Furiosa && Time.time < limite) yield return null;
        Assert.IsTrue(jefa.Furiosa);

        yield return MedirAtaque(jefa, JefeLlorona.Estado.Grito, r);
        Assert.AreEqual(6, r[0], "Furiosa grita dos veces, tres ondas cada vez");
        yield return MedirAtaque(jefa, JefeLlorona.Estado.Agua, r);
        Assert.Greater(r[1], 5, "Furiosa saca cinco chorros y una segunda tanda donde quedó Mateo");

        bool desvanecida = false;
        for (float t = 0f; t < 1f && !desvanecida; t += Time.deltaTime)
        {
            desvanecida = jefa.EstadoActual == JefeLlorona.Estado.Desvanecida;
            yield return null;
        }
        Assert.IsTrue(desvanecida, "Tras el agua se esfuma para cambiar de lado");
    }

    [Test]
    public void Audio_MusicaYEfectosDelNivelYDeLaPelea()
    {
        var biblioteca = Resources.Load<BibliotecaAudio>("BibliotecaAudio");
        Assert.IsTrue(biblioteca.musicas.Any(m => m.id == Musica.Nivel3 && m.clip != null));
        Assert.IsTrue(biblioteca.musicas.Any(m => m.id == Musica.Llorona && m.clip != null));
        Assert.IsTrue(biblioteca.efectos.Any(e => e.id == Sfx.GritoLlorona && e.clips.Length > 0));
        Assert.IsTrue(biblioteca.efectos.Any(e => e.id == Sfx.ChorroAgua && e.clips.Length > 0));
    }

    /// <summary>Mateo invulnerable corre a la derecha; lo que se le pone enfrente se vence para no depender de la puntería.</summary>
    [UnityTest]
    [Timeout(240000)]
    public IEnumerator Nivel3_SePuedeCompletarDeInicioAFin()
    {
        yield return CargarNivel3();
        var gestor = GestorNivel.Actual;
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        var rb = jugador.GetComponent<Rigidbody2D>();
        var emboscada = Object.FindFirstObjectByType<Emboscada>();
        var encuentro = Object.FindFirstObjectByType<EncuentroLlorona>();
        Time.timeScale = 2f;

        bool peleo = false;
        float limite = Time.realtimeSinceStartup + 180f, siguienteRevision = 0f, xRevision = rb.position.x;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.RightArrow, Key.Space));
        while (gestor.EstadoActual != GestorNivel.Estado.Victoria && Time.realtimeSinceStartup < limite)
        {
            salud.HacerInvulnerable(5f);
            Assert.AreNotEqual(GestorNivel.Estado.GameOver, gestor.EstadoActual);
            peleo |= encuentro.PeleaEmpezada;
            foreach (var h in Object.FindObjectsByType<Health>())
            {
                if (h == salud || h.EstaMuerto) continue;
                float dx = h.transform.position.x - rb.position.x;
                bool estorba = h.GetComponent<Destructible>() != null && dx > 0f && dx < 3f;
                bool enemigo = h.gameObject.layer == LayerMask.NameToLayer("Enemy") && Mathf.Abs(dx) < 12f;
                if (estorba || enemigo) h.RecibirDano(100000);
            }
            if (Time.time >= siguienteRevision)
            {
                if (gestor.EstadoActual == GestorNivel.Estado.Jugando && rb.position.x - xRevision < 0.5f)
                {
                    Vector2 adelante = new Vector2(rb.position.x + 2f, rb.position.y + 1.5f);
                    rb.position = adelante;
                    jugador.transform.position = adelante;
                }
                xRevision = rb.position.x;
                siguienteRevision = Time.time + 1.5f;
            }
            yield return new WaitForSeconds(0.2f);
        }
        InputSystem.QueueStateEvent(teclado, new KeyboardState());

        Assert.IsTrue(emboscada.Terminada, "Pasó por la emboscada de las capillas");
        Assert.IsTrue(peleo, "Peleó con La Llorona");
        Assert.AreEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual, "El Nivel 3 se completa");
        Assert.AreEqual(4, GestorGuardado.Datos.nivelDesbloqueado, "y queda guardado");
    }
}
