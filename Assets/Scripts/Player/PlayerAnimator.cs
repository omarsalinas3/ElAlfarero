using UnityEngine;

/// <summary>
/// Puente entre el estado del jugador y el Animator (Idle / Run / Jump / Evadir / Muerte).
/// Va en el hijo "Visual", que es quien tiene el SpriteRenderer y el Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    static readonly int Velocidad = Animator.StringToHash("Velocidad");
    static readonly int EnSuelo = Animator.StringToHash("EnSuelo");
    static readonly int Evadiendo = Animator.StringToHash("Evadiendo");
    static readonly int Muerto = Animator.StringToHash("Muerto");

    Animator animator;
    PlayerController jugador;
    Rigidbody2D rb;
    Health salud;

    /// <summary>Montado en el alebrije: Mateo va sentado (pose de reposo) aunque se mueva o brinque.</summary>
    public bool Montado { get; set; }

    void Awake()
    {
        animator = GetComponent<Animator>();
        jugador = GetComponentInParent<PlayerController>();
        rb = GetComponentInParent<Rigidbody2D>();
        salud = GetComponentInParent<Health>();
    }

    void Update()
    {
        if (jugador == null) return;

        animator.SetFloat(Velocidad, Montado ? 0f : Mathf.Abs(rb.linearVelocity.x));
        animator.SetBool(EnSuelo, Montado || jugador.EstaEnSuelo);
        animator.SetBool(Evadiendo, jugador.EstaEvadiendo);
        animator.SetBool(Muerto, salud != null && salud.EstaMuerto);
    }
}
