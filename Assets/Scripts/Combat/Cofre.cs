using UnityEngine;

/// <summary>
/// Cofre del cuarto secreto (#63): al tocarlo se abre (cambia al sprite con monedas) y suelta su botín en abanico.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Cofre : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Sprite abierto;
    [SerializeField] Recogible[] botin;
    [SerializeField] int puntos = 300;

    public bool Abierto { get; private set; }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (Abierto || otro.GetComponentInParent<PlayerController>() == null) return;
        Abierto = true;
        sprite.sprite = abierto;
        GestorAudio.Reproducir(Sfx.AbrirCofre, 0f);
        GestorNivel.Actual?.SumarPuntos(puntos);
        for (int i = 0; i < botin.Length; i++)
        {
            if (botin[i] == null) continue;
            float desplazamiento = (i - (botin.Length - 1) * 0.5f) * 0.6f;
            Reciclaje.Sacar(botin[i], transform.position + new Vector3(desplazamiento, 1.6f + Mathf.Abs(desplazamiento) * -0.3f, 0f), Quaternion.identity);
        }
    }
}
