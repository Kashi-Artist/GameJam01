using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PocionFalsa : MonoBehaviour
{
    [Header("Configuración")]
    public float velocidadEscalado = 0.1f;
    public float velocidadFlotacion = 0.4f;
    public float amplitudFlotacion = 0.5f;
    
    [Header("Efectos")]
    public ParticleSystem efectoAmbiental; // Partículas ambientales que siempre están activas

    private Vector3 posicionInicial;
    private float tiempoOffset;
    private ParticleSystem instanciaEfectoAmbiental; // Para guardar la instancia
    
    private void Start()
    {
        // Guardar posición inicial para el efecto de flotación
        posicionInicial = transform.position;
        
        // Offset aleatorio para que no todas las pociones floten al mismo tiempo
        tiempoOffset = Random.Range(0f, 2f * Mathf.PI);
        
        // Instanciar efecto ambiental si está configurado
        if (efectoAmbiental != null)
        {
            instanciaEfectoAmbiental = Instantiate(efectoAmbiental, transform.position, Quaternion.identity, transform);
            // Ajustar posición si es necesario
            instanciaEfectoAmbiental.transform.localPosition = Vector3.zero;
        }
    }
    
    private void Update()
    {
        // Escalado suave
        float escalaFactor = 1f + Mathf.Sin((Time.time + tiempoOffset) * velocidadEscalado) * 0.05f;
        transform.localScale = new Vector3(escalaFactor, escalaFactor, escalaFactor);
        
        // Efecto de flotación suave
        float nuevaY = posicionInicial.y + Mathf.Sin((Time.time + tiempoOffset) * velocidadFlotacion) * amplitudFlotacion;
        transform.position = new Vector3(transform.position.x, nuevaY, transform.position.z);
    }
    
    private void OnDestroy()
    {
        // Destruir las partículas ambientales cuando la poción falsa se destruya
        if (instanciaEfectoAmbiental != null)
        {
            // Detener la emisión y dejar que las partículas restantes terminen
            instanciaEfectoAmbiental.Stop();
            Destroy(instanciaEfectoAmbiental.gameObject, instanciaEfectoAmbiental.main.duration);
        }
    }
    
    // A diferencia de la poción normal, no implementamos OnTriggerEnter2D
    // ya que no queremos que esta poción se pueda recolectar
}