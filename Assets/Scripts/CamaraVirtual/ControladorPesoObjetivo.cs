using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Componente para modificar dinámicamente el peso y radio de un objetivo en el grupo de la cámara
/// </summary>
public class ControladorPesoObjetivo : MonoBehaviour
{
    // Referencia al gestor de la cámara
    [SerializeField] private GestorCamaraCinemachine gestorCamara;
    
    // Objetivo cuyo peso y radio queremos modificar
    [SerializeField] private Transform objetivo;
    
    // Nuevo peso para el objetivo
    [SerializeField] private float nuevoPeso = 1.5f;
    
    // Nuevo radio para el objetivo
    [SerializeField] private float nuevoRadio = 5.0f;
    
    // Tag del objeto que activará este trigger (normalmente el jugador)
    [SerializeField] private string tagActivador = "Player";
    
    // Si está activado, restaurará los valores anteriores al salir
    [SerializeField] private bool restaurarAlSalir = true;
    
    // Valores originales para restaurar
    [SerializeField] private float pesoOriginal = 1.0f;
    [SerializeField] private float radioOriginal = 3.0f;

    private void Start()
    {
        // Verificar que tenemos los componentes necesarios
        if (gestorCamara == null)
        {
            Debug.LogError("¡El ControladorPesoObjetivo necesita una referencia al GestorCamaraCinemachine!");
        }
        
        if (objetivo == null)
        {
            Debug.LogError("¡El ControladorPesoObjetivo necesita un Transform objetivo!");
        }
        
        // Asegúrate de que este objeto tiene un collider marcado como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("¡El ControladorPesoObjetivo necesita un Collider2D!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("¡El Collider2D del ControladorPesoObjetivo debería estar marcado como Trigger!");
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificamos si el objeto que entró tiene el tag esperado
        if (other.CompareTag(tagActivador))
        {
            // Modificamos el peso y radio del objetivo
            gestorCamara.ActualizarObjetivo(objetivo, nuevoPeso, nuevoRadio);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Si está configurado para restaurar al salir y el objeto que sale tiene el tag esperado
        if (restaurarAlSalir && other.CompareTag(tagActivador))
        {
            // Restauramos el peso y radio originales
            gestorCamara.ActualizarObjetivo(objetivo, pesoOriginal, radioOriginal);
        }
    }
}