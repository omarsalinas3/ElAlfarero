using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #50: el Alebrije liberado como montura. Se monta y desmonta con F; montado corre más rápido,
/// salta más alto y ataca con zarpazo; tiene su propia vida y, si se acaba, Mateo cae y sigue a pie.
/// </summary>
public class MonturaTests
{
    PlayerController jugador;
    Health salud;
    Rigidbody2D rb;
    MonturaAlebrije montura;
    Keyboard teclado;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);
        jugador = Object.FindFirstObjectByType<PlayerController>();
        salud = jugador.GetComponent<Health>();
        rb = jugador.GetComponent<Rigidbody2D>();
        montura = Object.FindFirstObjectByType<MonturaAlebrije>(FindObjectsInactive.Include);
        Assert.IsNotNull(montura, "El Nivel 1 trae la montura lista para cuando se rompa el hechizo");
        Assert.IsFalse(montura.gameObject.activeSelf, "No se ve hasta vencer al alebrije");

        // Como si ya lo hubiera vencido: aparece junto a Mateo, en la zona del tutorial (sin enemigos)
        montura.Liberar(new Vector3(jugador.transform.position.x + 1f, jugador.transform.position.y, 0f), -1);
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        InputSystem.RemoveDevice(teclado);
    }

    IEnumerator Pulsar(Key tecla)
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState(tecla));
        yield return null;
        yield return null;
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;
    }

    IEnumerator VelocidadCorriendo(System.Action<float> resultado)
    {
        rb.linearVelocity = Vector2.zero;
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.D));
        yield return new WaitForSeconds(0.4f);
        resultado(rb.linearVelocity.x);
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return new WaitForSeconds(0.3f);
    }

    [UnityTest]
    public IEnumerator Montura_FMontaYDesmonta()
    {
        yield return Pulsar(Key.F);
        Assert.IsTrue(montura.Montado, "F cerca del alebrije lo monta");
        Assert.IsFalse(jugador.GetComponent<PlayerShooter>().enabled, "Montado no lanza cerámica: ataca con zarpazo");

        yield return Pulsar(Key.F);
        Assert.IsFalse(montura.Montado, "F otra vez lo desmonta");
        Assert.AreEqual(MonturaAlebrije.Estado.Libre, montura.EstadoActual);
        Assert.IsTrue(jugador.GetComponent<PlayerShooter>().enabled, "A pie vuelve a lanzar");
    }

    [UnityTest]
    public IEnumerator Montura_CorreMasRapidoYSaltaMasAlto()
    {
        float aPie = 0f, montado = 0f;
        yield return VelocidadCorriendo(v => aPie = v);
        montura.Liberar(new Vector3(jugador.transform.position.x, jugador.transform.position.y, 0f), 1);
        montura.Montar();
        yield return VelocidadCorriendo(v => montado = v);

        Assert.Greater(montado, aPie * 1.3f, $"Montado corre más rápido ({montado:0.0} contra {aPie:0.0})");
        Assert.Greater(jugador.MultiplicadorSalto, 1f, "Montado salta más alto");
    }

    [UnityTest]
    public IEnumerator Montura_LosGolpesSeLosLlevaElAlebrije()
    {
        montura.Montar();
        int vidaMateo = salud.VidaActual, vidaMontura = montura.Salud.VidaActual;
        salud.RecibirDano(15);
        yield return null;
        Assert.AreEqual(vidaMateo, salud.VidaActual, "Mateo no pierde vida mientras va montado");
        Assert.AreEqual(vidaMontura - 15, montura.Salud.VidaActual, "La montura tiene su propia vida");
    }

    [UnityTest]
    public IEnumerator Montura_SiSeAgotaMateoCaeYSigueAPie()
    {
        montura.Montar();
        montura.Salud.RecibirDano(1000);
        yield return new WaitForSeconds(0.3f);
        Assert.IsFalse(montura.Montado, "Sin vida, Mateo se cae");
        Assert.AreEqual(1f, jugador.MultiplicadorVelocidad, "Sigue a pie con su velocidad normal");
        Assert.IsTrue(jugador.GetComponent<PlayerShooter>().enabled, "Vuelve a lanzar cerámica");

        yield return new WaitForSeconds(2f);
        int vida = salud.VidaActual;
        salud.RecibirDano(10);
        Assert.Less(salud.VidaActual, vida, "Los golpes vuelven a ser para Mateo");
        Assert.IsFalse(montura.gameObject.activeSelf, "El alebrije cansado se va");
    }

    [UnityTest]
    public IEnumerator Montura_ZarpazoLastimaAlEnemigoDeEnfrente()
    {
        montura.Montar();
        var minero = Object.FindFirstObjectByType<IAEsqueleto>();
        var saludMinero = minero.GetComponent<Health>();
        int vida = saludMinero.VidaActual;
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        minero.transform.position = jugador.transform.position + Vector3.right * 1.8f;
        minero.GetComponent<Rigidbody2D>().position = minero.transform.position;
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(1, jugador.DireccionMirada);
        yield return new WaitForSeconds(0.3f);                 // Al montar hay un instante sin zarpazo

        yield return Pulsar(Key.Space);
        yield return new WaitForSeconds(0.1f);
        Assert.Less(saludMinero.VidaActual, vida, "El zarpazo lastima al enemigo que tiene enfrente");
    }
}
