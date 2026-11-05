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
/// Pruebas de la issue #63: muro agrietado que se rompe y abre el paso, vigas altas, tiro de mina al cuarto secreto
/// (con cofre) y escalera de regreso, 3 exvotos por nivel que se guardan y cuentan en la victoria (SECRETOS n/3).
/// </summary>
public class SecretosTests
{
    Keyboard teclado;
    PlayerController jugador;
    Rigidbody2D rb;

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
        InputSystem.RemoveDevice(teclado);
        GestorGuardado.Borrar();
    }

    IEnumerator Cargar(string escena)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        if (escena == Escenas.Nivel2) yield return EsperaLlegada.Terminar();
        jugador = Object.FindFirstObjectByType<PlayerController>();
        rb = jugador.GetComponent<Rigidbody2D>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
    }

    IEnumerator MoverA(Vector2 posicion, float espera = 0.4f)
    {
        rb.position = posicion;
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(espera);
    }

    IEnumerator Pulsar(Key tecla)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    [UnityTest]
    public IEnumerator Nivel1_ElMuroTapaElPasoHastaQueSeRompe()
    {
        yield return Cargar(Escenas.Nivel1);
        var muro = Object.FindObjectsByType<Destructible>().First(d => d.name.StartsWith("Muro_Agrietado"));
        var exvoto = Object.FindObjectsByType<Exvoto>().First(e => e.Id == 0);
        Assert.Less(exvoto.transform.position.x, muro.transform.position.x, "El exvoto está detrás del muro");

        // Caminando a la izquierda se topa con el muro
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.A));
        yield return new WaitForSeconds(1.2f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        Assert.Greater(jugador.transform.position.x, muro.transform.position.x, "El muro no deja pasar");
        Assert.AreEqual(0, GestorGuardado.ExvotosEncontrados(1));

        muro.GetComponent<Health>().RecibirDano(9999);
        yield return null;
        Assert.IsTrue(muro == null, "El muro se rompe a golpes");
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.A));
        yield return new WaitForSeconds(1.5f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        Assert.AreEqual(1, GestorGuardado.ExvotosEncontrados(1), "Detrás estaba el exvoto");
    }

    [UnityTest]
    public IEnumerator Nivel1_LasVigasSostienenAMateoArriba()
    {
        yield return Cargar(Escenas.Nivel1);
        var vigas = Object.FindObjectsByType<PlatformEffector2D>().Select(e => e.transform).OrderBy(t => t.position.y).ToArray();
        Assert.GreaterOrEqual(vigas.Length, 2, "Viga baja y viga alta");
        var alta = vigas.Last();
        float superficie = alta.GetComponent<BoxCollider2D>().bounds.max.y;
        yield return MoverA(new Vector2(alta.position.x, superficie + 0.5f), 0.8f);
        Assert.IsTrue(jugador.EstaEnSuelo, "Se para encima de la viga");
        Assert.AreEqual(superficie, jugador.transform.position.y, 0.2f);
        var exvoto = Object.FindObjectsByType<Exvoto>().FirstOrDefault(e => e.Id == 1);
        Assert.IsTrue(exvoto == null, "El exvoto de la viga alta se recoge al llegar");
        Assert.AreEqual(1, GestorGuardado.ExvotosEncontrados(1));
    }

    [UnityTest]
    public IEnumerator Nivel2_TiroDeMinaBajaAlCuartoYLaEscaleraRegresa()
    {
        yield return Cargar(Escenas.Nivel2);
        var pasos = Object.FindObjectsByType<PasoSecreto>();
        var tiro = pasos.First(p => p.name == "Tiro_Mina");
        var escalera = pasos.First(p => p.name == "Escalera_Cuarto");
        var cam = Camera.main.transform;

        yield return MoverA(new Vector2(tiro.transform.position.x, tiro.transform.position.y + 0.1f));
        yield return Pulsar(Key.DownArrow);
        yield return new WaitForSeconds(1f);
        Assert.Greater(jugador.transform.position.x, escalera.transform.position.x - 2f, "Bajó al cuarto");
        // El cuarto mide una pantalla en 16:9; en pantallas más angostas la cámara se mueve dentro de él sin salirse
        float holgura = 21.5f / 2f - Camera.main.GetComponent<CameraFollow>().MitadAncho;
        Assert.LessOrEqual(Mathf.Abs(cam.position.x - escalera.transform.parent.position.x), holgura + 0.1f, "La cámara se queda en el cuarto");

        var cofre = Object.FindFirstObjectByType<Cofre>();
        var inventario = jugador.GetComponent<InventarioJugador>();
        int recogiblesAntes = Object.FindObjectsByType<Recogible>().Length, monedasAntes = inventario.Monedas;
        yield return MoverA(cofre.transform.position + Vector3.up * 0.1f);
        Assert.IsTrue(cofre.Abierto, "El cofre se abre al tocarlo");
        // El botín sale encima de Mateo: o sigue flotando o ya lo recogió
        Assert.IsTrue(Object.FindObjectsByType<Recogible>().Length > recogiblesAntes || inventario.Monedas > monedasAntes, "y suelta su botín");

        yield return MoverA(escalera.transform.position + Vector3.up * 0.1f);
        yield return Pulsar(Key.UpArrow);
        yield return new WaitForSeconds(1f);
        Assert.Less(Mathf.Abs(jugador.transform.position.x - tiro.transform.position.x), 3f, "Regresó junto al tiro");
        Assert.Less(cam.position.x, tiro.transform.position.x + 15f, "La cámara vuelve al recorrido");
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual);
    }

    [UnityTest]
    public IEnumerator CadaNivelTieneTresExvotos_SeGuardanYSeCuentanEnLaVictoria()
    {
        yield return Cargar(Escenas.Nivel2);
        Assert.AreEqual(3, Object.FindObjectsByType<Exvoto>().Length, "3 exvotos en Pozos");
        Assert.AreEqual(2, Object.FindObjectsByType<Destructible>().Count(d => d.name.StartsWith("Muro_Agrietado")) +
                           Object.FindObjectsByType<PasoSecreto>().Count(p => p.name == "Tiro_Mina"), "Muro y tiro de mina en Pozos");

        yield return Cargar(Escenas.Nivel1);
        var exvotos = Object.FindObjectsByType<Exvoto>().OrderBy(e => e.Id).ToArray();
        Assert.AreEqual(3, exvotos.Length, "3 exvotos en Dolores");
        yield return MoverA(exvotos[2].transform.position, 0.3f);           // El de los huacales apilados
        Assert.AreEqual(1, GestorGuardado.ExvotosEncontrados(1));

        GestorNivel.Actual.Completar();
        yield return new WaitForSeconds(0.6f);
        var resumen = Object.FindObjectsByType<Text>().First(t => t.name == "Resumen" && t.gameObject.activeInHierarchy);
        StringAssert.Contains("SECRETOS  1/3", resumen.text);

        // La partida lo recuerda: al volver, ese exvoto se ve translúcido y el conteo sigue en 1
        GestorGuardado.Cargar();
        Assert.IsTrue(GestorGuardado.TieneExvoto(1, 2));
        yield return Cargar(Escenas.Nivel1);
        var repetido = Object.FindObjectsByType<Exvoto>().First(e => e.Id == 2);
        Assert.Less(repetido.GetComponentInChildren<SpriteRenderer>().color.a, 1f);
        yield return MoverA(repetido.transform.position, 0.3f);
        Assert.AreEqual(1, GestorGuardado.ExvotosEncontrados(1), "No se cuenta dos veces");
    }

    [Test]
    public void Sonidos_ExvotoYCofre()
    {
        var biblioteca = Resources.Load<BibliotecaAudio>("BibliotecaAudio");
        Assert.IsTrue(biblioteca.efectos.Any(e => e.id == Sfx.Exvoto && e.clips.Length > 0));
        Assert.IsTrue(biblioteca.efectos.Any(e => e.id == Sfx.AbrirCofre && e.clips.Length > 0));
    }
}
