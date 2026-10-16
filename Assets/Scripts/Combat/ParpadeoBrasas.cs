using UnityEngine;

/// <summary>Brillo vivo para brasas y fuego: varía color y escala con ruido para que parezca que arde.</summary>
public class ParpadeoBrasas : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Color colorBajo = new Color(0.85f, 0.6f, 0.5f);
    [SerializeField] Color colorAlto = new Color(1.0f, 0.95f, 0.8f);
    [SerializeField] float velocidad = 6f;

    Vector3 escalaBase;
    float semilla;

    void Awake()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        escalaBase = sprite.transform.localScale;
        semilla = Random.value * 100f;
    }

    void Update()
    {
        float n = Mathf.PerlinNoise(semilla, Time.time * velocidad);
        sprite.color = Color.Lerp(colorBajo, colorAlto, n);
        sprite.transform.localScale = escalaBase * (0.97f + n * 0.06f);
    }
}
