using System.Collections;
using UnityEngine;

/// Componente para crear zonas de activación que añaden objetivos al grupo de la cámara
public class ActivadorObjetivoCamara : MonoBehaviour
{
    // Referencia al gestor de la cámara
    [SerializeField] private GestorCamaraCinemachine gestorCamara;

    // Objeto que será añadido como objetivo
    [SerializeField] private Transform objetivoParaSeguir;

    [Header("Configuración del Objetivo")]
    // Peso que tendrá este objetivo en el grupo
    [Range(0.1f, 20f)]
    [SerializeField] private float pesoObjetivo = 1.0f;

    // Radio del objetivo para la cámara
    [Range(0.5f, 20f)]
    [SerializeField] private float radioObjetivo = 5.0f;

    [Header("Configuración de Transición")]
    // Tiempo de transición específico para este objetivo
    [Range(0.1f, 10f)]
    [SerializeField] private float tiempoTransicion = 2.0f;
    
    // Factor de aceleración cuando el jugador sale durante la transición (menor = más rápido)
    [Range(0.1f, 1.0f)]
    [SerializeField] private float factorAceleracion = 0.25f;

    // Tipo de suavizado para la transición
    [SerializeField] private GestorCamaraCinemachine.TipoSuavizado tipoSuavizado = 
        GestorCamaraCinemachine.TipoSuavizado.SuaveEntradaSalida;

    [Header("Opciones Adicionales")]
    // Tag del objeto que activará este trigger (normalmente el jugador)
    [SerializeField] private string tagActivador = "Player";

    // Si se marca, el objetivo se eliminará automáticamente cuando el activador salga del trigger
    [SerializeField] private bool eliminarAlSalir = true;

    // Tiempo mínimo que el jugador debe permanecer en la zona para activar el objetivo
    [Range(0.0f, 5f)]
    [SerializeField] private float tiempoMinimoActivacion = 0.5f;
    
    // NUEVA OPCIÓN: Tiempo que el jugador debe permanecer idle para desactivar el objetivo
    [Range(0.0f, 10f)]
    [SerializeField] private float tiempoIdleParaDesactivar = 5.0f;
    
    // NUEVA OPCIÓN: Activar o desactivar la eliminación por inactividad
    [SerializeField] private bool eliminarPorInactividad = true;

    // NUEVA OPCIÓN: Tiempo de bloqueo después de desactivación por inactividad
    [SerializeField] private float tiempoBloqueoTrasInactividad = 20f;

    // Para rastrear si el objetivo está actualmente activado
    private bool objetivoActivado = false;
    private bool jugadorEnZona = false;
    private float tiempoEnZona = 0f;
    private Coroutine corrutinaActivacion;
    private Coroutine corrutinaDesactivacion;
    private bool transicionEnProgreso = false;
    
    // NUEVAS VARIABLES para gestionar tiempo de inactividad
    private float tiempoInactividad = 0f;
    private Animator animadorJugador;
    private Transform jugador;
    private Coroutine corrutinaInactividad;
    private Vector3? posicionAnteriorJugador;
    
    // NUEVA VARIABLE para evitar la reactivación cíclica
    private bool bloqueadoPorInactividad = false;
    private float tiempoBloqueoRestante = 0f;

    private void Start()
    {
        // Verificar que tenemos los componentes necesarios
        if (gestorCamara == null)
        {
            Debug.LogError("¡El ActivadorObjetivoCamara necesita una referencia al GestorCamaraCinemachine!");
        }
        
        if (objetivoParaSeguir == null)
        {
            Debug.LogError("¡El ActivadorObjetivoCamara necesita un Transform objetivo para seguir!");
        }
        
        // Asegúrate de que este objeto tiene un collider marcado como trigger
        Collider2D collider = GetComponent<Collider2D>();
        if (collider == null)
        {
            Debug.LogError("¡El ActivadorObjetivoCamara necesita un Collider2D!");
        }
        else if (!collider.isTrigger)
        {
            collider.isTrigger = true;
        }
    }

