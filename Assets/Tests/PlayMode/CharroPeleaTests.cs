using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// La pelea final con El Charro Negro (#90): cinemática de entrada, fase 1 a caballo (embestidas y ráfagas), caída del
/// caballo y pacto, fase 2 a pie (espadazo, abanico y llamaradas) con furia a media vida, una barra por fase, victoria
/// y reintento desde la fase 2. Se prueba en la arena de pruebas (Arena_Charro) mientras no exista el Nivel 4; el
/// Nivel 4 usará el mismo CrearEncuentroCharro.
/// </summary>
public class CharroPeleaTests
{
    const string Arena = "Arena_Charro";

    [UnitySetUp]
    public IEnumerator Preparar()
    {
        GestorGuardado.UsarRuta(GuardadoPruebas.Ruta);
        GestorGuardado.Borrar();
        yield return null;
    }

    [TearDown]
    public void Limpiar()
    {
        Time.timeScale = 1f;
        JefeCharro.TomarPuntoDeControl();                 // Que ningún punto de control pase a otra prueba
        GestorGuardado.Borrar();
    }

    IEnumerator CargarArena()
    {
        yield return SceneManager.LoadSceneAsync(Arena, LoadSceneMode.Single);
        yield return new WaitForSeconds(0.5f);
    }

    static void Poner(PlayerController jugador, float x)
    {
        var rb = jugador.GetComponent<Rigidbody2D>();
        Vector2 p = new Vector2(x, rb.position.y + 0.1f);
        rb.position = p;
        jugador.transform.position = p;
    }

    /// <summary>Lleva a Mateo a la entrada de la arena y espera a que termine la cinemática.</summary>
    IEnumerator EmpezarPelea(EncuentroCharro encuentro, PlayerController jugador)
    {
        Poner(jugador, encuentro.transform.position.x);
        float limite = Time.time + 12f;
        while (!encuentro.PeleaEmpezada && Time.time < limite) yield return null;
        Assert.IsTrue(encuentro.PeleaEmpezada, "Llegar a la arena despierta al Charro");
    }

    /// <summary>Tumba al caballo, se salta el diálogo del pacto y espera a que el Charro pelee a pie.</summary>
    IEnumerator LlegarAFase2(JefeCharro jefe, EncuentroCharro encuentro)
    {
        float limite = Time.time + 5f;
        while (jefe.SaludCaballo.EsInvulnerable && Time.time < limite) yield return null;
        jefe.SaludCaballo.RecibirDano(100000);
        limite = Time.time + 6f;
        while (!(CuadroDialogo.Actual != null && CuadroDialogo.Actual.Activo) && !encuentro.PactoTerminado && Time.time < limite) yield return null;
        CuadroDialogo.Actual?.Saltar();
        limite = Time.time + 6f;
        while (!(encuentro.PactoTerminado && jefe.EstadoActual != JefeCharro.Estado.Pacto) && Time.time < limite) yield return null;
        Assert.AreEqual(2, jefe.Fase, "Sin caballo sigue a pie");
        Assert.AreNotEqual(JefeCharro.Estado.Pacto, jefe.EstadoActual, "Tras el pacto pelea a pie");
    }

    /// <summary>Espera a que empiece un ataque desde el principio y termine; deja cuánto lanzó (fuego, llamaradas).</summary>
    IEnumerator MedirAtaque(JefeCharro jefe, JefeCharro.Estado ataque, int[] r, float espera = 15f)
    {
        float limite = Time.time + espera;
        while (jefe.EstadoActual == ataque && Time.time < limite) yield return null;
        while (jefe.EstadoActual != ataque && Time.time < limite) yield return null;
        Assert.AreEqual(ataque, jefe.EstadoActual, $"Llega a usar el ataque {ataque}");
        int fuegos = jefe.FuegosLanzados, llamaradas = jefe.LlamaradasLanzadas, espadazos = jefe.Espadazos;
        while (jefe.EstadoActual == ataque && Time.time < limite) yield return null;
        r[0] = jefe.FuegosLanzados - fuegos;
        r[1] = jefe.LlamaradasLanzadas - llamaradas;
        r[2] = jefe.Espadazos - espadazos;
    }

