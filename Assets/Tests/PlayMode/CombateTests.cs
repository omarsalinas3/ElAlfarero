using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #62: con un enemigo pegado el clic golpea en vez de lanzar, combo de 3 golpes con clics
/// seguidos (el tercero empuja), el combo se reinicia si se deja de presionar, y Mateo no se mueve mientras golpea.
/// El regreso a la Talavera al acabarse la munición ya lo prueba ArmasTests.
/// </summary>
public class CombateTests
{
    Keyboard teclado;
    PlayerController jugador;
    CombateCuerpo combate;
    Health enemigo;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);
        jugador = Object.FindFirstObjectByType<PlayerController>();
        combate = jugador.GetComponent<CombateCuerpo>();

        // El minero de prueba no se mueve ni tiene cuerpo físico: se aleja y se usa un enemigo que sí se puede empujar
        foreach (var h in Object.FindObjectsByType<Health>().Where(h => h.GetComponent<PlayerController>() == null))
            h.transform.position += Vector3.right * 40f;
        Physics2D.SyncTransforms();
    }

    [TearDown]
    public void Limpiar() => InputSystem.RemoveDevice(teclado);

    void Enemigo(float distancia)
    {
        var go = new GameObject("Enemigo_Prueba") { layer = LayerMask.NameToLayer("Enemy") };
        go.transform.position = jugador.transform.position + new Vector3(distancia, 0.05f, 0f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 2.2f);
        col.offset = new Vector2(0f, 1.1f);
        enemigo = go.AddComponent<Health>();
        Physics2D.SyncTransforms();
    }

    IEnumerator Clic()
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Space));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    static int Lanzadas() => Object.FindObjectsByType<Projectile>().Count(p => p.gameObject.activeInHierarchy);

    [UnityTest]
    public IEnumerator EnemigoCerca_ElClicGolpeaEnVezDeLanzar()
    {
        Enemigo(1.3f);
        yield return null;
        yield return Clic();
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(0, Lanzadas(), "No lanza cerámica");
        Assert.AreEqual(1, combate.Paso, "Primer golpe del combo");
        Assert.AreEqual(90, enemigo.VidaActual, "El puñetazo hace daño");
    }

    [UnityTest]
    public IEnumerator EnemigoLejos_ElClicLanzaCeramica()
    {
        Enemigo(6f);
        yield return null;
        yield return Clic();
        Assert.Greater(Lanzadas(), 0, "Lejos se lanza la Talavera");
        Assert.AreEqual(0, combate.Paso);
    }

    [UnityTest]
    public IEnumerator Combo_TresClicsSeguidosYElTerceroEmpuja()
    {
        Enemigo(1.3f);
        yield return null;
        for (int i = 0; i < 3; i++)
        {
            yield return Clic();
            yield return new WaitForSeconds(i < 2 ? 0.2f : 0.05f);   // El 2.º clic cae durante el 1.er golpe: se encadena
        }
        float x0 = enemigo.transform.position.x;
        float limite = Time.time + 1.5f;
        while (combate.Paso < 3 && Time.time < limite) yield return null;
        yield return new WaitForSeconds(0.45f);
        Assert.AreEqual(3, combate.Paso, "Llega al tercer golpe");
        Assert.AreEqual(100 - 10 - 10 - 25, enemigo.VidaActual, "Dos puñetazos y el golpe fuerte");
        Assert.Greater(enemigo.transform.position.x - x0, 0.8f, "El golpe fuerte lo empuja");
    }

    [UnityTest]
    public IEnumerator Combo_SeReiniciaSiSeDejaDePresionar()
    {
        Enemigo(1.3f);
        yield return null;
        yield return Clic();
        yield return new WaitForSeconds(0.25f + 0.6f);          // Más que la ventana de 0.4 s
        yield return Clic();
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(1, combate.Paso, "Vuelve a empezar con el primer golpe");
        Assert.AreEqual(80, enemigo.VidaActual, "Fueron dos puñetazos ligeros, no el golpe fuerte");
    }

    [UnityTest]
    public IEnumerator Golpeando_MateoNoSeMueve()
    {
        Enemigo(1.3f);
        yield return null;
        float x0 = jugador.transform.position.x;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.D, Key.Space));
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.D));
        yield return new WaitForSeconds(0.15f);
        Assert.IsTrue(combate.Golpeando);
        Assert.Less(Mathf.Abs(jugador.transform.position.x - x0), 0.1f, "Aunque se mantenga D, no avanza mientras golpea");
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    [Test]
    public void Sonidos_GolpeLigeroYFuerte()
    {
        var biblioteca = Resources.Load<BibliotecaAudio>("BibliotecaAudio");
        Assert.IsTrue(biblioteca.efectos.Any(e => e.id == Sfx.PunoLigero && e.clips.Length > 0));
        Assert.IsTrue(biblioteca.efectos.Any(e => e.id == Sfx.PunoFuerte && e.clips.Length > 0));
    }
}
