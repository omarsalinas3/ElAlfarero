using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #52 (Sandbox): un roce del mouse ya no le quita el control al teclado,
/// y mantener flecha arriba mientras se lanza con Espacio apunta hacia arriba sin saltar.
/// </summary>
public class ApuntadoTecladoTests
{
    Keyboard teclado;
    Mouse mouse;
    PlayerController jugador;
    PlayerShooter tirador;
    Rigidbody2D rb;
    Vector2 posMouse;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);

        // En batchmode no hay foco de ventana: sin esto el Input System descarta los dispositivos virtuales
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;

        teclado = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();

        jugador = Object.FindAnyObjectByType<PlayerController>();
        tirador = jugador.GetComponent<PlayerShooter>();
        rb = jugador.GetComponent<Rigidbody2D>();
        yield return new WaitForSeconds(0.6f);   // Que caiga y se asiente

        // Mouse a la izquierda de Mateo: si el mouse mandara, Mateo miraría a la izquierda
        Vector3 izquierda = Camera.main.WorldToScreenPoint(jugador.transform.position + new Vector3(-5f, 1.4f, 0f));
        posMouse = new Vector2(izquierda.x, izquierda.y);
        MoverMouse(Vector2.zero);
        yield return null;

        // Caminar un poco a la derecha: desde aquí manda el teclado
        Teclas(Key.RightArrow);
        yield return new WaitForSeconds(0.15f);
        Teclas();
        yield return new WaitForSeconds(0.2f);
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(teclado);
        InputSystem.RemoveDevice(mouse);
    }

    void Teclas(params Key[] teclas) => InputSystem.QueueStateEvent(teclado, new KeyboardState(teclas));

    void MoverMouse(Vector2 delta)
    {
        posMouse += delta;
        InputSystem.QueueStateEvent(mouse, new MouseState { position = posMouse });
    }

    [UnityTest]
    public IEnumerator MouseApenasMovido_NoLeQuitaElControlAlTeclado()
    {
        Assert.IsFalse(tirador.UsandoMouse, "Después de caminar manda el teclado");

        // Roces de pocos píxeles, como al tocar el escritorio
        for (int i = 0; i < 6; i++)
        {
            MoverMouse(new Vector2(i % 2 == 0 ? 4f : -4f, 2f));
            yield return new WaitForSeconds(0.05f);
        }
        Assert.IsFalse(tirador.UsandoMouse, "Mover el mouse unos pocos píxeles no le quita el control al teclado");
        Assert.AreEqual(1, jugador.DireccionMirada);
        Assert.Greater(tirador.DireccionApuntado.x, 0.9f, "Sigue lanzando al frente");

        // Moverlo de verdad sí le devuelve el control al mouse
        MoverMouse(new Vector2(-150f, 0f));
        yield return new WaitForSeconds(0.1f);
        Assert.IsTrue(tirador.UsandoMouse, "Un movimiento real del mouse vuelve a mandar");
        Assert.AreEqual(-1, jugador.DireccionMirada, "Y Mateo voltea hacia el mouse");
    }

    [UnityTest]
    public IEnumerator FlechaArribaAlLanzar_ApuntaArribaSinSaltar()
    {
        float sueloY = jugador.transform.position.y;

        Teclas(Key.Space);
        yield return new WaitForSeconds(0.1f);
        Teclas(Key.Space, Key.UpArrow);
        yield return new WaitForSeconds(0.3f);

        Assert.Greater(tirador.DireccionApuntado.y, 0.99f, "Flecha arriba mientras lanza apunta hacia arriba");
        Assert.IsTrue(jugador.EstaEnSuelo, "Y no salta");
        Assert.AreEqual(sueloY, jugador.transform.position.y, 0.05f);

        // Al soltar la flecha vuelve a lanzar al frente
        Teclas(Key.Space);
        yield return new WaitForSeconds(0.1f);
        Assert.Greater(tirador.DireccionApuntado.x, 0.9f);
        Teclas();
    }

    [UnityTest]
    public IEnumerator LanzandoConEspacio_WSigueSaltando()
    {
        Teclas(Key.Space);
        yield return new WaitForSeconds(0.1f);
        Teclas(Key.Space, Key.W);
        yield return new WaitForSeconds(0.15f);

        Assert.Greater(rb.linearVelocity.y, 1f, "W salta aunque se esté lanzando");
        Teclas();
    }
}
