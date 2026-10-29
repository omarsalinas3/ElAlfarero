using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #56: el Jinete sin Cabeza en el patio de la capilla del Nivel 2. Cinemática de entrada con
/// barra de vida, ataques (carga de lado a lado, sablazo y calavera en llamas) y segunda fase a la mitad de la vida.
/// </summary>
public class JineteTests
{
    PlayerController jugador;
    Health salud;
    EncuentroJinete encuentro;
    JefeJinete jefe;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        yield return SceneManager.LoadSceneAsync("Nivel2_Pozos", LoadSceneMode.Single);
        yield return EsperaLlegada.Terminar();
        jugador = Object.FindFirstObjectByType<PlayerController>();
        salud = jugador.GetComponent<Health>();
        salud.HacerInvulnerable(999f);
        encuentro = Object.FindFirstObjectByType<EncuentroJinete>();
        jefe = Object.FindFirstObjectByType<JefeJinete>(FindObjectsInactive.Include);
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        GestorGuardado.Borrar();
    }

    IEnumerator EmpezarPelea()
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        var pos = new Vector2(encuentro.transform.position.x, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        float limite = Time.time + 10f;
        while (!encuentro.PeleaEmpezada && Time.time < limite) yield return null;
    }

    [UnityTest]
    public IEnumerator Jinete_CinematicaDeEntradaYBarraDeVida()
    {
        Assert.IsNotNull(encuentro, "El patio de la capilla tiene el encuentro con el Jinete");
        Assert.IsFalse(jefe.gameObject.activeSelf, "Nadie lo ve antes de la cinemática");
        Assert.IsNull(GameObject.Find("Meta_Capilla"), "Ya no hay meta provisional");

        var rb = jugador.GetComponent<Rigidbody2D>();
        var pos = new Vector2(encuentro.transform.position.x, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        yield return new WaitForSeconds(0.3f);
        Assert.IsTrue(encuentro.Iniciado);
        Assert.AreEqual(GestorNivel.Estado.Cinematica, GestorNivel.Actual.EstadoActual, "Durante la entrada Mateo no se controla");
        Assert.IsTrue(Camera.main.GetComponent<CameraFollow>().Bloqueada, "El patio queda cerrado");

        float limite = Time.time + 10f;
        while (!encuentro.PeleaEmpezada && Time.time < limite) yield return null;
        Assert.IsTrue(encuentro.PeleaEmpezada, "Tras la cinemática empieza la pelea");
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual);
        Assert.IsTrue(Object.FindFirstObjectByType<BarraJefe>() != null, "Se ve la barra de vida del jefe");
        Assert.AreEqual("DERROTA AL JINETE SIN CABEZA", GestorNivel.Actual.Objetivo);
    }

    [UnityTest]
    public IEnumerator Jinete_CargaCruzaLaArenaYLanzaCalaveras()
    {
        yield return EmpezarPelea();
        float minX = jefe.transform.position.x, maxX = minX;
        bool cargo = false, lanzo = false;
        Time.timeScale = 2f;
        float limite = Time.realtimeSinceStartup + 25f;
        while (!(cargo && lanzo) && Time.realtimeSinceStartup < limite)
        {
            minX = Mathf.Min(minX, jefe.transform.position.x);
            maxX = Mathf.Max(maxX, jefe.transform.position.x);
            cargo |= jefe.EstadoActual == JefeJinete.Estado.Carga && maxX - minX > 8f;
            lanzo |= Object.FindObjectsByType<BalaEnemiga>(FindObjectsSortMode.None).Any(b => b.gameObject.activeInHierarchy);
            yield return null;
        }
        Time.timeScale = 1f;
        Assert.IsTrue(cargo, $"Carga a galope cruzando la arena (recorrió {maxX - minX:0.0} u)");
        Assert.IsTrue(lanzo, "Lanza la calavera en llamas de su linterna");
    }

    [UnityTest]
    public IEnumerator Jinete_SegundaFaseALaMitadYVictoria()
    {
        yield return EmpezarPelea();
        jefe.Salud.RecibirDano(jefe.Salud.VidaMaxima / 2 + 1);
        yield return new WaitForSeconds(1.5f);
        Assert.IsTrue(jefe.Furioso, "A la mitad de la vida pasa a su segunda fase");

        jefe.Salud.RecibirDano(10000);
        yield return new WaitForSeconds(3.5f);
        Assert.AreEqual(GestorNivel.Estado.Victoria, GestorNivel.Actual.EstadoActual, "Vencerlo completa el nivel");
    }

    [Test]
    public void Jinete_ElSaltoLibraLaCarga()
    {
        // La parte que lastima queda por debajo del salto de Mateo (~3.3 u), para esquivar la carga saltando
        var caja = jefe.GetComponent<BoxCollider2D>();
        float alto = (caja.offset.y + caja.size.y / 2f) * jefe.transform.localScale.y;
        Assert.Less(alto, 3f, $"El jinete lastima hasta {alto:0.0} u de alto");
    }
}
