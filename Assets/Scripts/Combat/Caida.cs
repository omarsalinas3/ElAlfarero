using System.Collections;
using UnityEngine;

/// <summary>
/// Caída desde lo alto con gravedad que lastima a Mateo si le cae encima. La comparten la maceta que cae (Nivel 1)
/// y las rocas del derrumbe (Nivel 4, #88). Termina al llegar al suelo o al golpear a Mateo; Golpeo dice cuál fue.
/// </summary>
public class Caida
{
    public bool Golpeo { get; private set; }

    /// <summary>Hace caer "cuerpo" hasta ySuelo (medido desde la orilla de abajo de su collider).</summary>
    public IEnumerator Caer(Transform cuerpo, Collider2D col, DanoPorContacto dano, LayerMask capaJugador, float gravedad, float ySuelo)
    {
        Golpeo = false;
        float velocidad = 0f;
        float mitadAlto = col != null ? col.bounds.extents.y : 0.3f;

        while (cuerpo.position.y - mitadAlto > ySuelo)
        {
            velocidad += gravedad * Time.deltaTime;
            cuerpo.position += Vector3.down * velocidad * Time.deltaTime;

            // Golpe directo al jugador
            if (col != null && dano != null)
            {
                var golpeado = Physics2D.OverlapBox(col.bounds.center, col.bounds.size, 0f, capaJugador);
                if (golpeado != null && dano.Aplicar(golpeado)) { Golpeo = true; yield break; }
            }
            yield return null;
        }
        cuerpo.position = new Vector3(cuerpo.position.x, ySuelo + mitadAlto, cuerpo.position.z);
    }
}
