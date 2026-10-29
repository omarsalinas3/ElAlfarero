using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #38: las cerámicas se reciclan en lugar de crearse y destruirse, y los niveles 1 y 2
/// corren fluidos mientras Mateo avanza disparando (FPS promedio y peor cuadro). Los números salen en el log.
/// </summary>
public class RendimientoTests
{
    Keyboard teclado;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        teclado = InputSystem.AddDevice<Keyboard>();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        InputSystem.RemoveDevice(teclado);
        Time.timeScale = 1f;
    }

    [UnityTest]
    public IEnumerator Disparar_ReutilizaLasCeramicas()
    {
        yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.6f);

        // 4 s disparando a 8 por segundo = ~32 lanzamientos; con alcance de 1.5 s nunca hay más de ~12 en el aire
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.Space));
        yield return new WaitForSeconds(4f);
        InputSystem.QueueStateEvent(teclado, new KeyboardState());
        yield return null;

        Debug.Log($"[Rendimiento] Reciclaje en Sandbox: {Reciclaje.Creados} creados, {Reciclaje.Reutilizados} reutilizados");
        Assert.Greater(Reciclaje.Reutilizados, 10, "Las cerámicas que ya terminaron se vuelven a usar");
        Assert.Less(Reciclaje.Creados, 25, "No se crea una cerámica nueva por cada lanzamiento");
    }

    [UnityTest]
    public IEnumerator Nivel1_CorreFluidoDisparando() { yield return MedirNivel("Nivel1_Dolores"); }

    [UnityTest]
    public IEnumerator Nivel2_CorreFluidoDisparando() { yield return MedirNivel("Nivel2_Pozos"); }

    /// <summary>
    /// Mateo cruza el nivel disparando sin parar (invulnerable) mientras se miden los cuadros. Cada segundo se adelanta
    /// un tramo para que la medición pase por todo el recorrido (si no, se queda trabado en el primer huacal).
    /// </summary>
    IEnumerator MedirNivel(string escena)
    {
        yield return SceneManager.LoadSceneAsync(escena, LoadSceneMode.Single);
        yield return new WaitForSeconds(1f);                 // Que pase la carga inicial
        yield return EsperaLlegada.Terminar();               // Nivel 2: que termine la llegada montada
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var rb = jugador.GetComponent<Rigidbody2D>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        float xInicio = rb.position.x, xFin = GestorNivel.Actual != null ? xInicio + 70f : xInicio;

        var cuadros = new List<float>();
        int gcAntes = System.GC.CollectionCount(0);
        InputSystem.QueueStateEvent(teclado, new KeyboardState(Key.RightArrow, Key.Space));
        for (float t = 0f, siguienteSalto = 1f; t < 12f; t += Time.unscaledDeltaTime)
        {
            if (t >= siguienteSalto)
            {
                siguienteSalto += 1f;
                var pos = new Vector2(Mathf.Lerp(xInicio, xFin, t / 12f), rb.position.y + 0.1f);
                rb.position = pos;
                jugador.transform.position = pos;
            }
            yield return null;
            cuadros.Add(Time.unscaledDeltaTime);
        }
        InputSystem.QueueStateEvent(teclado, new KeyboardState());

        cuadros.RemoveAt(0);
        cuadros.Sort();
        float promedio = cuadros.Count / cuadros.Sum();
        float p99 = cuadros[(int)(cuadros.Count * 0.99f)] * 1000f;
        float peor = cuadros[cuadros.Count - 1] * 1000f;
        int recolecciones = System.GC.CollectionCount(0) - gcAntes;
        Debug.Log($"[Rendimiento] {escena}: {promedio:0.0} FPS promedio, 99% de cuadros en menos de {p99:0.0} ms, " +
                  $"peor cuadro {peor:0.0} ms, {recolecciones} recolecciones de basura, x final {jugador.transform.position.x:0.0}, " +
                  $"reciclaje {Reciclaje.Creados} creados / {Reciclaje.Reutilizados} reutilizados");

        Assert.GreaterOrEqual(promedio, 55f, "60 FPS (VSync) con margen");
        Assert.Less(p99, 33.4f, "Sin tirones: casi todos los cuadros por debajo de 2 cuadros de 60 Hz");
    }
}
