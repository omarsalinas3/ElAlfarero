using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// El Charro Negro, jefe final (#89): sus dos fases animadas (a caballo y a pie) y su fuego infernal como efecto
/// aparte. Mientras no exista el Nivel 4 se revisan en la galería apagada del Sandbox.
/// </summary>
public class CharroTests
{
    GameObject galeria;

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        yield return SceneManager.LoadSceneAsync("Sandbox", LoadSceneMode.Single);
        yield return null;
        galeria = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include).First(t => t.name == "Galeria_Charro").gameObject;
        Assert.IsFalse(galeria.activeSelf, "Apagada: no estorba a las demás pruebas del Sandbox");
        galeria.SetActive(true);
        yield return null;
    }

    static string[] Clips(Animator a) => a.runtimeAnimatorController.animationClips.Select(c => c.name).ToArray();

    [UnityTest]
    public IEnumerator Charro_ACaballoGalopaLanzaFuegoYElCaballoCae()
    {
        var caballo = galeria.transform.Find("Charro/Caballo");
        var anim = caballo.GetComponent<Animator>();
        var sr = caballo.GetComponent<SpriteRenderer>();
        foreach (var c in new[] { "Caballo_Reposo", "Caballo_Galope", "Caballo_Encabritar", "Caballo_Lanzar", "Caballo_Golpe", "Caballo_Caida" })
            CollectionAssert.Contains(Clips(anim), c);

        StringAssert.StartsWith("charro_caballo_reposo", sr.sprite.name, "Empieza en reposo");
        Assert.Greater(sr.bounds.size.y, 4.6f, "Montado es más grande que el Jinete (~4.5 u): es el jefe final");

        anim.SetFloat("Velocidad", 5f);
        yield return new WaitForSeconds(0.3f);
        StringAssert.StartsWith("charro_caballo_galope", sr.sprite.name, "Con velocidad galopa");

        anim.Play("Lanzar", 0, 0f);
        yield return new WaitForSeconds(0.15f);
        StringAssert.StartsWith("charro_caballo_lanzar", sr.sprite.name, "Lanza fuego desde el caballo");

        anim.SetBool("Caido", true);
        yield return new WaitForSeconds(1.5f);
        StringAssert.StartsWith("charro_caballo_caida", sr.sprite.name, "Al vencer al caballo, este cae");
    }

    [UnityTest]
    public IEnumerator Charro_APieTieneSusAtaquesYMuere()
    {
        var pie = galeria.transform.Find("Charro/Pie");
        Assert.IsFalse(pie.gameObject.activeSelf, "A pie aparece hasta que cae el caballo");
        pie.gameObject.SetActive(true);
        yield return null;

        var anim = pie.GetComponent<Animator>();
        var sr = pie.GetComponent<SpriteRenderer>();
        foreach (var c in new[] { "Pie_Reposo", "Pie_Caminar", "Pie_Espadazo", "Pie_Lanzar", "Pie_Furia", "Pie_Clavar", "Pie_Golpe", "Pie_Muerte" })
            CollectionAssert.Contains(Clips(anim), c);

        StringAssert.StartsWith("charro_pie_reposo", sr.sprite.name);
        Assert.That(sr.bounds.size.y, Is.InRange(3.5f, 4.6f), "A pie mide más que Mateo (~2.5 u)");
        float montado = galeria.transform.Find("Charro/Caballo").GetComponent<SpriteRenderer>().bounds.size.y;
        Assert.Less(sr.bounds.size.y, montado, "Sin el caballo se ve más chico que montado");

        foreach (var (estado, cuadro) in new[] { ("Espadazo", "charro_pie_espadazo"), ("Clavar", "charro_pie_clavar"), ("Furia", "charro_pie_furia") })
        {
            anim.Play(estado, 0, 0f);
            yield return new WaitForSeconds(0.15f);
            StringAssert.StartsWith(cuadro, sr.sprite.name, $"Ataque {estado}");
        }

        anim.SetFloat("Velocidad", 2f);
        anim.Play("Reposo", 0, 0f);
        yield return new WaitForSeconds(0.3f);
        StringAssert.StartsWith("charro_pie_caminar", sr.sprite.name, "Con velocidad camina");

        anim.SetBool("Muerto", true);
        yield return new WaitForSeconds(1.2f);
        StringAssert.StartsWith("charro_pie_muerte", sr.sprite.name, "Al morir arde y se hace humo");
    }

    [UnityTest]
    public IEnumerator Charro_FuegoInfernalComoEfectoAparte()
    {
        var fuego = galeria.transform.Find("Fuego").GetComponent<SpriteRenderer>();
        StringAssert.StartsWith("charro_fuego", fuego.sprite.name);
        string primero = fuego.sprite.name;
        bool flamea = false;
        for (float t = 0f; t < 0.5f && !flamea; t += Time.deltaTime)
        {
            flamea = fuego.sprite.name != primero;
            yield return null;
        }
        Assert.IsTrue(flamea, "La ráfaga está animada");

        var llamarada = galeria.transform.Find("Llamarada").GetComponent<SpriteRenderer>();
        llamarada.GetComponent<Animator>().Play("Brotar", 0, 0f);
        yield return null;
        StringAssert.StartsWith("charro_llamarada_00", llamarada.sprite.name, "Empieza con la grieta encendida que avisa");
        yield return new WaitForSeconds(0.3f);
        Assert.AreNotEqual("charro_llamarada_00", llamarada.sprite.name, "Luego brota el fuego");
        Assert.Greater(llamarada.bounds.size.y, 3.3f, "Más alta que el salto de Mateo: hay que esquivarla de lado");
        Assert.AreEqual(llamarada.transform.position.y, llamarada.bounds.min.y, 0.05f, "Brota del suelo");
    }
}
