using UnityEngine;

/// <summary>
/// Combate cuerpo a cuerpo de Mateo (#62), como en Metal Slug: si hay un enemigo muy cerca enfrente, el clic (o Espacio)
/// golpea en lugar de lanzar cerámica. Clics seguidos encadenan un combo de 3: dos puñetazos y un gancho que empuja al
/// enemigo y sacude la cámara. Si pasa más de ventanaCombo sin otro clic, el combo vuelve a empezar.
/// Mientras golpea Mateo se queda quieto. Lo decide PlayerShooter (que es quien lee el clic) llamando a Golpear().
/// </summary>
public class CombateCuerpo : MonoBehaviour
{
    [System.Serializable]
    public struct Golpe
    {
        public string estado;          // Estado del Animator de Mateo
        public int dano;
        public float duracion;         // Lo que dura la animación (Mateo no se mueve)
        public float impacto;          // Segundos desde el inicio en que el puño conecta
        public bool fuerte;            // Empuja, sacude la cámara y suena distinto
    }

    [SerializeField] Golpe[] combo =
    {
        new Golpe { estado = "Golpe1", dano = 10, duracion = 0.25f, impacto = 0.12f },
        new Golpe { estado = "Golpe2", dano = 10, duracion = 0.25f, impacto = 0.12f },
        new Golpe { estado = "Golpe3", dano = 25, duracion = 0.4f, impacto = 0.2f, fuerte = true },
    };
    [SerializeField] float ventanaCombo = 0.4f;
    [Tooltip("Qué tan cerca (desde el frente de Mateo) tiene que estar un enemigo para golpear en vez de lanzar.")]
    [SerializeField] float alcance = 1.3f;
    [SerializeField] Vector2 cajaGolpe = new Vector2(1.5f, 1.8f);
    [SerializeField] Vector2 empujeFuerte = new Vector2(8f, 4f);
    [SerializeField] LayerMask capasEnemigo;
    [SerializeField] Animator animator;

    PlayerController jugador;
    Rigidbody2D rb;
    Health salud;
    int paso = -1;                         // Golpe en curso o último que se dio (0..2); -1 = sin combo
    float inicioGolpe, finGolpe = -10f;
    bool conecto, siguientePedido;
    readonly Collider2D[] encontrados = new Collider2D[12];

    /// <summary>Número del golpe actual o último del combo (1..3); 0 si no hay combo.</summary>
    public int Paso => paso + 1;
    public bool Golpeando => Time.time < finGolpe;

    void Awake()
    {
        jugador = GetComponent<PlayerController>();
        rb = GetComponent<Rigidbody2D>();
        salud = GetComponent<Health>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    /// <summary>¿Hay un enemigo vivo pegado enfrente? Ahí el clic golpea en lugar de lanzar.</summary>
    public bool EnemigoCerca() => Buscar(alcance / 2f + 0.2f, new Vector2(alcance, cajaGolpe.y), false) > 0;

    /// <summary>Clic de golpe: inicia el siguiente golpe del combo (si está golpeando, se encadena al terminar).</summary>
    public void Golpear()
    {
        if (Golpeando) { siguientePedido = true; return; }
        bool sigueCombo = paso >= 0 && paso < combo.Length - 1 && Time.time - finGolpe <= ventanaCombo;
        Iniciar(sigueCombo ? paso + 1 : 0);
    }

    void Iniciar(int i)
    {
        paso = i;
        siguientePedido = false;
        conecto = false;
        inicioGolpe = Time.time;
        finGolpe = Time.time + combo[i].duracion;
        if (animator != null) animator.Play(combo[i].estado, 0, 0f);
        GestorAudio.Reproducir(Sfx.Evadir, 0.15f);           // Zumbido del puño
    }

    void Update()
    {
        if (paso < 0) return;
        var g = combo[paso];

        if (Golpeando)
        {
            // Quieto mientras golpea (sin quitarle la gravedad si lo agarra en el aire)
            jugador.AplicarEmpuje(new Vector2(0f, rb.linearVelocity.y), 0.05f);
            if (!conecto && Time.time - inicioGolpe >= g.impacto) Conectar(g);
            return;
        }

        if (finGolpe > 0f && animator != null && animator.GetCurrentAnimatorStateInfo(0).IsName(g.estado))
            animator.Play("Idle", 0, 0f);                     // De ahí el Animator sigue solo (correr, saltar)
        if (siguientePedido && paso < combo.Length - 1) Iniciar(paso + 1);
        else siguientePedido = false;
    }

    void Conectar(Golpe g)
    {
        conecto = true;
        int lado = jugador.DireccionMirada;
        int n = Buscar(cajaGolpe.x / 2f + 0.3f, cajaGolpe, true);
        bool pego = false;
        for (int i = 0; i < n; i++)
        {
            Health h = encontrados[i].GetComponentInParent<Health>();
            if (h == null || h == salud || h.EstaMuerto) continue;
            bool repetido = false;                             // Un enemigo con varios colliders recibe un solo golpe
            for (int j = 0; j < i && !repetido; j++) repetido = encontrados[j].GetComponentInParent<Health>() == h;
            if (repetido) continue;
            h.RecibirDano(g.dano);
            pego = true;
            var rbEnemigo = h.GetComponent<Rigidbody2D>();
            if (g.fuerte && rbEnemigo != null && rbEnemigo.bodyType == RigidbodyType2D.Dynamic)
                rbEnemigo.linearVelocity = new Vector2(lado * empujeFuerte.x, empujeFuerte.y);   // Después de su propio retroceso
        }
        if (!pego) return;
        GestorAudio.Reproducir(g.fuerte ? Sfx.PunoFuerte : Sfx.PunoLigero);
        if (g.fuerte)
        {
            Efectos.Particulas(TipoParticula.Chispas, (Vector2)transform.position + new Vector2(jugador.DireccionMirada * 1.3f, 1.3f));
            Efectos.GolpeFuerte(0.07f, 0.15f);
        }
    }

    // Caja enfrente de Mateo, a media altura del cuerpo. conHuacales: el golpe también rompe huacales y hornos
    int Buscar(float distancia, Vector2 tam, bool conHuacales)
    {
        Vector2 centro = (Vector2)transform.position + new Vector2(jugador.DireccionMirada * distancia, 1.2f);
        int capas = capasEnemigo.value | (conHuacales ? 1 << 6 : 0);
        var filtro = new ContactFilter2D { useLayerMask = true, layerMask = capas, useTriggers = true };
        int n = Physics2D.OverlapBox(centro, tam, 0f, filtro, encontrados);
        if (conHuacales) return n;
        int vivos = 0;
        for (int i = 0; i < n; i++)
        {
            var h = encontrados[i].GetComponentInParent<Health>();
            if (h != null && !h.EstaMuerto) vivos++;
        }
        return vivos;
    }

    void OnDrawGizmosSelected()
    {
        if (jugador == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + new Vector3(jugador.DireccionMirada * (cajaGolpe.x / 2f + 0.3f), 1.2f), cajaGolpe);
    }
}
