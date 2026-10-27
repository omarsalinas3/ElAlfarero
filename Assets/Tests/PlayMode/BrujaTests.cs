using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #54 en el Nivel 2: la Bruja de Fuego vuela en zigzag, suelta bolas de fuego, se lanza en
/// picada hacia Mateo y se derriba con la cerámica. Se usa la bruja de la calle (x = 18).
/// </summary>
public class BrujaTests
{
    const float XBruja = 18f;

    IABruja bruja;
    PlayerController jugador;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Nivel2_Pozos", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        bruja = Object.FindObjectsByType<IABruja>().OrderBy(b => Mathf.Abs(b.transform.position.x - XBruja)).FirstOrDefault();
        jugador = Object.FindFirstObjectByType<PlayerController>();
        Assert.IsNotNull(bruja, "El Nivel 2 tiene Brujas de Fuego");
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
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
    public IEnumerator Bruja_VuelaEnZigzagSinTocarElSuelo()
    {
        float minY = float.MaxValue, maxY = float.MinValue;
        for (float t = 0f; t < 3f; t += Time.deltaTime)
        {
            minY = Mathf.Min(minY, bruja.transform.position.y);
            maxY = Mathf.Max(maxY, bruja.transform.position.y);
            yield return null;
        }
        Assert.Greater(maxY - minY, 0.6f, "Sube y baja en zigzag");
        Assert.Greater(minY, -2f, "Vuela: no baja al suelo mientras no ataca");
    }

    [UnityTest]
    public IEnumerator Bruja_AlVerAMateoSueltaBolasDeFuegoYSeLanzaEnPicada()
    {
        MoverJugador(XBruja - 6f);
        float alturaVuelo = bruja.transform.position.y, minY = float.MaxValue;
        bool bola = false, picada = false;
        float finMedicion = 11f;
        for (float t = 0f; t < finMedicion; t += Time.deltaTime)
        {
            bola |= Object.FindObjectsByType<BalaEnemiga>().Any(b => b.name.StartsWith("Bola_Fuego"));
            if (!picada && bruja.EstadoActual == IABruja.Estado.Picada)
            {
                picada = true;
                finMedicion = Mathf.Min(finMedicion, t + 1.5f);   // Se sigue midiendo mientras baja
            }
            minY = Mathf.Min(minY, bruja.transform.position.y);
            yield return null;
        }
        Assert.IsTrue(bola, "Suelta bolas de fuego hacia Mateo");
        Assert.IsTrue(picada, "Se lanza en picada");
        Assert.Less(minY, alturaVuelo - 1.5f, "En la picada baja hacia Mateo");
    }

    [UnityTest]
    public IEnumerator Bruja_SeDerribaConLaCeramica()
    {
        var salud = bruja.GetComponent<Health>();
        var ceramica = jugador.GetComponent<PlayerShooter>().CeramicaActual;
        int vidaInicial = salud.VidaActual;

        // Una cerámica de verdad lanzada hacia ella
        Vector2 hacia = (Vector2)bruja.transform.position - new Vector2(bruja.transform.position.x - 3f, bruja.transform.position.y);
        var p = Reciclaje.Sacar(ceramica.prefabProyectil, bruja.transform.position - new Vector3(3f, 0f, 0f), Quaternion.identity);
        p.Inicializar(ceramica, hacia);
        yield return new WaitForSeconds(0.5f);
        Assert.Less(salud.VidaActual, vidaInicial, "La cerámica le hace daño");

        salud.RecibirDano(9999);
        yield return new WaitForSeconds(0.3f);
        Assert.AreEqual(IABruja.Estado.Muerta, bruja.EstadoActual);
        Assert.IsFalse(bruja.GetComponent<Collider2D>().enabled, "Ya no lastima ni estorba");

        yield return new WaitForSeconds(3.5f);
        Assert.IsTrue(bruja == null, "Cae, se hace cenizas y desaparece");
    }
}
