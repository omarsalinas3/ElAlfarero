using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas del Sprint 1 sobre la escena Sandbox: el jugador cae al suelo, corre, salta, evade y dispara.
/// Simulan teclado y mouse con dispositivos virtuales del Input System.
/// </summary>
public class Sprint1Tests
{
    Keyboard teclado;
    Mouse mouse;
    PlayerController jugador;
    Rigidbody2D rb;

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
        rb = jugador.GetComponent<Rigidbody2D>();
        yield return new WaitForSeconds(0.6f);   // Que caiga y se asiente

        // El mouse apunta a la derecha del jugador (si no, el personaje mira hacia donde esté el cursor)
        Vector3 derecha = Camera.main.WorldToScreenPoint(jugador.transform.position + new Vector3(5f, 1.4f, 0f));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(derecha.x, derecha.y) });
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.RemoveDevice(teclado);
        InputSystem.RemoveDevice(mouse);
    }

    void Teclas(params Key[] teclas)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(teclas));
    }

    [UnityTest]
    public IEnumerator Jugador_AterrizaYDetectaSuelo()
    {
        Assert.IsTrue(jugador.EstaEnSuelo, "Debe estar en el suelo tras caer");
        Assert.Less(Mathf.Abs(rb.linearVelocity.y), 0.1f);
        Assert.AreEqual(-4.5f, jugador.transform.position.y, 0.1f, "Los pies deben quedar sobre el adoquin");
        yield return null;
    }

    [UnityTest]
    public IEnumerator Jugador_CorreHaciaLaDerecha()
    {
        float x0 = jugador.transform.position.x;
        Teclas(Key.D);
        yield return new WaitForSeconds(0.5f);
        Teclas();
        Assert.Greater(jugador.transform.position.x - x0, 2.5f, "Debe avanzar al mantener D");
        Assert.AreEqual(1, jugador.DireccionMirada);
    }

    [UnityTest]
    public IEnumerator Jugador_SaltaYVuelveAlSuelo()
    {
        float y0 = jugador.transform.position.y;
        float yMax = y0;

        Teclas(Key.UpArrow);   // Espacio ahora es para lanzar
        yield return null;
        float fin = Time.time + 0.7f;   // Por tiempo de juego, no por frames (en batchmode los frames son muy cortos)
        while (Time.time < fin)
        {
            yMax = Mathf.Max(yMax, jugador.transform.position.y);
            yield return null;
        }
        Teclas();
        Assert.Greater(yMax - y0, 2f, "El salto completo debe superar 2 unidades");
        yield return new WaitForSeconds(1f);
        Assert.IsTrue(jugador.EstaEnSuelo, "Debe volver a caer al suelo");
    }

    [UnityTest]
    public IEnumerator Jugador_EvadeEsInvulnerable()
    {
        var salud = jugador.GetComponent<Health>();
        Teclas(Key.LeftShift);
        yield return new WaitForSeconds(0.1f);
        Assert.IsTrue(jugador.EstaEvadiendo);
        salud.RecibirDano(10);
        Assert.AreEqual(salud.VidaMaxima, salud.VidaActual, "Sin dano durante la evasion");
        Teclas();
        yield return new WaitForSeconds(0.4f);
        Assert.IsFalse(jugador.EstaEvadiendo);
        salud.RecibirDano(10);
        Assert.AreEqual(salud.VidaMaxima - 10, salud.VidaActual, "Con dano al terminar la evasion");
    }

    [UnityTest]
    public IEnumerator Disparo_LastimaAlDummy()
    {
        // El dummy de x=4 queda en pantalla; apuntamos a su torso
        Health dummy = null;
        foreach (var h in Object.FindObjectsByType<Health>())
            if (h.gameObject.layer == 8 && Mathf.Abs(h.transform.position.x - 4f) < 0.1f) dummy = h;
        Assert.IsNotNull(dummy, "Falta el dummy de pruebas en x=4");

        Vector3 pantalla = Camera.main.WorldToScreenPoint(dummy.transform.position + Vector3.up * 1.3f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(pantalla.x, pantalla.y) }.WithButton(MouseButton.Left));

        yield return new WaitForSeconds(0.6f);
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(pantalla.x, pantalla.y) });

        Assert.Less(dummy.VidaActual, dummy.VidaMaxima, "El dummy debe recibir dano de la Talavera");
    }

    Health DummyDeEnfrente()
    {
        foreach (var h in Object.FindObjectsByType<Health>())
            if (h.gameObject.layer == 8 && Mathf.Abs(h.transform.position.x - 4f) < 0.1f) return h;
        return null;
    }

    [UnityTest]
    public IEnumerator Disparo_ConEspacioLanzaAlFrente()
    {
        Health dummy = DummyDeEnfrente();
        Assert.IsNotNull(dummy, "Falta el dummy de pruebas en x=4");

        // Primero camina un poco a la derecha con la flecha: desde ahí se apunta al frente, sin mouse
        Teclas(Key.RightArrow);
        yield return new WaitForSeconds(0.1f);
        Teclas(Key.Space);
        yield return new WaitForSeconds(0.6f);
        Teclas();

        Assert.AreEqual(1, jugador.DireccionMirada);
        Assert.Less(dummy.VidaActual, dummy.VidaMaxima, "Espacio debe lanzar la Talavera hacia el frente");
    }

    [UnityTest]
    public IEnumerator Orientacion_GanaElUltimoControlUsado()
    {
        // Mover el mouse a la izquierda lo voltea hacia el mouse...
        Vector3 izquierda = Camera.main.WorldToScreenPoint(jugador.transform.position + new Vector3(-5f, 1.4f, 0f));
        InputSystem.QueueStateEvent(mouse, new MouseState { position = new Vector2(izquierda.x, izquierda.y) });
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(-1, jugador.DireccionMirada, "Debe mirar hacia el mouse");

        // ...pero caminar con la flecha derecha lo voltea a la derecha aunque el mouse siga a la izquierda
        Teclas(Key.RightArrow);
        yield return new WaitForSeconds(0.15f);
        Teclas();
        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(1, jugador.DireccionMirada, "Las flechas también lo voltean");
        Assert.Greater(jugador.GetComponent<PlayerShooter>().DireccionApuntado.x, 0.9f, "Y el disparo va al frente");
    }
}
