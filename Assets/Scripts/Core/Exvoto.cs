using UnityEngine;

/// <summary>
/// Exvoto de barro escondido (#63): hay 3 por nivel (id 0..2). Al tocarlo se guarda en la partida (GestorGuardado
/// recuerda cuáles se encontraron en cada nivel) y la victoria muestra SECRETOS n/3. Los que ya se encontraron en otra
/// partida se ven translúcidos y no vuelven a sumar puntos.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Exvoto : MonoBehaviour
{
    [SerializeField] int id;
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] SpriteAnimadoTemporal efectoRecoger;
    [SerializeField] float amplitudFlote = 0.1f;

    Vector3 origen;
    public int Id => id;

    void Start()
    {
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();
        origen = sprite.transform.localPosition;
        var gestor = GestorNivel.Actual;
        if (gestor != null && GestorGuardado.TieneExvoto(gestor.NumeroNivel, id))
            sprite.color = new Color(1f, 1f, 1f, 0.45f);
    }

    void Update()
    {
        // Flota y brilla un poco para que se distinga del escenario
        sprite.transform.localPosition = origen + Vector3.up * Mathf.Sin(Time.time * 2.5f + id) * amplitudFlote;
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.GetComponentInParent<PlayerController>() == null) return;
        GestorNivel.Actual?.EncontrarExvoto(id);
        GestorAudio.Reproducir(Sfx.Exvoto, 0f);
        if (efectoRecoger != null) Reciclaje.Sacar(efectoRecoger, sprite.transform.position, Quaternion.identity);
        Destroy(gameObject);                                   // Uno por escena: no vale la pena reciclarlo
    }
}
