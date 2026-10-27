using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #32: el Nivel 2 (Mineral de Pozos) es más difícil que Dolores, deja libres la cañada
/// para la estampida y el patio de la capilla para el Jinete, y llegar a la capilla lo completa y lo guarda.
/// </summary>
public class NivelDosTests
{
    [SetUp]
    public void Preparar()
    {
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        GestorGuardado.Borrar();
    }

    struct Conteo { public int enemigos, curacion; }

    static IEnumerator Contar(string escena, System.Action<Conteo> resultado)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return null;
        resultado(new Conteo
        {
            enemigos = Object.FindObjectsByType<IAEsqueleto>().Length + Object.FindObjectsByType<IABandido>().Length,
            // Guacamayas y caguamas sueltas o dentro de huacales y hornos
            curacion = Object.FindObjectsByType<Transform>().Count(t => t.parent != null && (t.parent.name == "Obstaculos" || t.parent.name == "Recogibles")
                                                                     && (t.name.Contains("Guacamaya") || t.name.Contains("Caguama"))),
        });
    }

    [UnityTest]
    public IEnumerator Nivel2_MasEnemigosYMenosCuracionQueDolores()
    {
        Conteo uno = default, dos = default;
        yield return Contar("Nivel1_Dolores", c => uno = c);
        yield return Contar("Nivel2_Pozos", c => dos = c);

        Assert.Greater(dos.enemigos, uno.enemigos, $"Más enemigos de entrada ({dos.enemigos} contra {uno.enemigos})");
        Assert.Less(dos.curacion, uno.curacion, $"Menos curación ({dos.curacion} contra {uno.curacion})");
    }

    [UnityTest]
    public IEnumerator Nivel2_CanadaYCapillaQuedanLibres()
    {
        yield return SceneManager.LoadSceneAsync("Nivel2_Pozos", LoadSceneMode.Single);
        yield return null;
        float estampida = GameObject.Find("Zona_Estampida").transform.position.x;
        float arena = GameObject.Find("Arena_Jinete").transform.position.x;

        var estorbos = Object.FindObjectsByType<Destructible>().Select(d => d.transform.position.x)
                             .Concat(Object.FindObjectsByType<IAEsqueleto>().Select(e => e.transform.position.x))
                             .Concat(Object.FindObjectsByType<IABandido>().Select(b => b.transform.position.x));
        Assert.IsFalse(estorbos.Any(x => Mathf.Abs(x - estampida) < 7.5f), "La cañada queda libre para la estampida (#53)");
        Assert.IsFalse(estorbos.Any(x => Mathf.Abs(x - arena) < 10.5f), "El patio de la capilla queda libre para el Jinete (#56)");
    }

    [UnityTest]
    public IEnumerator Nivel2_LlegarALaCapillaLoCompletaYLoGuarda()
    {
        yield return SceneManager.LoadSceneAsync("Nivel2_Pozos", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        var gestor = GestorNivel.Actual;
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        Assert.AreEqual("CRUZA EL PUEBLO FANTASMA", gestor.Objetivo);

        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 pos = new Vector2(GameObject.Find("Meta_Capilla").transform.position.x - 1f, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        rb.linearVelocity = new Vector2(4f, 0f);
        yield return new WaitForSeconds(1f);

        Assert.AreEqual(GestorNivel.Estado.Victoria, gestor.EstadoActual, "Llegar a la capilla completa el nivel");
        Assert.IsNotNull(GestorGuardado.Datos.Record(2), "Se guarda el récord del Nivel 2");
        Assert.AreEqual(3, GestorGuardado.Datos.nivelDesbloqueado);
    }
}
