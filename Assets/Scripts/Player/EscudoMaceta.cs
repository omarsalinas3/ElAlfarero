using UnityEngine;

/// <summary>
/// Maceta Escudo (#33): con ella equipada Mateo carga una maceta grande al frente que detiene las balas, bolas de fuego
/// y calaveras que le llegan de frente (BalaEnemiga pregunta aquí antes de dañarlo). Aguanta 5 golpes: se va cuarteando
/// y al quinto se rompe en pedazos y Mateo vuelve a la Talavera. Mientras la carga no lanza cerámica.
/// Va en un hijo del jugador, así se voltea con él.
/// </summary>
public class EscudoMaceta : MonoBehaviour
{
    [SerializeField] ArsenalJugador arsenal;
    [SerializeField] PlayerController jugador;
    [SerializeField] SpriteRenderer sprite;
    [Tooltip("De intacta a casi rota; se elige según los golpes que le quedan.")]
    [SerializeField] Sprite[] estados;
    [SerializeField] SpriteAnimadoTemporal prefabRestos;

    PlayerShooter tirador;
    float temblor;

    void Awake()
    {
        if (jugador == null) jugador = GetComponentInParent<PlayerController>();
        if (arsenal == null) arsenal = GetComponentInParent<ArsenalJugador>();
        tirador = GetComponentInParent<PlayerShooter>();
    }

    /// <summary>True si Mateo trae la maceta en las manos (montado en el alebrije no la lleva).</summary>
    public bool Activo => arsenal != null && arsenal.Actual.EsEscudo && (tirador == null || tirador.enabled);

    void Update()
    {
        bool activo = Activo;
        if (sprite.enabled != activo) sprite.enabled = activo;
        if (!activo) return;

        int quedan = arsenal.MunicionActual;
        int maximo = arsenal.Actual.municionMaxima;
        float dano = 1f - (float)quedan / maximo;
        sprite.sprite = estados[Mathf.Clamp(Mathf.FloorToInt(dano * estados.Length), 0, estados.Length - 1)];

        // Tiembla un instante al recibir un golpe
        temblor = Mathf.Max(0f, temblor - Time.deltaTime);
        sprite.transform.localPosition = new Vector3(Mathf.Sin(Time.time * 80f) * temblor * 0.4f, 0f, 0f);
    }

    /// <summary>¿Detiene una bala que viaja con esta velocidad? Si sí, la maceta recibe el golpe.</summary>
    public bool Bloquea(Vector2 velocidadBala)
    {
        if (!Activo) return false;
        int mirada = jugador != null ? jugador.DireccionMirada : 1;
        if (velocidadBala.x * mirada >= 0f) return false;          // Viene por la espalda (o cae derecho): no la cubre

        temblor = 0.15f;
        arsenal.Gastar();
        if (arsenal.Actual.EsEscudo)
        {
            GestorAudio.Reproducir(Sfx.BloqueoEscudo);
        }
        else
        {
            // Era el último golpe: se rompe en pedazos (Gastar ya regresó a la Talavera)
            GestorAudio.Reproducir(Sfx.RomperEscudo, 0f);
            if (prefabRestos != null) Reciclaje.Sacar(prefabRestos, sprite.bounds.center, Quaternion.identity);
        }
        return true;
    }
}
