using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #50 (montura en la transición al Nivel 2): Mateo llega a Mineral de Pozos montado en el
/// alebrije, una bola de fuego de la Bruja lo tumba sin quitarle vida y sigue a pie con el control de vuelta.
/// </summary>
public class LlegadaMontadoTests
{
    [SetUp]
    public void Preparar() => GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);

    [TearDown]
    public void Limpiar() => GestorGuardado.Borrar();

    [UnityTest]
    public IEnumerator Nivel2_LlegaMontadoYLaBrujaLoTumba()
    {
        yield return SceneManager.LoadSceneAsync("Nivel2_Pozos", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.3f);

        var llegada = Object.FindFirstObjectByType<LlegadaMontado>();
        var montura = Object.FindFirstObjectByType<MonturaAlebrije>(FindObjectsInactive.Include);
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        Assert.IsNotNull(llegada, "El Nivel 2 empieza con la llegada montada");
        Assert.IsTrue(montura.Montado, "Mateo llega montado en el alebrije");
        Assert.AreEqual(GestorNivel.Estado.Cinematica, GestorNivel.Actual.EstadoActual, "Mientras llega no se controla");
        int vida = salud.VidaActual;

        yield return EsperaLlegada.Terminar();
        Assert.IsTrue(llegada.Terminada);
        Assert.IsFalse(montura.Montado, "La bola de fuego de la Bruja lo tumba");
        Assert.AreEqual(vida, salud.VidaActual, "Caerse no le quita vida");
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual, "Después sigue a pie con el control");
        Assert.IsTrue(jugador.GetComponent<PlayerShooter>().enabled, "Y vuelve a lanzar cerámica");

        yield return new WaitForSeconds(1.5f);
        Assert.IsFalse(montura.gameObject.activeSelf, "El alebrije se va volando");
    }
}
