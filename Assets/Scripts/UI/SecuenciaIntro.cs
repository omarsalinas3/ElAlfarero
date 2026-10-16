using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Intro de la historia: viñetas con ilustración y texto que se escribe letra por letra.
/// Enter / Espacio / clic: completa el texto o pasa a la siguiente viñeta. Esc: salta toda la intro.
/// Al terminar carga el Nivel 1.
/// </summary>
public class SecuenciaIntro : MonoBehaviour
{
    [Serializable]
    public class Vineta
    {
        public Sprite imagen;
        [TextArea(2, 4)] public string texto;
    }

    [SerializeField] Vineta[] vinetas;
    [SerializeField] Image imagen;
    [SerializeField] CanvasGroup grupoImagen;
    [SerializeField] Text texto;
    [SerializeField] float letrasPorSegundo = 38f;
    [SerializeField] string escenaSiguiente = Escenas.Nivel1;

    InputAction accionAvanzar, accionSaltar;
    bool avanzarPedido;

    void Awake()
    {
        accionAvanzar = new InputAction("Avanzar", InputActionType.Button, "<Keyboard>/enter");
        accionAvanzar.AddBinding("<Keyboard>/space");
        accionAvanzar.AddBinding("<Mouse>/leftButton");
        accionAvanzar.AddBinding("<Gamepad>/buttonSouth");
        accionSaltar = new InputAction("SaltarIntro", InputActionType.Button, "<Keyboard>/escape");
        accionSaltar.AddBinding("<Gamepad>/start");
    }

    void OnEnable() { accionAvanzar.Enable(); accionSaltar.Enable(); }
    void OnDisable() { accionAvanzar.Disable(); accionSaltar.Disable(); }

    void Start()
    {
        GestorAudio.CambiarMusica(Musica.Intro, 1.5f);
        StartCoroutine(Reproducir());
    }

    void Update()
    {
        if (accionAvanzar.WasPressedThisFrame()) avanzarPedido = true;
        if (accionSaltar.WasPressedThisFrame()) Terminar();
    }

    IEnumerator Reproducir()
    {
        foreach (var v in vinetas)
        {
            imagen.sprite = v.imagen;
            imagen.preserveAspect = true;
            texto.text = "";

            // Aparece la imagen (fundido) mientras empieza un zoom lento
            StartCoroutine(Zoom(imagen.rectTransform, 7f));
            for (float t = 0f; t < 0.6f; t += Time.deltaTime) { grupoImagen.alpha = t / 0.6f; yield return null; }
            grupoImagen.alpha = 1f;

            // Máquina de escribir
            avanzarPedido = false;
            for (float n = 0f; n < v.texto.Length && !avanzarPedido; n += Time.deltaTime * letrasPorSegundo)
            {
                texto.text = v.texto.Substring(0, Mathf.Min(v.texto.Length, (int)n));
                yield return null;
            }
            texto.text = v.texto;

            // Espera a que el jugador avance (o 6 segundos)
            avanzarPedido = false;
            for (float t = 0f; t < 6f && !avanzarPedido; t += Time.deltaTime) yield return null;
            GestorAudio.Reproducir(Sfx.MenuMover, 0f);

            for (float t = 0f; t < 0.4f; t += Time.deltaTime) { grupoImagen.alpha = 1f - t / 0.4f; yield return null; }
        }
        Terminar();
    }

    IEnumerator Zoom(RectTransform rt, float duracion)
    {
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            rt.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, t / duracion);
            yield return null;
        }
    }

    void Terminar()
    {
        if (CambioEscena.Cargando) return;
        StopAllCoroutines();
        CambioEscena.Cargar(escenaSiguiente);
    }
}
