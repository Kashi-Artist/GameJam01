using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DetectorPlataforma : MonoBehaviour
{
    [Header("Configuración")]
    public Vector2 tamanoDeteccion = new Vector2(3f, 2f); // X = ancho, Y = alto
    public LayerMask capaJugador;
    public PlataformaMovil_02 plataformaControlada;
    
    [Header("Animación")]
    public Animator animadorObjeto;
    public string nombreParametroActivacion = "Activo";
    
    private bool jugadorDetectado = false;
    
    private void Start()
    {
        // Si no se asignó un animador, intentar encontrarlo en este objeto
        if (animadorObjeto == null)
        {
            animadorObjeto = GetComponent<Animator>();
        }

        // Añadir offset de animación aleatorio si hay animador
        if (animadorObjeto != null)
        {
            // Asume que tu animación está en la primera capa (0) y se llama igual que el parámetro (por ejemplo, "Activo")
            float offsetAleatorio = Random.Range(0f, 1f); // Valor entre 0% y 100% del ciclo
            animadorObjeto.Play(0, 0, offsetAleatorio);
            animadorObjeto.speed = Random.Range(0.8f, 1.2f); // Animaciones un poco más lentas o rápidas
        }
    }
    
    private void Update()
    {
        // Solo actualizar si tenemos una plataforma para controlar
        if (plataformaControlada == null) return;
        
        bool deteccionPrevia = jugadorDetectado;
        
        // Detectar jugador usando un área rectangular (en vez de un círculo)
        Collider2D jugadorCol = Physics2D.OverlapBox(
            transform.position, 
            tamanoDeteccion, 
            0f, // Sin rotación del área
            capaJugador
        );
        
        jugadorDetectado = jugadorCol != null;
        
        // Control de la plataforma según la detección
        if (jugadorDetectado && plataformaControlada.estaMoviendose)
        {
            // Detener la plataforma cuando detectamos al jugador
            plataformaControlada.DetenerTemporalmente();
        }
        else if (!jugadorDetectado && deteccionPrevia && !plataformaControlada.estaMoviendose)
        {
            // Reanudar movimiento cuando el jugador ya no es detectado
            plataformaControlada.ReanudarMovimiento();
        }
        
        // Control de la animación basado en el movimiento de la plataforma
        ActualizarAnimacion();
    }
    
    private void ActualizarAnimacion()
    {
        if (animadorObjeto != null)
        {
            // La animación se activa solo si la plataforma está en movimiento
            bool animacionActiva = plataformaControlada != null && plataformaControlada.estaMoviendose;
            animadorObjeto.SetBool(nombreParametroActivacion, animacionActiva);
        }
    }
    
    // Para visualizar el área de detección rectangular en el editor
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, tamanoDeteccion);
    }
}