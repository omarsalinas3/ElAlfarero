using System.Collections;
using UnityEngine;

/// <summary>
/// Retroalimentación al recibir daño: el sprite se tiñe de rojo un instante.
/// Escucha Health.OnVidaCambiada; no modifica la lógica de combate.
/// </summary>
[RequireComponent(typeof(Health))]
public class ParpadeoDano : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Color colorGolpe = new Color(1f, 0.45f, 0.45f);
    [SerializeField] float duracion = 0.07f;

    Health salud;
    int vidaPrevia;

    void Awake()
    {
        salud = GetComponent<Health>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        salud.OnVidaCambiada += AlCambiarVida;
        vidaPrevia = salud.VidaActual;
    }

    void OnDisable() { salud.OnVidaCambiada -= AlCambiarVida; }

    void AlCambiarVida(int actual, int maxima)
    {
        bool fueDano = actual < vidaPrevia;
        vidaPrevia = actual;
        if (fueDano && sprite != null && isActiveAndEnabled) StartCoroutine(Parpadear());
    }

    IEnumerator Parpadear()
    {
        sprite.color = colorGolpe;
        yield return new WaitForSeconds(duracion);
        if (sprite != null) sprite.color = Color.white;
    }
}
