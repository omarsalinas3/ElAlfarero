using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Las cerámicas de Mateo (#33): 1 Talavera (infinita), 2 Barro Vidriado (abanico), 3 Barro Negro (granada) y
/// 4 Maceta Escudo. Se cambia con las teclas 1-4, la rueda del mouse o los bumpers; solo se puede elegir un arma que se
/// tenga y con munición. Las armas especiales se consiguen en sus cajas (quedan desbloqueadas en el guardado) y cada
/// nivel empieza con lo que da una caja de cada arma desbloqueada. Al acabarse la munición se vuelve a la Talavera.
/// PlayerShooter lanza la cerámica equipada y llama a Gastar(); la Maceta no se lanza: la sostiene EscudoMaceta.
/// </summary>
public class ArsenalJugador : MonoBehaviour
{
    [Tooltip("En el orden de las teclas 1-4. La primera es la Talavera (infinita).")]
    [SerializeField] CeramicaData[] armas;
    [SerializeField] PlayerShooter tirador;

    int[] municion;
    PlayerController control;
    InputAction accionSiguiente, accionAnterior, accionDirecta;

    public int Indice { get; private set; }
    public CeramicaData Actual => armas[Indice];
    public int Cantidad => armas.Length;
    /// <summary>Munición del arma actual; -1 si no se acaba.</summary>
    public int MunicionActual => Actual.Infinita ? -1 : municion[Indice];
    public event Action OnCambio;

    void Awake()
    {
        if (tirador == null) tirador = GetComponent<PlayerShooter>();
        control = GetComponent<PlayerController>();
        municion = new int[armas.Length];

        accionSiguiente = new InputAction("ArmaSiguiente", InputActionType.Button, "<Gamepad>/rightShoulder");
        accionAnterior = new InputAction("ArmaAnterior", InputActionType.Button, "<Gamepad>/leftShoulder");
        accionDirecta = new InputAction("ArmaDirecta", InputActionType.Button);
        for (int i = 1; i <= 4; i++) accionDirecta.AddBinding("<Keyboard>/" + i);
    }

    void OnEnable() { accionSiguiente.Enable(); accionAnterior.Enable(); accionDirecta.Enable(); }
    void OnDisable() { accionSiguiente.Disable(); accionAnterior.Disable(); accionDirecta.Disable(); }

    void Start()
    {
        // Lo desbloqueado en la partida llega con una caja de munición
        for (int i = 0; i < armas.Length; i++)
            if (!armas[i].Infinita && GestorGuardado.Datos.armasDesbloqueadas.Contains(armas[i].Clave))
                municion[i] = armas[i].municionPorCaja;
        Equipar(0, false);
    }

    void Update()
    {
        // Montado en el alebrije, en pausa o en una cinemática no se cambia de arma
        if (Time.timeScale == 0f || tirador == null || !tirador.enabled) return;
        if (control != null && !control.ControlActivo) return;

        if (accionDirecta.WasPressedThisFrame())
        {
            var tecla = accionDirecta.activeControl as UnityEngine.InputSystem.Controls.KeyControl;
            if (tecla != null) Seleccionar(tecla.keyCode - Key.Digit1);
        }
        if (accionSiguiente.WasPressedThisFrame()) Ciclar(1);
        if (accionAnterior.WasPressedThisFrame()) Ciclar(-1);
        if (Mouse.current != null)
        {
            float rueda = Mouse.current.scroll.ReadValue().y;
            if (rueda > 0.1f) Ciclar(-1);
            else if (rueda < -0.1f) Ciclar(1);
        }
    }

    public CeramicaData Arma(int i) => armas[i];
    public bool Disponible(int i) => i >= 0 && i < armas.Length && (armas[i].Infinita || municion[i] > 0);
    public int MunicionDe(int i) => armas[i].Infinita ? -1 : municion[i];

    /// <summary>Equipa el arma i si se tiene. Devuelve si se pudo.</summary>
    public bool Seleccionar(int i)
    {
        if (!Disponible(i) || i == Indice) return false;
        Equipar(i, true);
        return true;
    }

    /// <summary>La siguiente (o anterior) que se tenga; da la vuelta.</summary>
    public void Ciclar(int paso)
    {
        for (int k = 1; k < armas.Length; k++)
        {
            int i = ((Indice + paso * k) % armas.Length + armas.Length) % armas.Length;
            if (Seleccionar(i)) return;
        }
    }

    /// <summary>Gasta una de munición del arma actual. False si ya no había (no se debe disparar).</summary>
    public bool Gastar()
    {
        if (Actual.Infinita) return true;
        if (municion[Indice] <= 0) return false;
        municion[Indice]--;
        OnCambio?.Invoke();
        if (municion[Indice] == 0) Equipar(0, false);          // Se acabó: de vuelta a la Talavera
        return true;
    }

    /// <summary>Caja de un arma: la desbloquea, le suma munición y la equipa si es nueva para este nivel.</summary>
    public bool Recoger(CeramicaData arma, int cantidad)
    {
        int i = Array.IndexOf(armas, arma);
        if (i < 0 || arma.Infinita || municion[i] >= arma.municionMaxima) return false;
        bool nueva = municion[i] == 0;
        municion[i] = Mathf.Min(arma.municionMaxima, municion[i] + cantidad);
        GestorGuardado.DesbloquearArma(arma.Clave);
        GestorAudio.Reproducir(Sfx.RecogerArma, 0f);
        if (nueva) Equipar(i, false);
        else OnCambio?.Invoke();
        return true;
    }

    void Equipar(int i, bool conSonido)
    {
        Indice = i;
        // La maceta no se lanza: con ella equipada PlayerShooter no dispara y EscudoMaceta la muestra
        if (tirador != null) tirador.EquiparCeramica(Actual.EsEscudo ? null : Actual);
        if (conSonido) GestorAudio.Reproducir(Sfx.CambioArma, 0f);
        OnCambio?.Invoke();
    }
}
