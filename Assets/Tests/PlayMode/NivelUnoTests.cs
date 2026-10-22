using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de lo agregado al Nivel 1: monedas por recogibles sobrantes, muerte animada de Mateo, bandidos,
/// emboscada en oleadas y el jefe (cinemática de las torres y victoria al vencer al alebrije).
/// </summary>
public class NivelUnoTests
{
    PlayerController jugador;
    Health salud;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        jugador = Object.FindFirstObjectByType<PlayerController>();
        salud = jugador.GetComponent<Health>();
    }

    [TearDown]
    public void Limpiar() { Time.timeScale = 1f; }

    void MoverJugador(float x)
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        var pos = new Vector2(x, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        rb.linearVelocity = Vector2.zero;
    }

    [UnityTest]
    public IEnumerator Guacamaya_ConVidaLlenaSeCambiaPorMonedas()
    {
        var inventario = GestorNivel.Actual.Inventario;
        int monedasAntes = inventario.Monedas;
        var guacamaya = Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).First(r => r.Tipo == TipoRecogible.Guacamaya);
        int esperadas = guacamaya.MonedasSiLleno;

        guacamaya.transform.position = jugador.transform.position + Vector3.up;
        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(guacamaya == null, "Con la vida llena la guacamaya no se queda tirada");
        Assert.AreEqual(monedasAntes + esperadas, inventario.Monedas, "Se cambia por monedas");
    }

    [UnityTest]
    public IEnumerator Caguama_AlMaximoSeCambiaPorMonedas()
    {
        var inventario = GestorNivel.Actual.Inventario;
        inventario.AgregarCaguamas(inventario.MaximoCaguamas);
        int monedasAntes = inventario.Monedas;
        var caguama = Object.FindObjectsByType<Recogible>(FindObjectsSortMode.None).First(r => r.Tipo == TipoRecogible.Caguama);

        caguama.transform.position = jugador.transform.position + Vector3.up;
        yield return new WaitForSeconds(0.2f);

        Assert.IsTrue(caguama == null);
        Assert.AreEqual(inventario.MaximoCaguamas, inventario.Caguamas);
        Assert.Greater(inventario.Monedas, monedasAntes);
    }

    [UnityTest]
    public IEnumerator Muerte_MateoReproduceSuAnimacion()
    {
        salud.RecibirDano(1000);
        yield return new WaitForSeconds(0.3f);
        var animador = jugador.GetComponentInChildren<Animator>();
        Assert.IsTrue(animador.GetCurrentAnimatorStateInfo(0).IsName("Muerte"), "Debe verse la animación de muerte");
    }

    [UnityTest]
    public IEnumerator Bandido_ConRevolverLeDisparaAMateo()
    {
        var bandido = Object.FindObjectsByType<IABandido>(FindObjectsSortMode.None).First(b => b.TipoArma == IABandido.Arma.Revolver);
        var rb = bandido.GetComponent<Rigidbody2D>();
        rb.position = new Vector2(jugador.transform.position.x + 4.5f, rb.position.y);

        float limite = Time.time + 4f;
        while (salud.VidaActual == salud.VidaMaxima && Time.time < limite) yield return null;
        Assert.Less(salud.VidaActual, salud.VidaMaxima, "La bala del bandido debe lastimar a Mateo");
    }

    [UnityTest]
    public IEnumerator Bandido_AlMorirSeDesvanece()
    {
        var bandido = Object.FindObjectsByType<IABandido>(FindObjectsSortMode.None).First();
        bandido.GetComponent<Health>().RecibirDano(1000);
        yield return null;
        Assert.AreEqual(IABandido.Estado.Muerto, bandido.EstadoActual);
        yield return new WaitForSeconds(3.5f);
        Assert.IsTrue(bandido == null, "El cuerpo desaparece después de la animación");
    }

    [UnityTest]
    public IEnumerator Emboscada_EncierraYSeLiberaAlVencerLasOleadas()
    {
        var emboscada = Object.FindFirstObjectByType<Emboscada>();
        var camara = Camera.main.GetComponent<CameraFollow>();
        salud.HacerInvulnerable(999f);                 // Que los bandidos no maten a Mateo mientras dura la prueba
        MoverJugador(emboscada.transform.position.x);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(emboscada.Activa, "Pisar la zona dispara la emboscada");
        Assert.IsTrue(camara.Bloqueada, "La cámara se queda fija");

        float limite = Time.time + 30f;
        while (!emboscada.Terminada && Time.time < limite)
        {
            foreach (var b in emboscada.GetComponentsInChildren<IABandido>())
                if (b.EstadoActual != IABandido.Estado.Muerto) b.GetComponent<Health>().RecibirDano(1000);
            yield return new WaitForSeconds(0.25f);
        }
        Assert.IsTrue(emboscada.Terminada, "Al caer todas las oleadas la emboscada termina");
        Assert.IsFalse(camara.Bloqueada, "La cámara vuelve a seguir a Mateo");
    }

    [UnityTest]
    public IEnumerator Jefe_CinematicaYVictoriaAlVencerAlAlebrije()
    {
        var encuentro = Object.FindFirstObjectByType<EncuentroAlebrije>();
        salud.HacerInvulnerable(999f);
        MoverJugador(encuentro.transform.position.x);
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(encuentro.Iniciado);
        Assert.AreEqual(GestorNivel.Estado.Cinematica, GestorNivel.Actual.EstadoActual, "Durante la cinemática Mateo no se controla");

        float limite = Time.time + 12f;
        while (!encuentro.PeleaEmpezada && Time.time < limite) yield return null;
        Assert.IsTrue(encuentro.PeleaEmpezada, "Tras la cinemática empieza la pelea");
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual);

        var jefe = Object.FindFirstObjectByType<JefeAlebrije>();
        Assert.AreNotEqual(JefeAlebrije.Estado.Dormido, jefe.EstadoActual);
        jefe.Salud.RecibirDano(10000);
        yield return new WaitForSeconds(4.6f);
        Assert.AreEqual(GestorNivel.Estado.Victoria, GestorNivel.Actual.EstadoActual, "Vencer al alebrije completa el nivel");
    }
}
