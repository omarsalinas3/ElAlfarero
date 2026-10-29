using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Sonido y pequeño rebote al seleccionar un botón (mouse o teclado) y sonido al presionarlo.</summary>
[RequireComponent(typeof(Button))]
public class BotonConSonido : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler
{
    [SerializeField] float escalaSeleccionado = 1.08f;

    Vector3 escalaBase;
    bool seleccionado;

    void Awake()
    {
        escalaBase = transform.localScale;
        GetComponent<Button>().onClick.AddListener(() => GestorAudio.Reproducir(Sfx.MenuAceptar, 0f));
    }

    void OnDisable()
    {
        seleccionado = false;
        transform.localScale = escalaBase;
    }

    public void OnSelect(BaseEventData e)
    {
        seleccionado = true;
        GestorAudio.Reproducir(Sfx.MenuMover, 0f);
    }

    public void OnDeselect(BaseEventData e) { seleccionado = false; }

    // Con el mouse: pasar encima también selecciona (así teclado y mouse no muestran dos botones "activos")
    public void OnPointerEnter(PointerEventData e)
    {
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != gameObject)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    void Update()
    {
        float objetivo = seleccionado ? escalaSeleccionado : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, escalaBase * objetivo, Time.unscaledDeltaTime * 14f);
    }
}
