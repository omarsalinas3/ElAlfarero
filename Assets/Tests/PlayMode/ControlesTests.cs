using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.OnScreen;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de la issue #36: jugar el Nivel 1 con gamepad (mover, saltar, evadir, lanzar, apuntar con el stick
/// derecho, caguama), textos según el dispositivo activo y controles táctiles que solo aparecen con el dedo.
/// </summary>
public class ControlesTests
{
    Gamepad gamepad;
    Keyboard teclado;
    PlayerController mateo;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        gamepad = InputSystem.AddDevice<Gamepad>();
        teclado = InputSystem.AddDevice<Keyboard>();
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        mateo = Object.FindAnyObjectByType<PlayerController>();
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(gamepad);
        InputSystem.RemoveDevice(teclado);
        DispositivoActivo.Usar(TipoDispositivo.Teclado);
        GestorGuardado.Borrar();
    }

    IEnumerator Boton(GamepadButton b, int cuadros = 2)
    {
        InputSystem.QueueStateEvent(gamepad, new GamepadState { buttons = 1u << (int)b });
        for (int i = 0; i < cuadros; i++) yield return null;
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        yield return null;
    }

    [UnityTest]
    public IEnumerator Gamepad_StickMueveYAesSaltar()
    {
        float x0 = mateo.transform.position.x;
        InputSystem.QueueStateEvent(gamepad, new GamepadState { leftStick = new Vector2(1f, 0f) });
        yield return new WaitForSeconds(0.5f);
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        Assert.Greater(mateo.transform.position.x, x0 + 1f, "El stick izquierdo mueve a Mateo");

        yield return new WaitForSeconds(0.3f);
        float y0 = mateo.transform.position.y;
        yield return Boton(GamepadButton.South);
        yield return new WaitForSeconds(0.15f);
        Assert.Greater(mateo.transform.position.y, y0 + 0.5f, "A salta");
    }

    [UnityTest]
    public IEnumerator Gamepad_BEvadeYEsInvulnerable()
    {
        yield return Boton(GamepadButton.East);
        Assert.IsTrue(mateo.EstaEvadiendo, "B evade");
    }

    [UnityTest]
    public IEnumerator Gamepad_XLanzaYElStickDerechoApunta()
    {
        var tirador = mateo.GetComponent<PlayerShooter>();
        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0f, 1f) });
        yield return null;
        yield return null;
        Assert.AreEqual(Vector2.up, tirador.DireccionApuntado, "El stick derecho apunta hacia arriba");

        int antes = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Count(p => p.gameObject.activeInHierarchy);
        InputSystem.QueueStateEvent(gamepad, new GamepadState { rightStick = new Vector2(0f, 1f), buttons = 1u << (int)GamepadButton.West });
        yield return new WaitForSeconds(0.3f);
        InputSystem.QueueStateEvent(gamepad, new GamepadState());
        int despues = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).Count(p => p.gameObject.activeInHierarchy);
        Assert.Greater(despues, antes, "X lanza cerámica");
    }

    [UnityTest]
    public IEnumerator Gamepad_YBebeCaguama()
    {
        var inv = GestorNivel.Actual.Inventario;
        inv.AgregarCaguamas(1);
        int antes = inv.Caguamas;
        yield return Boton(GamepadButton.North);
        Assert.AreEqual(antes - 1, inv.Caguamas, "Y bebe una caguama");
        Assert.IsTrue(inv.CaguamaActiva);
    }

    [UnityTest]
    public IEnumerator Dispositivo_LosTextosCambianConElGamepad()
    {
        var presiona = Object.FindObjectsByType<TextoSegunDispositivo>(FindObjectsSortMode.None).First().GetComponent<Text>();
        string conTeclado = presiona.text;
        yield return Boton(GamepadButton.South);
        Assert.AreEqual(TipoDispositivo.Gamepad, DispositivoActivo.Actual);
        Assert.AreNotEqual(conTeclado, presiona.text, "La ayuda del HUD muestra los botones del gamepad");

        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.D));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
        Assert.AreEqual(TipoDispositivo.Teclado, DispositivoActivo.Actual);
        Assert.AreEqual(conTeclado, presiona.text);
    }

    [UnityTest]
    public IEnumerator Tactiles_OcultosEnPCYFuncionanConElDedo()
    {
        var tactiles = Object.FindObjectsByType<ControlesTactiles>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single();
        if (!DispositivoActivo.HayTactil)
            Assert.IsFalse(tactiles.gameObject.activeInHierarchy, "Sin pantalla táctil no se muestran");

        tactiles.Forzar();
        yield return null;
        Assert.IsTrue(tactiles.Visibles, "Con el dedo aparecen el joystick y los botones");
        var botones = tactiles.GetComponentsInChildren<OnScreenButton>().Select(b => b.controlPath).ToArray();
        CollectionAssert.IsSubsetOf(new[] { "<Gamepad>/buttonSouth", "<Gamepad>/buttonWest", "<Gamepad>/buttonEast", "<Gamepad>/buttonNorth", "<Gamepad>/start" }, botones);
        Assert.IsNotNull(tactiles.GetComponentInChildren<OnScreenStick>(), "Hay joystick virtual");

        // Tocar SALTO hace saltar a Mateo (el botón alimenta un gamepad virtual)
        yield return new WaitForSeconds(0.2f);
        var salto = tactiles.GetComponentsInChildren<OnScreenButton>().First(b => b.name == "Salto");
        float y0 = mateo.transform.position.y;
        var evento = new PointerEventData(EventSystem.current);
        salto.OnPointerDown(evento);
        yield return null;
        yield return null;
        salto.OnPointerUp(evento);
        yield return new WaitForSeconds(0.15f);
        Assert.Greater(mateo.transform.position.y, y0 + 0.5f, "El botón SALTO salta");
        Assert.AreEqual(TipoDispositivo.Tactil, DispositivoActivo.Actual, "El gamepad virtual no cuenta como gamepad de verdad");

        // Con un gamepad de verdad se esconden
        yield return Boton(GamepadButton.South);
        Assert.IsFalse(tactiles.Visibles, "Al usar un gamepad de verdad se esconden");
    }
}
