using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #28: el Nivel 1 empieza con una zona segura con carteles que explican el objetivo y los
/// controles, y los carteles cambian de teclas a botones según el dispositivo que se usó al último.
/// </summary>
public class TutorialTests
{
    CartelTutorial[] carteles;
    PlayerController jugador;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        jugador = Object.FindFirstObjectByType<PlayerController>();
        carteles = Object.FindObjectsByType<CartelTutorial>(FindObjectsSortMode.None).OrderBy(c => c.transform.position.x).ToArray();
    }

    [Test]
    public void Tutorial_CartelesExplicanObjetivoYControles()
    {
        Assert.GreaterOrEqual(carteles.Length, 4, "Hay carteles al inicio del Nivel 1");
        string todo = string.Join("\n", carteles.Select(c => c.TextoActual));
        foreach (string clave in new[] { "PARROQUIA", "MUEVETE", "SALTA", "EVADE", "LANZA", "APUNTA", "HUACALES", "CAGUAMA" })
            StringAssert.Contains(clave, todo, $"Algún cartel explica {clave}");
        StringAssert.Contains("PARROQUIA", carteles[0].TextoActual, "El primero explica el objetivo");
        Assert.AreEqual("LLEGA A LA PARROQUIA", GestorNivel.Actual.Objetivo);
    }

    [Test]
    public void Tutorial_ZonaSeguraSinEnemigos()
    {
        float fin = carteles.Last().transform.position.x + 2f;
        float inicio = jugador.transform.position.x;
        var enemigos = Object.FindObjectsByType<IAEsqueleto>(FindObjectsSortMode.None).Select(e => e.transform.position.x)
                             .Concat(Object.FindObjectsByType<IABandido>(FindObjectsSortMode.None).Select(b => b.transform.position.x));
        Assert.IsFalse(enemigos.Any(x => x > inicio && x < fin), "Ningún enemigo dentro de la zona de los carteles");
    }

    [UnityTest]
    public IEnumerator Tutorial_CambiaABotonesConElGamepad()
    {
        var gamepad = InputSystem.AddDevice<Gamepad>();
        var teclado = InputSystem.AddDevice<Keyboard>();
        try
        {
            string conTeclado = carteles[1].TextoActual;
            InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1 << (int)GamepadButton.South });
            yield return null;
            yield return null;
            Assert.IsTrue(CartelTutorial.UsandoGamepad, "Tocar el gamepad lo vuelve el dispositivo activo");
            Assert.AreNotEqual(conTeclado, carteles[1].TextoActual, "El cartel muestra los botones del gamepad");

            InputSystem.QueueStateEvent(gamepad, new GamepadState());
            InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.D));
            yield return null;
            yield return null;
            Assert.IsFalse(CartelTutorial.UsandoGamepad, "Usar el teclado regresa a las teclas");
            Assert.AreEqual(conTeclado, carteles[1].TextoActual);
        }
        finally
        {
            InputSystem.RemoveDevice(gamepad);
            InputSystem.RemoveDevice(teclado);
        }
    }
}
