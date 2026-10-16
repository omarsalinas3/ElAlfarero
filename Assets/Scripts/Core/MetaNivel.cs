using UnityEngine;

/// <summary>Trigger al final del nivel (frente a la parroquia): al entrar Mateo, el nivel se completa.</summary>
[RequireComponent(typeof(Collider2D))]
public class MetaNivel : MonoBehaviour
{
    void Awake() { GetComponent<Collider2D>().isTrigger = true; }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponentInParent<PlayerController>() != null) GestorNivel.Actual?.Completar();
    }
}
