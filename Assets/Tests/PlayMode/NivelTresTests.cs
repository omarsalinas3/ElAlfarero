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
/// Pruebas del Nivel 3 (Panteón junto al río, #58): se llega desde la victoria del Nivel 2, es más difícil que Pozos,
/// tiene sus 3 exvotos y se completa de principio a fin (emboscada incluida).
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

        yield return SceneManager.LoadSceneAsync(Escenas.Nivel3, LoadSceneMode.Single);
        yield return null;
        Assert.Greater(Object.FindObjectsByType<IABruja>().Length, brujasPozos, "Más ataques desde el aire");
        Assert.IsNotNull(Object.FindFirstObjectByType<Emboscada>(), "Tiene emboscada");
        Assert.GreaterOrEqual(Object.FindObjectsByType<IAEsqueleto>().Length, 4);
        Assert.AreEqual(3, Object.FindObjectsByType<Exvoto>().Select(e => e.Id).Distinct().Count(), "3 exvotos");
        Assert.IsNotNull(GameObject.Find("Arena_Llorona"), "El patio queda reservado para La Llorona");
    }

    [Test]
    public void Musica_DelNivelYDeLaPeleaConLaLlorona()
    {
        var biblioteca = Resources.Load<BibliotecaAudio>("BibliotecaAudio");
        Assert.IsTrue(biblioteca.musicas.Any(m => m.id == Musica.Nivel3 && m.clip != null));
        Assert.IsTrue(biblioteca.musicas.Any(m => m.id == Musica.Llorona && m.clip != null));
    }

    /// <summary>Mateo invulnerable corre a la derecha; lo que se le pone enfrente se vence para no depender de la puntería.</summary>
    [UnityTest]
    [Timeout(180000)]
    public IEnumerator Nivel3_SePuedeCompletarDeInicioAFin()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel3, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        var gestor = GestorNivel.Actual;
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        var rb = jugador.GetComponent<Rigidbody2D>();
        var emboscada = Object.FindFirstObjectByType<Emboscada>();
        Time.timeScale = 2f;

        float limite = Time.realtimeSinceStartup + 120f, siguienteRevision = 0f, xRevision = rb.position.x;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.RightArrow, Key.Space));
        while (gestor.EstadoActual != GestorNivel.Estado.Victoria && Time.realtimeSinceStartup < limite)
        {
            salud.HacerInvulnerable(5f);
            Assert.AreNotEqual(GestorNivel.Estado.GameOver, gestor.EstadoActual);
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

        Assert.IsTrue(emboscada.Terminada, "Pasó por la emboscada de las criptas");
        Assert.AreEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual, "El Nivel 3 se completa");
        Assert.AreEqual(4, GestorGuardado.Datos.nivelDesbloqueado, "y queda guardado");
    }
}
