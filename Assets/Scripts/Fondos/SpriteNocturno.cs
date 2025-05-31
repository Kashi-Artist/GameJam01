using System.Collections;
using UnityEngine;

public class SpriteNocturno : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("El sprite que aparecerá en modo nocturno")]
    public SpriteRenderer spriteRenderer;
    
    [Tooltip("Duración de la transición (fade in/out) en segundos")]
    public float duracionTransicion = 1.0f;
    
    [Tooltip("Alpha máximo que alcanzará el sprite (0-1)")]
    [Range(0, 1)]
    public float alphaMaximo = 1.0f;

    private void Awake()
    {
        // Verificar que tengamos una referencia al SpriteRenderer
        if (spriteRenderer == null)
        {
            // Si no se asignó externamente, intentar obtenerlo del mismo GameObject
            spriteRenderer = GetComponent<SpriteRenderer>();
            
            if (spriteRenderer == null)
            {
                Debug.LogError("SpriteNocturno: No se encontró un SpriteRenderer. Por favor asigna uno en el inspector.");
                return;
            }
        }
        
        // Inicializar como invisible
        Color color = spriteRenderer.color;
        color.a = 0f;
        spriteRenderer.color = color;
    }

    // Método público para iniciar el fade in
    public void MostrarSprite()
    {
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }
    
    // Método público para iniciar el fade out
    public void OcultarSprite()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }
    
    // Corrutina para el fade in
    private IEnumerator FadeIn()
    {
        Color color = spriteRenderer.color;
        float alphaInicial = color.a;
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionTransicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionTransicion;
            
            // Interpolar el alpha
            color.a = Mathf.Lerp(alphaInicial, alphaMaximo, t);
            spriteRenderer.color = color;
            
            yield return null;
        }
        
        // Asegurar que llegue al valor final exacto
        color.a = alphaMaximo;
        spriteRenderer.color = color;
    }
    
    // Corrutina para el fade out
    private IEnumerator FadeOut()
    {
        Color color = spriteRenderer.color;
        float alphaInicial = color.a;
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < duracionTransicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionTransicion;
            
            // Interpolar el alpha
            color.a = Mathf.Lerp(alphaInicial, 0f, t);
            spriteRenderer.color = color;
            
            yield return null;
        }
        
        // Asegurar que llegue a completamente transparente
        color.a = 0f;
        spriteRenderer.color = color;
    }
}