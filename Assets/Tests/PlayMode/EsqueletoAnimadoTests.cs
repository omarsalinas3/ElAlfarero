using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// Pruebas de la issue #51: el minero esqueleto del Nivel 1 camina, golpea con el pico y se desarma al morir
/// (animaciones de Flow), sin perder su IA. Se usa el minero de x=27, como en IAEsqueletoTests.
/// </summary>
public class EsqueletoAnimadoTests
{
    const float XMinero = 27f;

    IAEsqueleto minero;
    Animator animator;
    PlayerController jugador;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Nivel1_Dolores", LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
        minero = Object.FindObjectsByType<IAEsqueleto>(FindObjectsSortMode.None)
            .OrderBy(m => Mathf.Abs(m.transform.position.x - XMinero)).First();
        animator = minero.GetComponentInChildren<Animator>();
        jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
    }

    [TearDown]
    public void Limpiar() { Time.timeScale = 1f; }

    bool EnEstado(string estado) => animator.GetCurrentAnimatorStateInfo(0).IsName(estado);

    [UnityTest]
    public IEnumerator Patrulla_UsaLaAnimacionDeCaminar()
    {
        Assert.IsNotNull(animator, "El minero tiene Animator en su visual");
        Assert.IsNotNull(animator.runtimeAnimatorController);

        bool camino = false;
        for (float t = 0f; t < 3f && !camino; t += Time.deltaTime)
        {
            camino = EnEstado("Caminar");
            yield return null;
        }
        Assert.IsTrue(camino, "Al patrullar se ve la animación de caminar");
        Assert.AreEqual(IAEsqueleto.Estado.Patrullar, minero.EstadoActual, "La IA sigue igual");
    }

    [UnityTest]
    public IEnumerator Ataque_LevantaElPico()
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 pos = new Vector2(minero.transform.position.x + 1f, jugador.transform.position.y + 0.1f);
        rb.position = pos;
        jugador.transform.position = pos;
        rb.linearVelocity = Vector2.zero;

        bool golpe = false;
        for (float t = 0f; t < 3f && !golpe; t += Time.deltaTime)
        {
            golpe = EnEstado("Golpe");
            yield return null;
        }
        Assert.IsTrue(golpe, "Al atacar a Mateo se ve el golpe con el pico");
    }

    [UnityTest]
    public IEnumerator Muerte_SeDesarmaYLuegoDesaparece()
    {
        var col = minero.GetComponent<Collider2D>();
        minero.GetComponent<Health>().RecibirDano(9999);
        yield return new WaitForSeconds(0.4f);

        Assert.IsTrue(minero != null, "No desaparece de golpe: primero se ve la muerte");
        Assert.IsTrue(EnEstado("Muerte"), "Se reproduce la animación de muerte");
        Assert.IsFalse(col.enabled, "Los huesos ya no estorban ni lastiman");

        yield return new WaitForSeconds(2.5f);
        Assert.IsTrue(minero == null, "Después de desvanecerse se destruye");
    }
}