    [UnityTest]
    [Timeout(90000)]
    public IEnumerator Charro_CinematicaYFase1ACaballoConEmbestidasYRafagas()
    {
        yield return CargarArena();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        var encuentro = Object.FindFirstObjectByType<EncuentroCharro>();
        Assert.IsNull(Object.FindFirstObjectByType<JefeCharro>(), "Nadie lo ve hasta la cinemática");
        yield return EmpezarPelea(encuentro, jugador);

        var jefe = Object.FindFirstObjectByType<JefeCharro>();
        Assert.AreEqual(1, jefe.Fase, "Empieza a caballo");
        Assert.IsTrue(Object.FindObjectsByType<BarraJefe>().Any(), "Sale su barra de vida");
        Assert.Greater(jefe.SaludCaballo.VidaMaxima + jefe.SaludPie.VidaMaxima, 1200, "Aguanta más que La Llorona");

        int vida = salud.VidaActual;
        float limite = Time.time + 20f;
        while ((jefe.Embestidas == 0 || jefe.FuegosLanzados == 0) && Time.time < limite) yield return null;
        Assert.Greater(jefe.Embestidas, 0, "Embiste a caballo de lado a lado");
        Assert.Greater(jefe.FuegosLanzados, 0, "Lanza ráfagas de fuego infernal");
        yield return new WaitForSeconds(1.5f);
        Assert.Less(salud.VidaActual, vida, "Sus ataques lastiman a Mateo si no los esquiva");
    }

    [UnityTest]
    [Timeout(90000)]
    public IEnumerator Charro_AlCaerElCaballoOfreceElPactoYSigueAPieConOtraBarra()
    {
        yield return CargarArena();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        var encuentro = Object.FindFirstObjectByType<EncuentroCharro>();
        yield return EmpezarPelea(encuentro, jugador);
        var jefe = Object.FindFirstObjectByType<JefeCharro>();

        float limite = Time.time + 5f;
        while (jefe.SaludCaballo.EsInvulnerable && Time.time < limite) yield return null;
        jefe.SaludCaballo.RecibirDano(100000);
        limite = Time.time + 4f;
        while (jefe.EstadoActual != JefeCharro.Estado.Pacto && Time.time < limite) yield return null;
        Assert.AreEqual(JefeCharro.Estado.Pacto, jefe.EstadoActual, "El caballo cae y el Charro se levanta");
        Assert.IsFalse(jefe.SaludCaballo.gameObject.activeSelf, "El caballo se hizo humo");
        Assert.IsTrue(jefe.SaludPie.gameObject.activeSelf, "Queda el Charro a pie");
        Assert.IsTrue(jefe.SaludPie.EsInvulnerable, "Mientras habla no se le puede pegar");

        limite = Time.time + 3f;
        while (!(CuadroDialogo.Actual != null && CuadroDialogo.Actual.Activo) && Time.time < limite) yield return null;
        Assert.IsTrue(CuadroDialogo.Actual.Activo, "Ofrece el pacto");
        Assert.IsTrue(encuentro.LineasPacto.Any(l => l.texto.Contains("oro") && l.texto.Contains("Carmen")), "Le ofrece oro a cambio de Carmen");
        Assert.IsTrue(encuentro.LineasPacto.Any(l => l.personaje == "MATEO"), "y Mateo le contesta");
        Assert.AreEqual(GestorNivel.Estado.Cinematica, GestorNivel.Actual.EstadoActual, "Mateo escucha sin moverse");
        CuadroDialogo.Actual.Saltar();

        limite = Time.time + 6f;
        while (jefe.EstadoActual == JefeCharro.Estado.Pacto && Time.time < limite) yield return null;
        Assert.AreEqual(GestorNivel.Estado.Jugando, GestorNivel.Actual.EstadoActual, "Sigue la pelea");
        Assert.IsFalse(jefe.SaludPie.EsInvulnerable);
        yield return new WaitForSeconds(0.5f);
        Assert.IsNotNull(Object.FindFirstObjectByType<BarraJefe>(), "Sale la barra de la fase 2");
        Assert.AreEqual(jefe.SaludPie.VidaMaxima, jefe.SaludPie.VidaActual, "con la vida completa del Charro a pie");
    }

    [UnityTest]
    [Timeout(150000)]
    public IEnumerator Charro_APieUsaTresAtaquesYFuriosoLanzaMasFuego()
    {
        yield return CargarArena();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        var encuentro = Object.FindFirstObjectByType<EncuentroCharro>();
        yield return EmpezarPelea(encuentro, jugador);
        var jefe = Object.FindFirstObjectByType<JefeCharro>();
        yield return LlegarAFase2(jefe, encuentro);

        // Lejos: abanico de tres ráfagas y llamaradas bajo los pies de Mateo
        float lejos = encuentro.transform.position.x;
        var r = new int[3];
        Poner(jugador, lejos);
        yield return MedirAtaque(jefe, JefeCharro.Estado.Abanico, r);
        Assert.AreEqual(3, r[0], "Abanico de tres ráfagas");
        Poner(jugador, lejos);
        yield return MedirAtaque(jefe, JefeCharro.Estado.Llamaradas, r);
        Assert.AreEqual(3, r[1], "Tres llamaradas, una tras otra, donde está Mateo");

        // Cerca: espadazo
        Poner(jugador, jefe.transform.position.x - 2f);
        yield return MedirAtaque(jefe, JefeCharro.Estado.Espadazo, r);
        Assert.AreEqual(1, r[2], "Pegado a él da un espadazo");

        // A media vida se enfurece: el abanico es de cinco
        float limite = Time.time + 5f;
        while (jefe.SaludPie.EsInvulnerable && Time.time < limite) yield return null;
        jefe.SaludPie.RecibirDano(jefe.SaludPie.VidaMaxima / 2 + 1);
        limite = Time.time + 3f;
        while (!jefe.Furioso && Time.time < limite) yield return null;
        Assert.IsTrue(jefe.Furioso, "A media vida se enfurece");
        Poner(jugador, lejos);
        yield return MedirAtaque(jefe, JefeCharro.Estado.Abanico, r, 20f);
        Assert.AreEqual(5, r[0], "Furioso lanza cinco ráfagas");
    }

