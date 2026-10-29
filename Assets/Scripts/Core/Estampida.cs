using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Evento especial del Nivel 2 (#53): al entrar a la cañada la cámara se queda fija, sale el aviso "¡ESTAMPIDA!", la
/// tierra tiembla y los nahuales cruzan la pantalla a toda velocidad en manadas. Se esquivan saltando o evadiendo.
/// Antes de cada manada la cámara tiembla más fuerte y se oye el aullido: es el aviso para prepararse.
/// Cuando pasa la última, Mateo puede seguir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Estampida : MonoBehaviour
{
    [Serializable]
    public struct Carrera
    {
        public bool porLaDerecha;
        public float retraso;         // Segundos después del nahual anterior de la manada
        public float velocidad;
    }

    [Serializable]
    public class Manada { public Carrera[] nahuales; }

    [SerializeField] Nahual prefabNahual;
    [SerializeField] Manada[] manadas;
    [SerializeField] float ySuelo = -4.5f;
    [SerializeField] float avisoAntes = 1.1f;            // Temblor y aullido antes de que entre la manada
    [SerializeField] float pausaEntreManadas = 1.6f;

    public bool Activa { get; private set; }
    public bool Terminada { get; private set; }
    public int ManadaActual { get; private set; } = -1;
    public float Centro { get; private set; }

    void Awake() { GetComponent<Collider2D>().isTrigger = true; }

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
        gestor?.MostrarLetrero("", "¡ESTAMPIDA!", "SALTA O EVADE A LOS NAHUALES");
        GestorAudio.Reproducir(Sfx.AullidoNahual, 0f);
        camara?.Sacudir(1.5f, 0.08f);
        yield return new WaitForSeconds(1.5f);

        for (ManadaActual = 0; ManadaActual < manadas.Length; ManadaActual++)
        {
            gestor?.FijarObjetivo("ESQUIVA LA ESTAMPIDA", (float)ManadaActual / manadas.Length);

            // Aviso: retumba la cañada antes de que lleguen
            camara?.Sacudir(avisoAntes + 0.8f, 0.16f);
            GestorAudio.Reproducir(Sfx.Estampida, 0f);
            yield return new WaitForSeconds(avisoAntes);

            var carreras = manadas[ManadaActual].nahuales;
            for (int j = 0; j < carreras.Length; j++)
            {
                if (carreras[j].retraso > 0f) yield return new WaitForSeconds(carreras[j].retraso);
                Soltar(carreras[j], mitad);
            }
            yield return new WaitForSeconds(pausaEntreManadas);
        }

        gestor?.LiberarJugador();
        gestor?.VolverAlRecorrido();
        camara?.Desbloquear();
        gestor?.MostrarLetrero("", "¡ADELANTE!", "");
        Activa = false;
        Terminada = true;
    }

    void Soltar(Carrera c, float mitad)
    {
        if (prefabNahual == null) return;
        int lado = c.porLaDerecha ? 1 : -1;
        var pos = new Vector3(Centro + lado * (mitad + 2f), ySuelo, 0f);   // Entra desde fuera de la pantalla
        Nahual n = Reciclaje.Sacar(prefabNahual, pos, Quaternion.identity);
        n.Correr(-lado, c.velocidad);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.6f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, new Vector3(1f, 6f, 0f));
    }
}
