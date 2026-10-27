using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Evento especial del GDD: al pisar la zona, la cámara se queda fija, Mateo no puede salirse y los bandidos
/// del Charro Negro entran corriendo por ambos lados en oleadas. Cuando cae el último, todo se libera.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Emboscada : MonoBehaviour
{
    [Serializable]
    public struct Entrada
    {
        public IABandido prefab;
        public bool porLaDerecha;
        public float retraso;         // Segundos después de la entrada anterior
    }

    [Serializable]
    public class Oleada { public Entrada[] bandidos; }

    [SerializeField] Oleada[] oleadas;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] float pausaEntreOleadas = 1f;

    public bool Activa { get; private set; }
    public bool Terminada { get; private set; }
    public int OleadaActual { get; private set; } = -1;
    public float Centro { get; private set; }

    readonly List<IABandido> vivos = new List<IABandido>();
    IABandido[][] listos;            // Creados al cargar el nivel y apagados: entrar no cuesta un tirón

    void Awake() { GetComponent<Collider2D>().isTrigger = true; }

    void Start()
    {
        listos = new IABandido[oleadas.Length][];
        for (int i = 0; i < oleadas.Length; i++)
        {
            listos[i] = new IABandido[oleadas[i].bandidos.Length];
            for (int j = 0; j < listos[i].Length; j++)
            {
                var prefab = oleadas[i].bandidos[j].prefab;
                if (prefab == null) continue;
                listos[i][j] = Instantiate(prefab, transform.position, Quaternion.identity, transform);
                listos[i][j].gameObject.SetActive(false);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Activa || Terminada || otro.GetComponentInParent<PlayerController>() == null) return;
        StartCoroutine(Secuencia());
    }

    IEnumerator Secuencia()
    {
        Activa = true;
        var gestor = GestorNivel.Actual;
        var camara = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;

        float mitad = camara != null ? camara.MitadAncho : 10f;
        Centro = camara != null ? camara.Bloquear(transform.position.x) : transform.position.x;
        gestor?.LimitarJugador(Centro - mitad + 0.6f, Centro + mitad - 0.6f);
        gestor?.MostrarLetrero("", "¡EMBOSCADA!", "LOS BANDIDOS DEL CHARRO");

        for (OleadaActual = 0; OleadaActual < oleadas.Length; OleadaActual++)
        {
            gestor?.FijarObjetivo($"SOBREVIVE A LA EMBOSCADA  {OleadaActual + 1}/{oleadas.Length}");
            var entradas = oleadas[OleadaActual].bandidos;
            for (int j = 0; j < entradas.Length; j++)
            {
                if (entradas[j].retraso > 0f) yield return new WaitForSeconds(entradas[j].retraso);
                Aparecer(listos[OleadaActual][j], entradas[j], mitad);
            }
            yield return new WaitUntil(() => vivos.TrueForAll(b => b == null || b.EstadoActual == IABandido.Estado.Muerto));
            vivos.Clear();
            yield return new WaitForSeconds(pausaEntreOleadas);
        }

        gestor?.LiberarJugador();
        gestor?.VolverAlRecorrido();
        camara?.Desbloquear();
        gestor?.MostrarLetrero("", "¡ADELANTE!", "");
        Activa = false;
        Terminada = true;
    }

    void Aparecer(IABandido b, Entrada e, float mitad)
    {
        if (b == null) return;
        int lado = e.porLaDerecha ? 1 : -1;
        b.transform.position = new Vector3(Centro + lado * (mitad + 1.5f), ySuelo, 0f);   // Fuera de la pantalla
        Vector3 s = b.transform.localScale;
        b.transform.localScale = new Vector3(Mathf.Abs(s.x) * -lado, s.y, s.z);   // Mira hacia el centro
        b.SiempreAlerta = true;
        b.gameObject.SetActive(true);
        vivos.Add(b);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.3f, 0.2f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, new Vector3(1f, 6f, 0f));
    }
}
