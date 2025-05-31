// TextoInteractivoBoton.cs
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class TextoInteractivoBoton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{   
    [Header("Configuración de Texto")]
    public TextMeshProUGUI texto;

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorHover = Color.yellow;
    public Color colorPresionado = Color.red;

    [Header("Escala")]
    private Vector3 escalaOriginal;
    public float escalaHover = 1.1f; // 10% más grande

    [Header("Audio")]
    public AudioClip sonidoBoton; // Puedes asignar un sonido específico para este botón (opcional)
    public bool usarSonidoGlobal = true; // Si es true, usa el sonido global del AudioManager

    void Start()
    {
        // Guardar la escala original del texto
        escalaOriginal = texto != null ? texto.rectTransform.localScale : Vector3.one;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (texto != null)
        {
            texto.color = colorHover;
            texto.rectTransform.localScale = escalaOriginal * escalaHover;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (texto != null)
        {
            texto.color = colorNormal;
            texto.rectTransform.localScale = escalaOriginal;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (texto != null)
        {
            texto.color = colorPresionado;
        }
        
        // Reproducir sonido al presionar el botón
        ReproducirSonido();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (texto != null)
        {
            texto.color = colorHover;
        }
    }
    
    private void ReproducirSonido()
    {
        // Si está configurado para usar sonido global y existe el AudioManager
        if (usarSonidoGlobal && SimpleAudioSystem.Instance != null)
        {
            SimpleAudioSystem.Instance.PlayButtonSound();
        }
        // Si tiene un sonido específico asignado
        else if (sonidoBoton != null && SimpleAudioSystem.Instance != null)
        {
            SimpleAudioSystem.Instance.sfxSource.PlayOneShot(sonidoBoton);
        }
    }
}