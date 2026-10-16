using UnityEngine;

/// <summary>
/// Objetos que se rompen a cerámicazos: huacales y hornos de barro.
/// Cambia de sprite según el daño, al romperse deja la animación de restos, suelta botín y suena.
/// Necesita Health con "destruir al morir" activado.
/// </summary>
[RequireComponent(typeof(Health))]
public class Destructible : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [Tooltip("Sprites de intacto a casi roto; se elige según la vida restante.")]
    [SerializeField] Sprite[] estados;
    [SerializeField] SpriteAnimadoTemporal prefabRestos;
    [SerializeField] Sfx sonidoRomper = Sfx.RomperHuacal;

    [Header("Botín")]
    [SerializeField] Recogible[] botin;
    [SerializeField, Range(0f, 1f)] float probabilidadBotin = 1f;
    [SerializeField] GameObject dejarAlRomper;     // Ej. brasas que quedan del horno

    Health salud;

    void Awake()
    {
        salud = GetComponent<Health>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable() { salud.OnVidaCambiada += AlCambiarVida; salud.OnMuerte += AlRomperse; }
    void OnDisable() { salud.OnVidaCambiada -= AlCambiarVida; salud.OnMuerte -= AlRomperse; }

    void AlCambiarVida(int actual, int maxima)
    {
        if (actual <= 0 || estados == null || estados.Length == 0) return;
        float dano = 1f - (float)actual / maxima;
        sprite.sprite = estados[Mathf.Min(estados.Length - 1, Mathf.FloorToInt(dano * estados.Length))];
        GestorAudio.Reproducir(Sfx.Golpe);
    }

    void AlRomperse()
    {
        GestorAudio.Reproducir(sonidoRomper);
        Vector3 pos = transform.position;

        if (prefabRestos != null)
        {
            var restos = Instantiate(prefabRestos, pos, Quaternion.identity);
            restos.transform.localScale = transform.localScale;
        }
        if (dejarAlRomper != null) Instantiate(dejarAlRomper, pos, Quaternion.identity);

        if (botin != null && botin.Length > 0 && Random.value <= probabilidadBotin)
        {
            for (int i = 0; i < botin.Length; i++)
            {
                if (botin[i] == null) continue;
                float desplazamiento = (i - (botin.Length - 1) * 0.5f) * 0.7f;
                Instantiate(botin[i], pos + new Vector3(desplazamiento, 0.6f, 0f), Quaternion.identity);
            }
        }
    }
}
