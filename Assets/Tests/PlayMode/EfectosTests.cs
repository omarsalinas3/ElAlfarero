using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #30: partículas (polvo, aterrizaje, fragmentos, brillo), sacudida con hit stop que no pisa
/// la pausa, reutilización de partículas y botones de menú que entran escalonados.
/// </summary>
public class EfectosTests
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
        yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(teclado);
        Time.timeScale = 1f;
        GestorGuardado.Borrar();
    }

    [UnityTest]
    public IEnumerator Correr_LevantaPolvo()
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.D));
        float limite = Time.time + 1.5f;
        while (Efectos.Activas == 0 && Time.time < limite) yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        Assert.Greater(Efectos.Activas, 0, "Al correr salen motas de polvo de los pies");
    }

    [UnityTest]
    public IEnumerator Aterrizar_LevantaUnaNube()
    {
        var mateo = Object.FindAnyObjectByType<PlayerController>();
        mateo.transform.position += Vector3.up * 3f;           // Cae desde lo alto
        yield return new WaitForSeconds(0.1f);
        int antes = Efectos.Activas;
        float limite = Time.time + 2f;
        while (!mateo.EstaEnSuelo && Time.time < limite) yield return null;
        yield return null;
        Assert.GreaterOrEqual(Efectos.Activas - antes, 6, "Al caer de alto sale una nube a los lados");
    }

    [UnityTest]
    public IEnumerator Particulas_SeReutilizanSinPasarDelMaximo()
    {
        for (int i = 0; i < 60; i++) Efectos.Particulas(TipoParticula.Astillas, Vector2.zero);
        yield return null;
        int creadas = Efectos.Creadas;
        Assert.LessOrEqual(creadas, 240, "Hay un máximo fijo de partículas");
        for (int i = 0; i < 60; i++) Efectos.Particulas(TipoParticula.Brillo, Vector2.zero);
        yield return null;
        Assert.AreEqual(creadas, Efectos.Creadas, "Las nuevas reutilizan las viejas en lugar de crear objetos");

        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(0, Efectos.Activas, "Todas se apagan al terminar su vida");
    }

    [UnityTest]
    public IEnumerator GolpeFuerte_HitStopYVuelveALaVelocidadNormal()
    {
        Efectos.GolpeFuerte(0.08f, 0.2f);
        Assert.IsTrue(Efectos.EnHitStop);
        Assert.Less(Time.timeScale, 0.2f, "El juego se congela un instante");
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.IsFalse(Efectos.EnHitStop);
        Assert.AreEqual(1f, Time.timeScale);
    }

    [UnityTest]
    public IEnumerator GolpeFuerte_NoQuitaLaPausa()
    {
        Efectos.GolpeFuerte(0.08f, 0.2f);
        Time.timeScale = 0f;                                   // Se pausa durante el hit stop
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.AreEqual(0f, Time.timeScale, "Al terminar el hit stop la pausa se respeta");
    }

    [UnityTest]
    public IEnumerator Huacales_SueltanAstillasYRecogerBrilla()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        var mateo = Object.FindAnyObjectByType<PlayerController>();

        // Se rompen todos los huacales y hornos: cada uno suelta astillas y algunos, botín
        foreach (var d in Object.FindObjectsByType<Destructible>(FindObjectsSortMode.None))
        {
            int antes = Efectos.Activas;
            d.GetComponent<Health>().RecibirDano(10000);
            Assert.GreaterOrEqual(Efectos.Activas - antes, 10, d.name + " suelta astillas al romperse");
        }
        yield return new WaitForSeconds(1.2f);                 // Que se apaguen las astillas

        var botin = Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).FirstOrDefault(r => r.gameObject.activeInHierarchy);
        Assert.IsNotNull(botin, "Algún huacal soltó botín");
        int previas = Efectos.Activas;
        botin.transform.position = mateo.transform.position + Vector3.up;
        yield return new WaitForSeconds(0.15f);
        Assert.Greater(Efectos.Activas, previas, "Al recoger salen destellos");
    }

    [UnityTest]
    public IEnumerator Menu_BotonesEntranEscalonados()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Enter));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
        yield return null;

        var panel = Object.FindObjectsByType<Panel>(FindObjectsSortMode.None).First(p => p.name == "PanelMenu");
        var botones = panel.GetComponentsInChildren<UnityEngine.UI.Button>();
        float primero = ((RectTransform)botones.First(b => b.name == "Jugar").transform).anchoredPosition.x;
        float ultimo = ((RectTransform)botones.First(b => b.name == "Salir").transform).anchoredPosition.x;
        Assert.Less(ultimo, primero, "El último botón todavía va entrando cuando el primero ya avanzó");

        yield return new WaitForSecondsRealtime(0.6f);
        foreach (var b in botones) Assert.AreEqual(0f, ((RectTransform)b.transform).anchoredPosition.x, 0.01f, b.name + " termina en su lugar");
    }
}
