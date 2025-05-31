using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlataformaActivable : MonoBehaviour
{
    [Header("Animaciones")]
    [SerializeField] public string animacionCaliente = "PlataformaB_01_Caliente";
    [SerializeField] public string animacionFrio = "PlataformaB_01_Frio";
    
    [Header("Configuración")]
    [SerializeField] private bool iniciarActiva = true;
    
    [Header("Secuencia de Saltos")]
    [SerializeField] private bool usarSecuencia = false;
    [Tooltip("Usa 1 para activar y 0 para desactivar en cada salto (puede tener cualquier longitud)")]
    [SerializeField] private int[] secuenciaSaltos = new int[6] { 0, 0, 0, 0, 0, 0};
    
    private float tiempoMinimoEntreSaltos = 0.8f;
    private Collider2D plataformaCollider;
    private Animator animator;
    private GameObject jugador;
    private bool estabaEnSuelo = true;
    private bool plataformaActiva = true;
    private float ultimoTiempoSalto = 0f;
    private int contadorSaltos = 0;
    
    // Esto identifica de manera única a esta plataforma
    private string identificadorUnico;
    
    // Variable para rastrear el último estado establecido según la secuencia
    private int ultimoEstadoSecuencia = -1;

    private void Awake()
    {
        // Genera un identificador único para esta instancia
        identificadorUnico = gameObject.name + "_" + GetInstanceID();
        
        // Obtener componentes automáticamente
        plataformaCollider = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Buscar el jugador por la etiqueta "Player"
        jugador = GameObject.FindGameObjectWithTag("Player");
        // Verificar las referencias
        if (animator == null)
        {
            Debug.LogError("No se encontró el componente Animator en " + gameObject.name);
        }
        
        if (plataformaCollider == null)
        {
            Debug.LogError("No se encontró el componente Collider2D en " + gameObject.name);
        }
        
        // Validar la secuencia de saltos
        if (usarSecuencia && (secuenciaSaltos == null || secuenciaSaltos.Length == 0))
        {
            Debug.LogWarning("La secuencia de saltos está vacía. Desactivando el modo secuencia.");
            usarSecuencia = false;
        }
        
        // Iniciar en el estado configurado (sin reproducir animación al inicio)
        if (!usarSecuencia)
        {
            plataformaActiva = iniciarActiva;
            if (iniciarActiva)
            {
                ActivarPlataforma(false); // Sin animación inicial
            }
            else
            {
                DesactivarPlataforma(false); // Sin animación inicial
            }
        }
        // Si usamos secuencia, iniciamos según el primer valor
        else if (secuenciaSaltos.Length > 0)
        {
            ultimoEstadoSecuencia = secuenciaSaltos[0]; // Guardamos el estado inicial
            plataformaActiva = secuenciaSaltos[0] == 1;
            
            if (plataformaActiva)
            {
                ActivarPlataforma(false); // Sin animación inicial
            }
            else
            {
                DesactivarPlataforma(false); // Sin animación inicial
            }
        }
    }

    private void Update()
    {
        // Si no tenemos las referencias necesarias, no hacemos nada
        if (jugador == null ) return;
        
        // Detectamos cuando el jugador salta (estaba en suelo y ahora no)
        if (estabaEnSuelo)
        {
            // Verificamos si ha pasado suficiente tiempo desde el último salto
            if (Time.time - ultimoTiempoSalto >= tiempoMinimoEntreSaltos)
            {
                // Incrementamos el contador de saltos
                contadorSaltos++;
                
                if (usarSecuencia && secuenciaSaltos.Length > 0)
                {
                    // Calculamos el índice en la secuencia (cíclico)
                    int indiceSecuencia = (contadorSaltos - 1) % secuenciaSaltos.Length;
                    
                    // Obtenemos el estado según la secuencia actual
                    int estadoSecuenciaActual = secuenciaSaltos[indiceSecuencia];
                    bool nuevoEstado = estadoSecuenciaActual == 1;
                    
                    // Verificamos si hubo un cambio de estado respecto al anterior en la secuencia
                    bool cambioEstado = ultimoEstadoSecuencia != estadoSecuenciaActual;
                    
                    // Solo ejecutamos animación si hay un cambio real en el estado
                    if (cambioEstado)
                    {
                        // Cambiamos el estado según corresponda y reproducimos animación
                        if (nuevoEstado)
                        {
                            ActivarPlataforma(true); // Activar con animación
                        }
                        else
                        {
                            DesactivarPlataforma(true); // Desactivar con animación
                        }
                    }
                    else
                    {
                        // Mantenemos el mismo estado sin reproducir animación
                        if (nuevoEstado)
                        {
                            ActivarPlataforma(false); // Sin animación
                        }
                        else
                        {
                            DesactivarPlataforma(false); // Sin animación
                        }
                    }
                    
                    // Actualizamos el último estado de la secuencia
                    ultimoEstadoSecuencia = estadoSecuenciaActual;
                }
                else
                {
                    // Comportamiento original: alternar en cada salto
                    if (plataformaActiva)
                    {
                        DesactivarPlataforma(true); // Con animación
                    }
                    else
                    {
                        ActivarPlataforma(true); // Con animación
                    }
                    
                    Debug.Log("Salto #" + contadorSaltos + " - Alternando estado a: " + 
                             (plataformaActiva ? "Activada" : "Desactivada"));
                }
                
                // Actualizamos el tiempo del último salto
                ultimoTiempoSalto = Time.time;
            }
        }
    }
    
    private void ActivarPlataforma(bool reproducirAnimacion = true)
    {
        plataformaActiva = true;
        
        // Activamos el collider
        if (plataformaCollider != null)
        {
            plataformaCollider.enabled = true;
        }
        
        // Reproducimos la animación solo si se solicita y tenemos un animator válido
        if (reproducirAnimacion && animator != null)
        {
            // Intenta usar SetTrigger primero
            try {
                animator.SetTrigger("Activar");
            }
            // Si falla, intenta reproducir la animación directamente
            catch {
                try {
                    animator.Play(animacionCaliente);
                }
                catch (System.Exception e) {
                    Debug.Log("No se pudo reproducir la animación: " + e.Message);
                }
            }
        }
    }
    
    private void DesactivarPlataforma(bool reproducirAnimacion = true)
    {
        plataformaActiva = false;
        
        // Desactivamos el collider
        if (plataformaCollider != null)
        {
            plataformaCollider.enabled = false;
        }
        
        // Reproducimos la animación solo si se solicita y tenemos un animator válido
        if (reproducirAnimacion && animator != null)
        {
            // Intenta usar SetTrigger primero
            try {
                animator.SetTrigger("Desactivar");
            }
            // Si falla, intenta reproducir la animación directamente
            catch {
                try {
                    animator.Play(animacionFrio);
                }
                catch (System.Exception e) {
                    Debug.Log("No se pudo reproducir la animación: " + e.Message);
                }
            }
        }
    }
}