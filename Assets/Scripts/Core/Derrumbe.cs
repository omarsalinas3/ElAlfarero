using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Evento especial del Nivel 4 (#88): al entrar a la galería de vigas, la cámara se queda fija, sale "¡DERRUMBE!" y
/// el techo se viene abajo en tandas. Igual que la estampida (#53): cámara fija, aviso con temblor y retumbo antes de
/// cada tanda, y Mateo no puede salir de la pantalla hasta que pasa. Cada roca marca su sombra en el suelo antes de
/// caer (RocaQueCae), así que se esquiva moviéndose o evadiendo. Cuando cae la última, el camino queda libre.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Derrumbe : MonoBehaviour
{
    [Serializable]
    public struct Roca
    {
        public float x;               // Respecto al centro de la pantalla (-10 a 10)
        public float retraso;         // Segundos después de la roca anterior de la tanda
        public bool sobreMateo;       // Cae donde está Mateo en ese momento (x se suma como desvío)
    }

    [Serializable]
    public class Tanda { public Roca[] rocas; }

    [SerializeField] RocaQueCae prefabRoca;
    [SerializeField] Tanda[] tandas;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] float altoTecho = 6.5f;           // Desde dónde caen (arriba del borde de la pantalla)
    [SerializeField] float avisoTanda = 1.1f;          // Temblor y retumbo antes de cada tanda
    [SerializeField] float avisoRoca = 0.8f;           // Sombra en el suelo antes de que caiga cada roca
    [SerializeField] float pausaEntreTandas = 1.4f;

    public bool Activo { get; private set; }
    public bool Terminado { get; private set; }
    public int TandaActual { get; private set; } = -1;
    public int RocasSoltadas { get; private set; }
    public float Centro { get; private set; }
    public int TotalRocas
    {
        get { int n = 0; foreach (var t in tandas) n += t.rocas.Length; return n; }
    }

    Transform jugador;

    void Awake() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D otro)
    {
        var pc = otro.GetComponentInParent<PlayerController>();
        if (Activo || Terminado || pc == null) return;
        jugador = pc.transform;
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        Activo = true;
        var gestor = GestorNivel.Actual;
        var camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        float mitad = camara != null ? camara.MitadAncho : 10f;
        Centro = camara != null ? camara.Bloquear(transform.position.x) : transform.position.x;
        gestor?.LimitarJugador(Centro - mitad + 0.6f, Centro + mitad - 0.6f);
        gestor?.MostrarLetrero("", "¡DERRUMBE!", "FIJATE EN LAS SOMBRAS DEL SUELO");
        GestorAudio.Reproducir(Sfx.Derrumbe, 0f);
        camara?.Sacudir(1.5f, 0.1f);
        yield return new WaitForSeconds(1.5f);

        for (TandaActual = 0; TandaActual < tandas.Length; TandaActual++)
        {
            gestor?.FijarObjetivo("SOBREVIVE AL DERRUMBE", (float)TandaActual / tandas.Length);

            // Aviso: el túnel retumba antes de cada tanda
            camara?.Sacudir(avisoTanda + 0.6f, 0.14f);
            GestorAudio.Reproducir(Sfx.Derrumbe, 0f);
            yield return new WaitForSeconds(avisoTanda);

            foreach (var c in tandas[TandaActual].rocas)
            {
                if (c.retraso > 0f) yield return new WaitForSeconds(c.retraso);
                Soltar(c, mitad);
            }
            yield return new WaitForSeconds(pausaEntreTandas);
        }

        // Que termine de caer lo último antes de soltar a Mateo
        yield return new WaitForSeconds(avisoRoca + 0.6f);
        gestor?.LiberarJugador();
        gestor?.VolverAlRecorrido();
        camara?.Desbloquear();
        gestor?.MostrarLetrero("", "¡SALISTE VIVO!", "");
        Activo = false;
        Terminado = true;
    }

    void Soltar(Roca c, float mitad)
    {
        if (prefabRoca == null) return;
        float x = c.sobreMateo && jugador != null ? jugador.position.x + c.x : Centro + c.x;
        x = Mathf.Clamp(x, Centro - mitad + 0.8f, Centro + mitad - 0.8f);
        RocaQueCae roca = Reciclaje.Sacar(prefabRoca, new Vector3(x, ySuelo, 0f), Quaternion.identity);
        roca.Soltar(x, ySuelo + altoTecho + 5f, ySuelo, avisoRoca);
        RocasSoltadas++;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.5f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, new Vector3(1f, 6f, 0f));
    }
}
