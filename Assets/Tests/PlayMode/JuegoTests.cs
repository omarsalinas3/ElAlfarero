using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas del juego completo: Nivel 1 (pausa, game over, caguama, guacamaya, huacales, meta) y menú principal.
/// </summary>
public class JuegoTests
{
    Keyboard teclado;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        InputSystem.RemoveDevice(teclado);
    }

    IEnumerator CargarNivel1()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        Assert.IsNotNull(GestorNivel.Actual, "El Nivel 1 debe tener GestorNivel");
    }

    IEnumerator Tecla(Key k)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(k));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    [UnityTest]
    public IEnumerator Pausa_ConEscDetieneYReanudaElTiempo()
    {
        yield return CargarNivel1();
        yield return Tecla(Key.Escape);
        Assert.AreEqual(GestorNivel.Estado.Pausa, GestorNivel.Actual.EstadoActual);
        Assert.AreEqual(0f, Time.timeScale);
        Assert.IsTrue(Object.FindAnyObjectByType<MenuPausa>(FindObjectsInactive.Include).gameObject.activeInHierarchy, "Debe verse el menú de pausa");

        yield return Tecla(Key.Escape);
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator GameOver_AlQuedarseSinVida()
    {
        yield return CargarNivel1();
        GestorNivel.Actual.SaludJugador.RecibirDano(1000);
        Assert.AreEqual(GestorNivel.Estado.GameOver, GestorNivel.Actual.EstadoActual);
        Assert.IsFalse(Object.FindAnyObjectByType<PlayerController>().ControlActivo);

        yield return new WaitForSeconds(2.2f);
        var pantallas = Object.FindObjectsByType<PantallaFinal>(FindObjectsInactive.Include);
        Assert.IsTrue(pantallas.Any(p => p.gameObject.activeInHierarchy && p.name == "Raiz" && p.transform.parent.name == "GameOver"),
                      "Debe aparecer la pantalla de GAME OVER");
    }

    [UnityTest]
    public IEnumerator Caguama_SeBebeConEYDaInvencibilidad()
    {
        yield return CargarNivel1();
        var inv = GestorNivel.Actual.Inventario;
        inv.AgregarCaguamas(1);
        int antes = inv.Caguamas;

        yield return Tecla(Key.E);
        Assert.AreEqual(antes - 1, inv.Caguamas);
        Assert.IsTrue(inv.CaguamaActiva);

        var salud = GestorNivel.Actual.SaludJugador;
        salud.RecibirDano(50);
        Assert.AreEqual(salud.VidaMaxima, salud.VidaActual, "Con la caguama activa no recibe daño");
    }

    [UnityTest]
    public IEnumerator Guacamaya_CuraAlRecogerla()
    {
        yield return CargarNivel1();
        var salud = GestorNivel.Actual.SaludJugador;
        salud.RecibirDano(50);
        int vidaHerido = salud.VidaActual;

        var guacamaya = Object.FindObjectsByType<Recogible>().First(r => r.Tipo == TipoRecogible.Guacamaya);
        guacamaya.transform.position = salud.transform.position + Vector3.up * 1f;
        yield return new WaitForSeconds(0.2f);

        Assert.Greater(salud.VidaActual, vidaHerido, "La Guacamaya debe curar");
        Assert.IsTrue(guacamaya == null, "Se consume al recogerla");
    }

    [UnityTest]
    public IEnumerator Huacal_SeRompeYSueltaBotin()
    {
        yield return CargarNivel1();
        int recogiblesAntes = Object.FindObjectsByType<Recogible>().Length;
        var huacal = Object.FindObjectsByType<Destructible>().OrderBy(d => d.transform.position.x).First();
        huacal.GetComponent<Health>().RecibirDano(1000);
        yield return null;

        Assert.IsTrue(huacal == null, "El huacal se destruye");
        Assert.Greater(Object.FindObjectsByType<Recogible>().Length, recogiblesAntes, "Suelta monedas u otro botín");
    }

    [UnityTest]
    public IEnumerator Meta_CompletaElNivel()
    {
        yield return CargarNivel1();
        var meta = Object.FindAnyObjectByType<MetaNivel>();
        var jugador = Object.FindAnyObjectByType<PlayerController>();
        jugador.GetComponent<Rigidbody2D>().position = new Vector2(meta.transform.position.x, -4.4f);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(GestorNivel.Estado.Victoria, GestorNivel.Actual.EstadoActual);
    }

    [UnityTest]
    public IEnumerator Menu_AlPresionarUnaTeclaApareceElMenu()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        var menu = Object.FindAnyObjectByType<MenuPrincipal>();
        Assert.IsTrue(menu.EnTitulo);

        yield return Tecla(Key.Enter);
        Assert.IsFalse(menu.EnTitulo);
        Assert.IsTrue(Object.FindObjectsByType<Panel>().Any(p => p.name == "PanelMenu"), "El panel del menú debe estar visible");
    }
}