    private void Update()
    {
        // Gestión del tiempo de bloqueo tras inactividad
        if (bloqueadoPorInactividad)
        {
            tiempoBloqueoRestante -= Time.deltaTime;
            if (tiempoBloqueoRestante <= 0f)
            {
                bloqueadoPorInactividad = false;
            }
        }

        // Si el jugador está en la zona, no hay transición en progreso y no está bloqueado por inactividad
        if (jugadorEnZona && !objetivoActivado && !transicionEnProgreso && !bloqueadoPorInactividad)
        {
            tiempoEnZona += Time.deltaTime;
            
            if (tiempoEnZona >= tiempoMinimoActivacion)
            {
                corrutinaActivacion = StartCoroutine(ActivarObjetivoConVerificacion());
            }
        }
        
        // NUEVA LÓGICA: Verificar si el jugador está inactivo
        if (jugadorEnZona && objetivoActivado && eliminarPorInactividad && jugador != null)
        {
            // Verificamos si el jugador está en idle
            bool estaIdle = ComprobarSiEstaIdle();
            
            if (estaIdle)
            {
                tiempoInactividad += Time.deltaTime;
                
                // Si supera el tiempo de inactividad y no hay una desactivación en progreso
                if (tiempoInactividad >= tiempoIdleParaDesactivar && corrutinaDesactivacion == null && !transicionEnProgreso)
                {
                    corrutinaDesactivacion = StartCoroutine(DesactivarObjetivoInactividad());
                }
            }
            else
            {
                // Reiniciamos el contador de inactividad
                tiempoInactividad = 0f;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(tagActivador))
        {
            // Guardamos la referencia al jugador y su animator
            jugador = other.transform;
            if (animadorJugador == null && jugador != null)
            {
                animadorJugador = jugador.GetComponent<Animator>();
            }
            
            // Cancelar cualquier desactivación pendiente
            if (corrutinaDesactivacion != null)
            {
                StopCoroutine(corrutinaDesactivacion);
                corrutinaDesactivacion = null;
                transicionEnProgreso = false;
            }

            jugadorEnZona = true;
            tiempoEnZona = 0f;
            tiempoInactividad = 0f; // Reiniciamos el tiempo de inactividad
            
            // Solo activamos si no está bloqueado por inactividad
            if (tiempoMinimoActivacion <= 0f && !objetivoActivado && !transicionEnProgreso && !bloqueadoPorInactividad)
            {
                corrutinaActivacion = StartCoroutine(ActivarObjetivoConVerificacion());
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(tagActivador))
        {
            jugadorEnZona = false;
            tiempoEnZona = 0f;
            tiempoInactividad = 0f; // Reiniciamos el tiempo de inactividad
            
            // Cancelar cualquier activación pendiente
            if (corrutinaActivacion != null)
            {
                StopCoroutine(corrutinaActivacion);
                corrutinaActivacion = null;
                transicionEnProgreso = false;
            }
            
            // Si ya existe una desactivación en progreso, la detenemos para iniciar una más rápida
            if (corrutinaDesactivacion != null)
            {
                StopCoroutine(corrutinaDesactivacion);
                corrutinaDesactivacion = null;
            }
            
            // Si está configurado para eliminar al salir y el objetivo está activado
            if (eliminarAlSalir && objetivoActivado)
            {
                // Verificar que el GameObject está activo antes de iniciar la corrutina
                if (gameObject.activeInHierarchy)
                {
                    // Si estamos en transición, aceleramos la desactivación
                    if (transicionEnProgreso)
                    {
                        // Usar una transición rápida (tiempo reducido)
                        corrutinaDesactivacion = StartCoroutine(DesactivarObjetivoRapido());
                    }
                    else
                    {
                        corrutinaDesactivacion = StartCoroutine(DesactivarObjetivoConVerificacion());
                    }
                }
                else
                {
                    // Si el objeto está inactivo, desactivar el objetivo inmediatamente sin corrutina
                    if (gestorCamara != null)
                    {
                        gestorCamara.EliminarObjetivo(objetivoParaSeguir, 0f, tipoSuavizado);
                        objetivoActivado = false;
                    }
                }
            }
            
            // Al salir, también eliminamos el bloqueo por inactividad
            bloqueadoPorInactividad = false;
        }
    }

    private IEnumerator ActivarObjetivoConVerificacion()
    {
        transicionEnProgreso = true;
        
        // Esperamos el tiempo de transición
        yield return new WaitForSeconds(tiempoTransicion);
        
        // Verificamos si todavía estamos en la zona antes de activar y que no se ha bloqueado mientras tanto
        if (jugadorEnZona && !bloqueadoPorInactividad)
        {
            gestorCamara.AñadirObjetivo(objetivoParaSeguir, pesoObjetivo, radioObjetivo, 
                                     0f, tipoSuavizado); // 0f para activación inmediata ahora
            objetivoActivado = true;
        }
        
        transicionEnProgreso = false;
    }

    private IEnumerator DesactivarObjetivoConVerificacion()
    {
        transicionEnProgreso = true;
        
        // Esperamos el tiempo de transición
        yield return new WaitForSeconds(tiempoTransicion);
        
        // Verificamos que el jugador no haya vuelto a entrar
        if (!jugadorEnZona)
        {
            gestorCamara.EliminarObjetivo(objetivoParaSeguir, 0f, tipoSuavizado);
            objetivoActivado = false;
        }
        
        transicionEnProgreso = false;
    }
    
    // Corrutina para desactivar rápidamente el objetivo cuando el jugador sale durante una transición
    private IEnumerator DesactivarObjetivoRapido()
    {
        transicionEnProgreso = true;
        
        // Tiempo de transición acelerado según el factor configurable
        float tiempoTransicionRapida = tiempoTransicion * factorAceleracion;
        
        // Esperamos el tiempo de transición acelerado
        yield return new WaitForSeconds(tiempoTransicionRapida);
        
        // Verificamos que el jugador no haya vuelto a entrar
        if (!jugadorEnZona)
        {
            // Usamos una transición más rápida al eliminar el objetivo
            float velocidadTransicion = tiempoTransicion * factorAceleracion;
            gestorCamara.EliminarObjetivo(objetivoParaSeguir, velocidadTransicion, tipoSuavizado);
            objetivoActivado = false;
            
            // Registramos en la consola para debug
        }
        
        transicionEnProgreso = false;
    }
    
    // NUEVA CORRUTINA: Para desactivar el objetivo por inactividad del jugador
    // NUEVA CORRUTINA: Para desactivar el objetivo por inactividad del jugador
private IEnumerator DesactivarObjetivoInactividad()
    {
        transicionEnProgreso = true;
                
        // Utilizamos el tiempo de transición normal para asegurar una transición suave
        // en lugar de reducirlo a la mitad como antes
        gestorCamara.EliminarObjetivo(objetivoParaSeguir, tiempoTransicion, tipoSuavizado);
        objetivoActivado = false;
        
        // Esperamos el tiempo completo de la transición antes de finalizar
        yield return new WaitForSeconds(tiempoTransicion);
        
        // NUEVO: Activamos el bloqueo por inactividad
        bloqueadoPorInactividad = true;
        tiempoBloqueoRestante = tiempoBloqueoTrasInactividad;
        Debug.Log($"Objeto desactivado por inactividad para {gameObject.name}. Bloqueado por {tiempoBloqueoTrasInactividad}s");
        
        transicionEnProgreso = false;
        corrutinaDesactivacion = null;
    }

    // Método público para desactivar manualmente el objetivo si fuera necesario
    public void DesactivarObjetivoManualmente()
    {
        if (objetivoActivado)
        {
            gestorCamara.EliminarObjetivo(objetivoParaSeguir, 0f, tipoSuavizado);
            objetivoActivado = false;
        }
    }
    
    // Reemplazar la verificación de inactividad con un enfoque más robusto
    private bool ComprobarSiEstaIdle()
    {
        // Si tenemos acceso al animator, usamos sus parámetros
        if (animadorJugador != null)
        {
            float velocidadX = animadorJugador.GetFloat("Horizontal1");
            bool enSuelo = animadorJugador.GetBool("enSuelo1");
            
            // Consideramos idle si está en suelo y no se mueve horizontalmente
            return Mathf.Abs(velocidadX) < 0.1f && enSuelo;
        }
        else if (jugador != null)
        {
            // Alternativa cuando no hay animator: comprobar si el jugador se ha movido
            // Esto requiere mantener registro de la posición previa
            Vector3 posicionActual = jugador.position;
            
            // Comparar con la posición anterior (necesitarías declarar esta variable de clase)
            if (!posicionAnteriorJugador.HasValue)
            {
                posicionAnteriorJugador = posicionActual;
                return true;
            }
            
            // Verificar si el jugador se ha movido significativamente
            bool noSeHaMovido = Vector3.Distance(posicionActual, posicionAnteriorJugador.Value) < 0.01f;
            
            // Actualizar la posición anterior para la próxima comprobación
            posicionAnteriorJugador = posicionActual;
            
            return noSeHaMovido;
        }
        
        return false;
    }
}