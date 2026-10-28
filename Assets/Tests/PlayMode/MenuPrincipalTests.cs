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
/// Pruebas de la issue #23: orden del menú, Continuar con y sin partida guardada, confirmación al jugar
/// con partida y navegación con teclado/gamepad que se salta los botones desactivados.
/// </summary>
public class MenuPrincipalTests
{
    Keyboard teclado;
    MenuPrincipal menu;

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

    /// <summary>Carga el menú y presiona una tecla para pasar la portada.</summary>
    IEnumerator AbrirMenu()
    {
        yield return SceneManager.LoadSceneAsync("MenuPrincipal", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        menu = Object.FindAnyObjectByType<MenuPrincipal>();
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Enter));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return new WaitForSeconds(0.3f);
    }

    static Button Boton(string panel, string nombre) =>
        Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
              .First(b => b.name == nombre && b.transform.parent.name == panel);

    static bool PanelVisible(string nombre) => Object.FindObjectsByType<Panel>().Any(p => p.name == nombre);

    [UnityTest]
    public IEnumerator Menu_OrdenDelCursoYContinuarDesactivadoSinPartida()
    {
        yield return AbrirMenu();

        var orden = Object.FindObjectsByType<Panel>().First(p => p.name == "PanelMenu").GetComponentsInChildren<Button>()
                          .OrderByDescending(b => ((RectTransform)b.transform).anchoredPosition.y).Select(b => b.name).ToArray();
        CollectionAssert.AreEqual(new[] { "Jugar", "Continuar", "Opciones", "Creditos", "Salir" }, orden);
        Assert.IsNotNull(Boton("PanelOpciones", "Controles"), "Controles pasa dentro de Opciones");

        Assert.IsFalse(Boton("PanelMenu", "Continuar").interactable, "Sin partida no se puede continuar");
        Assert.AreSame(Boton("PanelMenu", "Opciones"), Boton("PanelMenu", "Jugar").navigation.selectOnDown,
                       "Con teclado o gamepad, de Jugar se baja directo a Opciones (se salta Continuar)");
    }

    [UnityTest]
    public IEnumerator Continuar_ConPartidaCargaElUltimoNivelDesbloqueado()
    {
        GestorGuardado.RegistrarVictoria(1, 500, 60f, 5);
        yield return AbrirMenu();

        var continuar = Boton("PanelMenu", "Continuar");
        Assert.IsTrue(continuar.interactable);
        continuar.onClick.Invoke();
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual("Nivel2_Pozos", SceneManager.GetActiveScene().name, "Ganar Dolores desbloquea Mineral de Pozos");
    }

    [UnityTest]
    public IEnumerator Jugar_ConPartidaPideConfirmacion()
    {
        GestorGuardado.RegistrarVictoria(1, 500, 60f, 5);
        yield return AbrirMenu();

        Boton("PanelMenu", "Jugar").onClick.Invoke();
        yield return null;
        Assert.IsTrue(PanelVisible("PanelConfirmar"), "Jugar con partida pregunta antes de empezar de nuevo");

        Boton("PanelConfirmar", "No").onClick.Invoke();
        yield return null;
        Assert.IsTrue(PanelVisible("PanelMenu"));
        Assert.IsTrue(GestorGuardado.HayPartida, "Decir que no conserva la partida");

        Boton("PanelMenu", "Jugar").onClick.Invoke();
        yield return null;
        Boton("PanelConfirmar", "Si").onClick.Invoke();
        yield return new WaitForSeconds(1.5f);
        Assert.IsFalse(GestorGuardado.HayPartida, "Empezar de nuevo borra la partida");
        Assert.AreEqual("Intro", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Jugar_SinPartidaVaDirectoALaIntro()
    {
        yield return AbrirMenu();
        Boton("PanelMenu", "Jugar").onClick.Invoke();
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual("Intro", SceneManager.GetActiveScene().name);
    }

    [UnityTest]
    public IEnumerator Creditos_SeAbrenYSeRegresaAlMenu()
    {
        yield return AbrirMenu();
        Boton("PanelMenu", "Creditos").onClick.Invoke();
        yield return null;
        Assert.IsTrue(PanelVisible("PanelCreditos"));

        Boton("PanelCreditos", "Volver").onClick.Invoke();
        yield return null;
        Assert.IsTrue(PanelVisible("PanelMenu"));
    }
}
