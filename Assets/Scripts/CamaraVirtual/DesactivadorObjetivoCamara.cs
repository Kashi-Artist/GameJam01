using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// Componente para crear zonas de desactivación que eliminan objetivos del grupo de la cámara
/// con transiciones personalizables
/// </summary>
public class DesactivadorObjetivoCamara : MonoBehaviour
{
    // Referencia al gestor de la cámara
    [SerializeField] private GestorCamaraCinemachine gestorCamara;
    
    // Lista de objetivos que serán eliminados del grupo
    [SerializeField] private List<Transform> objetivosParaEliminar = new List<Transform>();
    
    [Header("Configuración de Transición")]
    // Tiempo de transición para eliminar los objetivos
    [Range(0.1f, 10f)]
    [SerializeField] private float tiempoTransicion = 2.0f;
    
    // Tipo de suavizado para la transición
    [SerializeField] private GestorCamaraCinemachine.TipoSuavizado tipoSuavizado = 
        GestorCamaraCinemachine.TipoSuavizado.SuaveEntradaSalida;

    [Header("Configuración de Activación")]
    // Tag del objeto que activará este trigger
    [SerializeField] private string tagActivador = "Player";
    
    // Para evitar activaciones múltiples
    private bool yaActivado = false;
    private float tiempoEnfriamiento = 0.5f;
    private float ultimaActivacion = -999f;

    private void Start()
    {
        // Verificar que tenemos los componentes necesarios
        if (gestorCamara == null)
        {
            Debug.LogError("¡El DesactivadorObjetivoCamara necesita una referencia al GestorCamaraCinemachine!");
        }
        
        if (objetivosParaEliminar.Count == 0)
        {
            Debug.LogWarning("¡El DesactivadorObjetivoCamara no tiene objetivos para eliminar!");
        }
        
        // Asegúrate de que este objeto tiene un collider marcado como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("¡El DesactivadorObjetivoCamara necesita un Collider2D!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning("¡El Collider2D del DesactivadorObjetivoCamara debería estar marcado como Trigger!");
            collider.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Evitar activaciones múltiples en un periodo corto de tiempo
        if (Time.time - ultimaActivacion < tiempoEnfriamiento)
            return;
            
        // Verificamos si el objeto que entró tiene el tag esperado
        if (other.CompareTag(tagActivador) && !yaActivado)
        {
            ultimaActivacion = Time.time;
            yaActivado = true;
            
            // Eliminamos todos los objetivos especificados del grupo de la cámara
            foreach (Transform objetivo in objetivosParaEliminar)
            {
                if (objetivo != null)
                {
                    gestorCamara.EliminarObjetivo(objetivo, tiempoTransicion, tipoSuavizado);
                }
            }
            
            // Resetear el indicador después de un tiempo
            StartCoroutine(ResetearActivacion());
        }
    }
    
    private IEnumerator ResetearActivacion()
    {
        // Esperamos un tiempo más largo que el tiempo de transición
        yield return new WaitForSeconds(tiempoTransicion * 1.5f);
        yaActivado = false;
    }
}