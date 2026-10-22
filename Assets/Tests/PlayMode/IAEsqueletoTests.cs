using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la IA del minero esqueleto en el Nivel 1: patrulla, persigue, lastima, reacciona al golpe y regresa.
/// Se usa el minero de x=27, lejos de donde aparece Mateo, para que nada lo active antes de tiempo.
/// </summary>
public class IAEsqueletoTests
{
    const float XMinero = 27f;

    IAEsqueleto minero;
    PlayerController jugador;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        minero = Object.FindObjectsByType<IAEsqueleto>(FindObjectsSortMode.None)
            .OrderBy(m => Mathf.Abs(m.transform.position.x - XMinero)).FirstOrDefault();
        jugador = Object.FindFirstObjectByType<PlayerController>();
        Assert.IsNotNull(minero, "El Nivel 1 debe tener mineros con IAEsqueleto");
        Assert.Less(Mathf.Abs(minero.Origen.x - XMinero), 0.5f, "Falta el minero de x=27");
        Assert.IsNotNull(jugador);
    }

    [TearDown]
    public void Limpiar() { Time.timeScale = 1f; }

    void MoverJugador(float x)
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 pos = new Vector2(x, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        rb.linearVelocity = Vector2.zero;
    }

    [UnityTest]
    public IEnumerator Patrulla_SeMueveSinSalirDeSuZona()
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        for (float t = 0f; t < 5f; t += Time.deltaTime)
        {
            float x = minero.transform.position.x;
            minX = Mathf.Min(minX, x);
            maxX = Mathf.Max(maxX, x);
            yield return null;
        }
        Assert.AreEqual(IAEsqueleto.Estado.Patrullar, minero.EstadoActual);
        Assert.Greater(maxX - minX, 1f, "Debe caminar de un lado a otro");
        Assert.Less(Mathf.Max(maxX - minero.Origen.x, minero.Origen.x - minX), 3.2f, "No debe salir de su radio de patrulla");
    }

    [UnityTest]
    public IEnumerator Persecucion_AlVerAMateoLoSigue()
    {
        MoverJugador(XMinero - 2.8f);   // Justo a la derecha del huacal de x=23
        yield return new WaitForSeconds(0.3f);
        Assert.That(minero.EstadoActual, Is.EqualTo(IAEsqueleto.Estado.Perseguir).Or.EqualTo(IAEsqueleto.Estado.Atacar),
            "Al tener a Mateo enfrente debe perseguirlo");
        Assert.AreEqual(-1, minero.Direccion, "Debe voltear hacia Mateo (a la izquierda)");
    }

    [UnityTest]
    public IEnumerator Ataque_LastimaAMateo()
    {
        Health salud = jugador.GetComponent<Health>();
        MoverJugador(XMinero - 2.5f);
        float limite = Time.time + 4f;
        while (salud.VidaActual == salud.VidaMaxima && Time.time < limite) yield return null;
        Assert.Less(salud.VidaActual, salud.VidaMaxima, "El esqueleto debe alcanzar y lastimar a Mateo");
    }

    [UnityTest]
    public IEnumerator Golpe_LoAturdeYLuegoPersigue()
    {
        minero.GetComponent<Health>().RecibirDano(10);
        yield return new WaitForFixedUpdate();
        Assert.AreEqual(IAEsqueleto.Estado.Aturdido, minero.EstadoActual);
        yield return new WaitForSeconds(0.4f);
        Assert.AreNotEqual(IAEsqueleto.Estado.Aturdido, minero.EstadoActual, "El aturdimiento debe durar poco");
    }

    [UnityTest]
    public IEnumerator Regreso_SiPierdeAMateoVuelveASuZona()
    {
        MoverJugador(XMinero - 2.8f);   // Justo a la derecha del huacal de x=23
        yield return new WaitForSeconds(0.6f);
        MoverJugador(-6f);   // Mateo se va lejos: deja de verlo
        float limite = Time.time + 8f;
        while (Mathf.Abs(minero.transform.position.x - minero.Origen.x) > 3f && Time.time < limite) yield return null;
        yield return new WaitForSeconds(2f);
        Assert.That(minero.EstadoActual, Is.EqualTo(IAEsqueleto.Estado.Regresar).Or.EqualTo(IAEsqueleto.Estado.Patrullar));
        Assert.Less(Mathf.Abs(minero.transform.position.x - minero.Origen.x), 3.2f, "Debe volver a su zona de patrulla");
    }
}
