using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de la issue #57: tras ganar el Nivel 1 se puede seguir a Pozos con el viaje montado en el alebrije, el
/// viaje avanza solo y termina en el Nivel 2 (o se salta con una tecla). Que la victoria desbloquee el Nivel 2 y que
/// Continuar lleve ahí ya lo prueban GuardadoTests y MenuPrincipalTests.
/// </summary>
public class ViajeTests
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
        InputSystem.RemoveDevice(teclado);
        GestorGuardado.Borrar();
    }

    static IEnumerator EsperarEscena(string nombre, float segundos)
    {
        float limite = Time.realtimeSinceStartup + segundos;
        while (SceneManager.GetActiveScene().name != nombre && Time.realtimeSinceStartup < limite) yield return null;
    }

    [UnityTest]
    public IEnumerator Nivel1_LaVictoriaLlevaAlViajeAPozos()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Nivel1, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        Object.FindFirstObjectByType<PlayerController>().GetComponent<Health>().HacerInvulnerable(999f);
        GestorNivel.Actual.Completar();
        yield return new WaitForSeconds(0.6f);

        var siguiente = Object.FindObjectsByType<Button>().FirstOrDefault(b => b.name == "Siguiente");
        Assert.IsNotNull(siguiente, "La victoria del Nivel 1 ofrece seguir a Pozos");
        Assert.AreEqual(siguiente.gameObject, EventSystem.current.currentSelectedGameObject, "Es la opción seleccionada");
        Assert.AreEqual(2, GestorGuardado.Datos.nivelDesbloqueado, "Ya quedó desbloqueado el Nivel 2");

        siguiente.onClick.Invoke();
        yield return EsperarEscena(Escenas.Viaje, 6f);
        Assert.AreEqual(Escenas.Viaje, SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Viaje_ElAlebrijeGalopaYTerminaEnElNivel2()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Viaje, LoadSceneMode.Single);
        var viaje = Object.FindFirstObjectByType<ViajeMontado>();
        float xInicial = viaje.Montado.position.x;
        float camaraInicial = Camera.main.transform.position.x;

        yield return new WaitForSeconds(2f);
        Assert.Greater(viaje.Montado.position.x, xInicial + 10f, "Avanza galopando sin que nadie lo controle");
        Assert.Greater(Camera.main.transform.position.x, camaraInicial, "La cámara lo sigue");
        Assert.Greater(Object.FindObjectsByType<CanvasGroup>().First(g => g.name == "Letrero").alpha, 0.9f, "Se lee RUMBO A MINERAL DE POZOS");
        Assert.GreaterOrEqual(Object.FindObjectsByType<SpriteRenderer>().Count(s => s.name == "Huella"), 20, "Hay huellas de fuego que seguir");

        yield return EsperarEscena(Escenas.Nivel2, 12f);
        Assert.AreEqual(Escenas.Nivel2, SceneManager.GetActiveScene().name, "Al salir de cuadro llega a Mineral de Pozos");
        Assert.IsNotNull(Object.FindFirstObjectByType<LlegadaMontado>(), "El Nivel 2 sigue con la llegada montado");
    }

    [UnityTest]
    public IEnumerator Viaje_SeSaltaConUnaTecla()
    {
        yield return SceneManager.LoadSceneAsync(Escenas.Viaje, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);
        float inicio = Time.realtimeSinceStartup;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Escape));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());

        yield return EsperarEscena(Escenas.Nivel2, 5f);
        Assert.AreEqual(Escenas.Nivel2, SceneManager.GetActiveScene().name);
        Assert.Less(Time.realtimeSinceStartup - inicio, 4f, "No espera a que termine el galope");
    }
}
