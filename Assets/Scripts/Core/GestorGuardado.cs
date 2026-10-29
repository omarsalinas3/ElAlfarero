using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Lo que se guarda de la partida. JsonUtility solo serializa campos públicos, por eso no son propiedades.</summary>
[Serializable]
public class DatosGuardado
{
    public int version = 1;
    public int nivelDesbloqueado = 1;
    public int monedas;                                           // Acumuladas de todos los niveles ganados
    public List<RecordNivel> records = new List<RecordNivel>();
    public List<string> armasDesbloqueadas = new List<string> { "Talavera" };

    public RecordNivel Record(int nivel) => records.Find(r => r.nivel == nivel);
}

[Serializable]
public class RecordNivel
{
    public int nivel;
    public int puntajeMaximo;
    public float mejorTiempo;                                     // En segundos; menos es mejor
}

/// <summary>Lo que pasó con los récords al ganar un nivel (lo muestra la pantalla de victoria).</summary>
public struct ResultadoVictoria
{
    public int puntajeMaximo;
    public float mejorTiempo;
    public bool nuevoRecordPuntaje;
    public bool nuevoRecordTiempo;
    public bool NuevoRecord => nuevoRecordPuntaje || nuevoRecordTiempo;
}

/// <summary>
/// Partida guardada en JSON en Application.persistentDataPath. Se carga sola al iniciar el juego y se guarda al ganar un nivel.
/// Si el archivo está dañado se empieza una partida nueva (el archivo dañado se aparta como .corrupto para no perderlo).
/// </summary>
public static class GestorGuardado
{
    const string NombreArchivo = "partida.json";

    static DatosGuardado datos;
    static string rutaPersonalizada;

    public static string Ruta => rutaPersonalizada ?? Path.Combine(Application.persistentDataPath, NombreArchivo);

    public static DatosGuardado Datos
    {
        get
        {
            if (datos == null) Cargar();
            return datos;
        }
    }

    /// <summary>Hay algo que continuar: ya se ganó al menos un nivel.</summary>
    public static bool HayPartida => File.Exists(Ruta) && Datos.records.Count > 0;

    // Domain reload desactivado: lo estático sobrevive entre un Play y otro
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ReiniciarEstaticos()
    {
        datos = null;
        rutaPersonalizada = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CargarAlIniciar() => Cargar();

    /// <summary>Para pruebas: guarda en otro archivo y no toca la partida real.</summary>
    public static void UsarRuta(string ruta)
    {
        rutaPersonalizada = ruta;
        datos = null;
    }

    public static void Cargar()
    {
        datos = null;
        if (File.Exists(Ruta))
        {
            try
            {
                datos = JsonUtility.FromJson<DatosGuardado>(File.ReadAllText(Ruta));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Partida dañada, se empieza una nueva: {e.Message}");
                try { File.Copy(Ruta, Ruta + ".corrupto", true); } catch (IOException) { }
            }
        }
        datos ??= new DatosGuardado();
        datos.records ??= new List<RecordNivel>();
        datos.armasDesbloqueadas ??= new List<string>();
        if (!datos.armasDesbloqueadas.Contains("Talavera")) datos.armasDesbloqueadas.Insert(0, "Talavera");
        datos.nivelDesbloqueado = Mathf.Max(1, datos.nivelDesbloqueado);
    }

    public static void Guardar()
    {
        // Primero a un temporal y luego se reemplaza: si el juego se cierra a medio guardar, la partida anterior sigue entera
        string temporal = Ruta + ".tmp";
        Directory.CreateDirectory(Path.GetDirectoryName(Ruta));
        File.WriteAllText(temporal, JsonUtility.ToJson(Datos, true));
        if (File.Exists(Ruta)) File.Replace(temporal, Ruta, null);
        else File.Move(temporal, Ruta);
    }

    /// <summary>Registra un nivel ganado: récords, siguiente nivel desbloqueado y monedas. Guarda en disco.</summary>
    public static ResultadoVictoria RegistrarVictoria(int nivel, int puntaje, float tiempo, int monedas)
    {
        var d = Datos;
        var record = d.Record(nivel);
        bool primera = record == null;
        if (primera)
        {
            record = new RecordNivel { nivel = nivel };
            d.records.Add(record);
        }

        // La primera victoria fija la marca pero no se festeja como récord: no había nada que superar
        var resultado = new ResultadoVictoria
        {
            nuevoRecordPuntaje = !primera && puntaje > record.puntajeMaximo,
            nuevoRecordTiempo = !primera && tiempo < record.mejorTiempo,
        };
        record.puntajeMaximo = Mathf.Max(record.puntajeMaximo, puntaje);
        record.mejorTiempo = primera ? tiempo : Mathf.Min(record.mejorTiempo, tiempo);
        resultado.puntajeMaximo = record.puntajeMaximo;
        resultado.mejorTiempo = record.mejorTiempo;

        d.nivelDesbloqueado = Mathf.Max(d.nivelDesbloqueado, nivel + 1);
        d.monedas += Mathf.Max(0, monedas);
        Guardar();
        return resultado;
    }

    public static void DesbloquearArma(string arma)
    {
        if (Datos.armasDesbloqueadas.Contains(arma)) return;
        Datos.armasDesbloqueadas.Add(arma);
        Guardar();
    }

    /// <summary>Borra la partida del disco y vuelve a empezar desde el Nivel 1.</summary>
    public static void Borrar()
    {
        if (File.Exists(Ruta)) File.Delete(Ruta);
        datos = new DatosGuardado();
    }
}
