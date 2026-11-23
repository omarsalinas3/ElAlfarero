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
/// El final del juego (#91): vencer al Charro Negro lleva a las viñetas del rescate de Carmen al amanecer, la partida
/// queda marcada como terminada y al terminar se pasa a los créditos y al menú.
/// </summary>
public class FinalTests
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
        MenuPrincipal.OlvidarCreditos();                  // Que el menú de otras pruebas empiece en la portada
        GestorGuardado.Borrar();
    }

    IEnumerator EsperarEscena(string escena, float segundos = 10f)
    {
        float limite = Time.realtimeSinceStartup + segundos;
        while (SceneManager.GetActiveScene().name != escena && Time.realtimeSinceStartup < limite) yield return null;
        Assert.AreEqual(escena, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Nivel4_LaVictoriaLlevaAlFinal()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel4, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);
        GestorNivel.Actual.Completar();                   // Lo mismo que hace el Charro al caer
        yield return new WaitForSeconds(0.6f);

        var siguiente = Object.FindObjectsByType<Button>().FirstOrDefault(b => b.name == "Siguiente");
        Assert.IsNotNull(siguiente, "La victoria sobre el Charro ofrece seguir al final");
        siguiente.onClick.Invoke();
        yield return EsperarEscena(Escenas.Final);
    }

    [UnityTest]
    public IEnumerator Final_MarcaElJuegoTerminadoYTerminaEnLosCreditos()
    {
        Assert.IsFalse(GestorGuardado.Datos.juegoTerminado);
        yield return SceneManager.LoadSceneAsync(Escenas.Final, LoadSceneMode.Single);
        yield return null;

        var vinetas = Object.FindAnyObjectByType<SecuenciaIntro>();
        Assert.IsNotNull(vinetas, "El final son viñetas con SecuenciaIntro");
        Assert.AreEqual(4, vinetas.Vinetas, "Alebrije, Carmen con la Talavera, el Charro en humo y el amanecer");
        Assert.IsTrue(GestorGuardado.Datos.juegoTerminado, "La partida queda marcada como terminada");
        GestorGuardado.Cargar();
        Assert.IsTrue(GestorGuardado.Datos.juegoTerminado, "y se guarda en disco");

        // Se saltan las viñetas: el menú abre directo en los créditos
        yield return new WaitForSeconds(0.5f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Escape));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return EsperarEscena(Escenas.Menu);
        yield return new WaitForSeconds(0.3f);

        var menu = Object.FindAnyObjectByType<MenuPrincipal>();
        Assert.IsFalse(menu.EnTitulo, "Sin la portada");
        Assert.IsTrue(menu.EnCreditos, "Los créditos están abiertos");
    }

    [UnityTest]
    public IEnumerator Menu_SinVenirDelFinalEmpiezaEnLaPortada()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Menu, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        var menu = Object.FindAnyObjectByType<MenuPrincipal>();
        Assert.IsTrue(menu.EnTitulo, "Normalmente empieza en la portada");
        Assert.IsFalse(menu.EnCreditos);
    }
}
