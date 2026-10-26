using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

/// <summary>
/// Pruebas de la issue #29: puntaje (enemigos, objetos rotos, monedas), cronómetro y objetivo en el HUD,
/// y el puntaje en la pantalla final (es el valor que guardará el récord del sistema de guardado).
/// </summary>
public class HudPuntajeTests
{
    GestorNivel gestor;
    PlayerController jugador;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        gestor = GestorNivel.Actual;
        jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);   // Que nada lo mate mientras dura la prueba
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

    static Text TextoHud(string nombre) =>
        Object.FindObjectsByType<Text>().First(t => t.name == nombre && t.GetComponentInParent<HudJuego>() != null);

    [UnityTest]
    public IEnumerator Puntaje_SumaEnemigosObjetosYMonedas()
    {
        int inicial = gestor.Puntaje;

        var esqueleto = Object.FindObjectsByType<IAEsqueleto>().OrderBy(e => e.transform.position.x).First();
        esqueleto.GetComponent<Health>().RecibirDano(9999);
        yield return null;
        Assert.AreEqual(inicial + 100, gestor.Puntaje, "Derrotar un esqueleto suma 100");

        var huacal = Object.FindObjectsByType<Destructible>().Where(d => d.name.StartsWith("Huacal")).OrderBy(d => d.transform.position.x).First();
        huacal.GetComponent<Health>().RecibirDano(9999);
        yield return null;
        Assert.AreEqual(inicial + 150, gestor.Puntaje, "Romper un huacal suma 50");

        gestor.Inventario.AgregarMonedas(3);
        Assert.AreEqual(inicial + 180, gestor.Puntaje, "Cada moneda suma 10");
    }

    [UnityTest]
    public IEnumerator Hud_MuestraPuntajeTiempoYObjetivo()
    {
        gestor.SumarPuntos(1250);
        yield return new WaitForSeconds(2.2f);

        Assert.AreEqual("PUNTOS " + gestor.Puntaje.ToString("000000"), TextoHud("Puntaje").text);
        Assert.AreEqual("00:02", TextoHud("Tiempo").text, "El cronómetro corre desde que empieza el nivel");
        Assert.AreEqual("LLEGA A LA PARROQUIA", TextoHud("Objetivo").text);

        var barra = Object.FindObjectsByType<Image>().First(i => i.name == "Avance");
        float antes = barra.fillAmount;
        MoverJugador(30f);
        yield return new WaitForSeconds(0.2f);
        Assert.Greater(barra.fillAmount, antes + 0.3f, "La barra del objetivo avanza con el recorrido");
    }

    [UnityTest]
    public IEnumerator Objetivo_CambiaDuranteLaEmboscada()
    {
        var emboscada = Object.FindFirstObjectByType<Emboscada>();
        MoverJugador(emboscada.transform.position.x);
        yield return new WaitForSeconds(0.3f);

        Assert.IsTrue(emboscada.Activa);
        StringAssert.StartsWith("SOBREVIVE A LA EMBOSCADA", gestor.Objetivo);
        yield return null;
        Assert.AreEqual(gestor.Objetivo, TextoHud("Objetivo").text);
        Assert.IsFalse(Object.FindObjectsByType<Image>(FindObjectsInactive.Include).First(i => i.name == "Avance").gameObject.activeInHierarchy,
                       "Durante la emboscada no hay barra de recorrido");
    }

    [UnityTest]
    public IEnumerator Victoria_MuestraElPuntajeEnElResumen()
    {
        gestor.SumarPuntos(500);
        int puntaje = gestor.Puntaje;
        gestor.Completar();
        yield return new WaitForSeconds(0.6f);

        var resumen = Object.FindObjectsByType<Text>().First(t => t.name == "Resumen" && t.gameObject.activeInHierarchy);
        StringAssert.Contains("PUNTOS    " + puntaje, resumen.text);
    }
}
