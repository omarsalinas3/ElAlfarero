using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de integración de la issue #40 (sin errores críticos: cualquier error o excepción en la consola reprueba la
/// prueba). Completan lo que ya prueban GuardadoTests, MenuPrincipalTests, IAEsqueletoTests, OpcionesTests y NivelDosTests:
/// - Toda la partida (récords, nivel, monedas, armas y exvotos) sobrevive a cerrar y abrir el juego, y Continuar lleva
///   al último nivel aunque ya se hayan ganado todos.
/// - Los mineros del Nivel 2 también patrullan y persiguen.
/// - Calidad y volumen guardados se aplican al arrancar.
/// - El Nivel 2 se puede completar de principio a fin jugando (emboscada, estampida y Jinete incluidos).
/// </summary>
public class IntegracionTests
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

    [UnityTest]
    public IEnumerator Guardado_LaPartidaCompletaSobreviveAlReinicioYContinuarLlevaAlUltimoNivel()
    {
        GestorGuardado.RegistrarVictoria(1, 1200, 95f, 14);
        GestorGuardado.RegistrarVictoria(2, 3400, 210f, 20);
        GestorGuardado.DesbloquearArma("BarroNegro");
        GestorGuardado.RegistrarExvoto(1, 0);
        GestorGuardado.RegistrarExvoto(2, 2);

        GestorGuardado.Cargar();                                  // Como si se cerrara y abriera el juego: se lee del disco
        var d = GestorGuardado.Datos;
        Assert.AreEqual(3, d.nivelDesbloqueado);
        Assert.AreEqual(34, d.monedas, "Monedas de los dos niveles");
        Assert.AreEqual(3400, d.Record(2).puntajeMaximo);
        CollectionAssert.Contains(d.armasDesbloqueadas, "BarroNegro");
        Assert.IsTrue(GestorGuardado.TieneExvoto(1, 0) && GestorGuardado.TieneExvoto(2, 2));
        Assert.AreEqual(1, GestorGuardado.ExvotosEncontrados(2));

        // Ganado el último nivel que existe, Continuar lleva a ese (no a uno que no existe)
        yield return SceneManager.LoadSceneAsync(Escenas.Menu, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Enter));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return new WaitForSeconds(0.3f);
        Object.FindObjectsByType<Button>().First(b => b.name == "Continuar" && b.transform.parent.name == "PanelMenu").onClick.Invoke();
        float limite = Time.realtimeSinceStartup + 8f;
        while (SceneManager.GetActiveScene().name != Escenas.Nivel2 && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual(Escenas.Nivel2, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Nivel2_LosMinerosPatrullanYPersiguen()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel2, LoadSceneMode.Single);
        yield return EsperaLlegada.Terminar();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        var rb = jugador.GetComponent<Rigidbody2D>();

        // El minero de la calle (x 14.5) patrulla mientras Mateo está lejos, al inicio
        var minero = Object.FindObjectsByType<IAEsqueleto>().OrderBy(m => Mathf.Abs(m.transform.position.x - 14.5f)).First();
        float minX = float.MaxValue, maxX = float.MinValue;
        for (float t = 0f; t < 4f; t += Time.deltaTime)
        {
            minX = Mathf.Min(minX, minero.transform.position.x);
            maxX = Mathf.Max(maxX, minero.transform.position.x);
            yield return null;
        }
        Assert.AreEqual(IAEsqueleto.Estado.Patrullar, minero.EstadoActual);
        Assert.Greater(maxX - minX, 1f, "Camina de un lado a otro");

        // Con Mateo enfrente lo persigue
        Vector2 cerca = new Vector2(minero.transform.position.x - 3f, rb.position.y + 0.1f);
        rb.position = cerca;
        jugador.transform.position = cerca;
        yield return new WaitForSeconds(0.4f);
        Assert.That(minero.EstadoActual, Is.EqualTo(IAEsqueleto.Estado.Perseguir).Or.EqualTo(IAEsqueleto.Estado.Atacar));
        Assert.AreEqual(-1, minero.Direccion, "Voltea hacia Mateo");
    }

    [UnityTest]
    public IEnumerator Opciones_CalidadYVolumenGuardadosSeAplicanAlArrancar()
    {
        float musicaAntes = GestorAudio.VolumenMusica;
        try
        {
            PlayerPrefs.SetInt("graficos_calidad", 0);
            GestorAudio.VolumenMusica = 0.25f;
            PlayerPrefs.Save();
            // Lo que corre antes de cargar la primera escena al abrir el juego
            typeof(ConfiguracionGrafica).GetMethod("Cargar", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            yield return SceneManager.LoadSceneAsync(Escenas.Menu, LoadSceneMode.Single);

            Assert.AreEqual(0, ConfiguracionGrafica.Calidad);
            Assert.AreEqual("Low", QualitySettings.names[QualitySettings.GetQualityLevel()]);
            Assert.AreEqual(1, QualitySettings.vSyncCount);
            Assert.AreEqual(0.25f, GestorAudio.VolumenMusica, 0.001f);
        }
        finally
        {
            GestorAudio.VolumenMusica = musicaAntes;
            ConfiguracionGrafica.Restablecer();
        }
    }

    /// <summary>
    /// Mateo (invulnerable) corre a la derecha disparando; lo que se le pone enfrente se vence para no depender de la
    /// puntería. Pasa por la emboscada de 3 oleadas, la estampida y el Jinete, y el nivel debe terminar en victoria.
    /// </summary>
    [UnityTest]
    [Timeout(240000)]
    public IEnumerator Nivel2_SePuedeCompletarDeInicioAFin()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel2, LoadSceneMode.Single);
        yield return EsperaLlegada.Terminar();
        var gestor = GestorNivel.Actual;
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        var rb = jugador.GetComponent<Rigidbody2D>();
        var emboscada = Object.FindFirstObjectByType<Emboscada>();
        var encuentro = Object.FindFirstObjectByType<EncuentroJinete>();
        Time.timeScale = 2f;

        bool pasoEmboscada = false, peleoJinete = false;
        float limite = Time.realtimeSinceStartup + 180f, siguienteRevision = 0f, xRevision = rb.position.x;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.RightArrow, Key.Space));
        while (gestor.EstadoActual != GestorNivel.Estado.Victoria && Time.realtimeSinceStartup < limite)
        {
            salud.HacerInvulnerable(5f);
            Assert.AreNotEqual(GestorNivel.Estado.GameOver, gestor.EstadoActual);
            pasoEmboscada |= emboscada != null && emboscada.Terminada;
            peleoJinete |= encuentro != null && encuentro.PeleaEmpezada;

            // Vence lo que tenga cerca (enemigos y huacales que estorban); al Jinete, cuando ya empezó la pelea
            foreach (var h in Object.FindObjectsByType<Health>())
            {
                if (h == salud || h.EstaMuerto) continue;
                bool jefe = h.GetComponent<JefeJinete>() != null;
                if (jefe && !(encuentro.PeleaEmpezada && gestor.EstadoActual == GestorNivel.Estado.Jugando)) continue;
                bool estorba = h.GetComponent<Destructible>() != null && h.transform.position.x > rb.position.x && h.transform.position.x - rb.position.x < 3f;
                bool enemigo = h.gameObject.layer == LayerMask.NameToLayer("Enemy") && Mathf.Abs(h.transform.position.x - rb.position.x) < 12f;
                if (jefe || estorba || enemigo) h.RecibirDano(100000);
            }

            // Si algo lo detiene (un escalón, un enemigo que no muere) se adelanta un poco
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

        Assert.IsTrue(pasoEmboscada, "Pasó por la emboscada de la bocamina");
        Assert.IsTrue(peleoJinete, "Llegó al patio de la capilla y peleó con el Jinete");
        Assert.AreEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual, "El Nivel 2 se completa");
        Assert.AreEqual(3, GestorGuardado.Datos.nivelDesbloqueado, "y queda guardado");
    }
}
