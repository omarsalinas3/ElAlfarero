using UnityEngine;

/// <summary>
/// Puente entre el estado del jugador y el Animator (Idle / Run / Jump / Evadir).
/// Va en el hijo "Visual", que es quien tiene el SpriteRenderer y el Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    static readonly int Velocidad = Animator.StringToHash("Velocidad");
    static readonly int EnSuelo = Animator.StringToHash("EnSuelo");
    static readonly int Evadiendo = Animator.StringToHash("Evadiendo");

    Animator animator;
    PlayerController jugador;
    Rigidbody2D rb;

    void Awake()
    {
        animator = GetComponent<Animator>();
        jugador = GetComponentInParent<PlayerController>();
        rb = GetComponentInParent<Rigidbody2D>();
    }

    void Update()
    {
        if (jugador == null) return;

        animator.SetFloat(Velocidad, Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(EnSuelo, jugador.EstaEnSuelo);
        animator.SetBool(Evadiendo, jugador.EstaEvadiendo);
    }
}
