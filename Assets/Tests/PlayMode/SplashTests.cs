using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #25: la escena Splash es la primera del build, muestra el logo del equipo y luego el título
/// con fundidos, termina en el menú y se puede saltar con cualquier tecla.
/// </summary>
public class SplashTests
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
    public void Limpiar() { InputSystem.RemoveDevice(teclado); }

    static CanvasGroup Grupo(string nombre) => Object.FindObjectsByType<CanvasGroup>().First(g => g.name == nombre);

    [Test]
    public void Splash_EsLaPrimeraEscenaDelJuego()
    {
        StringAssert.EndsWith("/Splash.unity", SceneUtility.GetScenePathByBuildIndex(0));
    }

    [UnityTest]
    public IEnumerator Splash_LogoLuegoTituloYDespuesElMenu()
    {
        yield return SceneManager.LoadSceneAsync("Splash", LoadSceneMode.Single);
        yield return null;                             // Que corra Start (deja los dos grupos invisibles)
        var logo = Grupo("Logo");
        var titulo = Grupo("Titulo");

        // El orden importa: el título solo cuenta si aparece después de que ya se vio el logo
        bool logoVisto = false, tituloDespues = false;
        for (float t = 0f; t < 10f && SceneManager.GetActiveScene().name == "Splash"; t += Time.unscaledDeltaTime)
        {
            if (logo != null && titulo != null)
            {
                if (logo.alpha > 0.9f && titulo.alpha < 0.1f) logoVisto = true;
                if (logoVisto && titulo.alpha > 0.9f && logo.alpha < 0.1f) tituloDespues = true;
            }
            yield return null;
        }

        Assert.IsTrue(logoVisto, "Primero aparece el logo del equipo");
        Assert.IsTrue(tituloDespues, "Después aparece el título del juego");
        yield return new WaitForSeconds(1f);
        Assert.AreEqual("MenuPrincipal", SceneManager.GetActiveScene().name, "Al terminar pasa al menú");
    }

    [UnityTest]
    public IEnumerator Splash_SeSaltaConCualquierTecla()
    {
        yield return SceneManager.LoadSceneAsync("Splash", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Space));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());

        yield return new WaitForSeconds(1.5f);   // Mucho antes de que termine sola (~5 s)
        Assert.AreEqual("MenuPrincipal", SceneManager.GetActiveScene().name);
    }
}