    [UnityTest]
    [Timeout(90000)]
    public IEnumerator Charro_VencerloCompletaElNivel()
    {
        yield return CargarArena();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        jugador.GetComponent<Health>().HacerInvulnerable(999f);
        var encuentro = Object.FindFirstObjectByType<EncuentroCharro>();
        yield return EmpezarPelea(encuentro, jugador);
        var jefe = Object.FindFirstObjectByType<JefeCharro>();
        yield return LlegarAFase2(jefe, encuentro);

        float limite = Time.time + 5f;
        while (jefe.SaludPie.EsInvulnerable && Time.time < limite) yield return null;
        jefe.SaludPie.RecibirDano(100000);
        Assert.AreEqual(JefeCharro.Estado.Muerto, jefe.EstadoActual);
        limite = Time.time + 6f;
        while (GestorNivel.Actual.EstadoActual != GestorNivel.Estado.Victoria && Time.time < limite) yield return null;
        Assert.AreEqual(GestorNivel.Estado.Victoria, GestorNivel.Actual.EstadoActual, "Vencerlo completa el nivel");
        Assert.IsFalse(JefeCharro.HayPuntoDeControl, "Vencido, no queda punto de control");
    }

    [UnityTest]
    [Timeout(120000)]
    public IEnumerator Charro_SiMateoCaeEnLaFase2ElReintentoEmpiezaAhi()
    {
        yield return CargarArena();
        var jugador = Object.FindFirstObjectByType<PlayerController>();
        var salud = jugador.GetComponent<Health>();
        salud.Invulnerable = true;                        // Manual, para poder quitarla antes del golpe final
        var encuentro = Object.FindFirstObjectByType<EncuentroCharro>();
        yield return EmpezarPelea(encuentro, jugador);
        var jefe = Object.FindFirstObjectByType<JefeCharro>();
        Assert.IsFalse(JefeCharro.HayPuntoDeControl, "Pelear a caballo no deja punto de control");
        yield return LlegarAFase2(jefe, encuentro);

        // Mateo cae peleando a pie
        salud.Invulnerable = false;
        float espera = Time.time + 3f;
        while (salud.EsInvulnerable && Time.time < espera) yield return null;   // Por si un golpe le dejó unos segundos
        salud.RecibirDano(100000);
        Assert.IsTrue(salud.EstaMuerto);
        Assert.IsTrue(JefeCharro.HayPuntoDeControl, "Caer en la fase 2 deja el punto de control");

        float limite = Time.time + 6f;
        while (GestorNivel.Actual.EstadoActual != GestorNivel.Estado.GameOver && Time.time < limite) yield return null;
        GestorNivel.Actual.Reintentar();
        limite = Time.realtimeSinceStartup + 10f;
        EncuentroCharro nuevo = null;
        while (Time.realtimeSinceStartup < limite)
        {
            nuevo = Object.FindFirstObjectByType<EncuentroCharro>();
            if (nuevo != null && nuevo != encuentro && nuevo.PeleaEmpezada) break;
            yield return null;
        }
        Assert.IsNotNull(nuevo);
        Assert.IsTrue(nuevo.Retomado, "La escena empezó desde el punto de control");
        Assert.IsTrue(nuevo.PeleaEmpezada, "La pelea arranca sola");
        var jefeNuevo = Object.FindFirstObjectByType<JefeCharro>();
        Assert.AreEqual(2, jefeNuevo.Fase, "Directo contra el Charro a pie");
        Assert.IsFalse(jefeNuevo.SaludCaballo.gameObject.activeSelf, "Sin caballo");
        var mateo = Object.FindFirstObjectByType<PlayerController>();
        Assert.Greater(mateo.transform.position.x, nuevo.transform.position.x - 1f, "Mateo aparece en la arena");
        Assert.IsFalse(JefeCharro.HayPuntoDeControl, "El punto de control se usa una sola vez");
    }
}
