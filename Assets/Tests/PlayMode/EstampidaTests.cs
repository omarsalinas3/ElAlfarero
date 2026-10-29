using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #53: la estampida de nahuales en la cañada del Nivel 2 avisa, encierra a Mateo, los nahuales
/// cruzan a toda velocidad y lastiman al tocar (salvo evadiendo), y al pasar la última manada Mateo sigue.
/// </summary>
public class EstampidaTests
{
    PlayerController jugador;
    Health salud;
    Estampida estampida;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        yield return SceneManager.LoadSceneAsync("Nivel2_Pozos", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        jugador = Object.FindFirstObjectByType<PlayerController>();
        salud = jugador.GetComponent<Health>();
        estampida = Object.FindFirstObjectByType<Estampida>();
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        GestorGuardado.Borrar();
    }

    void MoverJugador(float x)
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        var pos = new Vector2(x, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        rb.linearVelocity = Vector2.zero;
    }

    static Nahual[] Corriendo() => Object.FindObjectsByType<Nahual>(FindObjectsSortMode.None);

    [UnityTest]
    public IEnumerator Estampida_AvisaEncierraYSeLiberaAlPasarLaUltimaManada()
    {
        Assert.IsNotNull(estampida, "La cañada del Nivel 2 tiene la estampida");
        var camara = Camera.main.GetComponent<CameraFollow>();
        salud.HacerInvulnerable(999f);
        MoverJugador(estampida.transform.position.x);
        yield return new WaitForSeconds(0.3f);

        Assert.IsTrue(estampida.Activa, "Pisar la cañada dispara la estampida");
        Assert.IsTrue(camara.Bloqueada, "La cámara se queda fija");

        Time.timeScale = 3f;
        bool vioNahuales = false, vioRapidos = false;
        float limite = Time.realtimeSinceStartup + 20f;
        while (!estampida.Terminada && Time.realtimeSinceStartup < limite)
        {
            var n = Corriendo();
            vioNahuales |= n.Length > 0;
            vioRapidos |= n.Any(x => x.Velocidad >= 15f);
            yield return null;
        }
        Time.timeScale = 1f;

        Assert.IsTrue(vioNahuales, "Los nahuales cruzan la pantalla");
        Assert.IsTrue(vioRapidos, "Corren a toda velocidad (más rápido que Mateo)");
        Assert.IsTrue(estampida.Terminada, "Al pasar la última manada termina");
        Assert.IsFalse(camara.Bloqueada, "La cámara vuelve a seguir a Mateo");
        Assert.AreEqual("CRUZA EL PUEBLO FANTASMA", GestorNivel.Actual.Objetivo, "Regresa el objetivo del recorrido");
    }

    [UnityTest]
    public IEnumerator Nahual_LastimaAlTocarPeroNoAlEvadir()
    {
        MoverJugador(estampida.transform.position.x - 20f);   // Lejos de la cañada: sin estampida
        yield return new WaitForSeconds(0.3f);

        // Un nahual que viene directo hacia Mateo
        SoltarContra(jugador.transform.position.x);
        int vidaAntes = salud.VidaActual;
        yield return new WaitForSeconds(0.6f);
        Assert.Less(salud.VidaActual, vidaAntes, "Tocar a un nahual lastima");

        // Evadiendo lo atraviesa sin daño
        yield return new WaitForSeconds(1.5f);                // Que pase la invulnerabilidad del golpe
        int vidaEvasion = salud.VidaActual;
        salud.Invulnerable = true;                            // Lo mismo que hace la evasión durante sus cuadros
        SoltarContra(jugador.transform.position.x);
        yield return new WaitForSeconds(0.6f);
        salud.Invulnerable = false;
        Assert.AreEqual(vidaEvasion, salud.VidaActual, "Evadiendo no lastima");
    }

    Nahual Prefab() => (Nahual)typeof(Estampida).GetField("prefabNahual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                                                 .GetValue(estampida);

    Nahual SoltarContra(float x)
    {
        var n = Reciclaje.Sacar(Prefab(), new Vector3(x + 4f, -4.5f, 0f), Quaternion.identity);
        n.Correr(-1, 15f);
        return n;
    }

    [UnityTest]
    public IEnumerator Nahual_SeSaltaPorEncima()
    {
        // La parte que lastima del nahual queda más baja que el punto más alto del salto de Mateo
        var prefab = Prefab();
        var caja = prefab.GetComponent<BoxCollider2D>();
        float alto = (caja.offset.y + caja.size.y / 2f) * prefab.transform.localScale.y;

        var rb = jugador.GetComponent<Rigidbody2D>();
        float y0 = rb.position.y, maximo = y0;
        var teclado = InputSystem.AddDevice<Keyboard>();
        try
        {
            var estado = new KeyboardState(Key.W);
            InputSystem.QueueStateEvent(teclado, estado);
            for (float t = 0f; t < 1f; t += Time.deltaTime)
            {
                maximo = Mathf.Max(maximo, rb.position.y);
                yield return null;
            }
        }
        finally { InputSystem.RemoveDevice(teclado); }

        Assert.Greater(maximo - y0, alto + 0.3f, $"El salto ({maximo - y0:0.0} u) libra al nahual ({alto:0.0} u)");
    }
}
