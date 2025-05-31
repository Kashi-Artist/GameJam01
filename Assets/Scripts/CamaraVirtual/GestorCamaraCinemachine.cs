using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// Gestor para la cámara Cinemachine que controla los objetivos en el Target Group
/// con transiciones suaves y personalizables
public class GestorCamaraCinemachine : MonoBehaviour
{
    // Referencia al grupo de objetivos de Cinemachine
    [SerializeField] private CinemachineTargetGroup targetGroup;
    
    // El jugador (siempre presente en el grupo)
    [SerializeField] private Transform jugador;
    
    // Tiempo de transición predeterminado
    [SerializeField] private float tiempoTransicionPredeterminado = 2.0f;
    
    [Header("Configuración de Radio del Jugador")]
    // Radio del jugador en estado idle
    [SerializeField] private float radioJugadorIdle = 2.0f;
    // Radio del jugador en movimiento
    [SerializeField] private float radioJugadorMovimiento = 3.0f;
    // Tiempo mínimo que debe permanecer en un estado para cambiar
    [SerializeField] private float tiempoMinimoCambioEstado = 2.0f;
    // Tiempo de transición para cambio de radio
    [SerializeField] private float tiempoTransicionRadio = 2.0f;
    
    // Referencia al Animator del jugador
    [SerializeField] private Animator animadorJugador;
    
    // Diccionario para mantener seguimiento de las corrutinas activas
    private Dictionary<Transform, Coroutine> corrutinasActivas = new Dictionary<Transform, Coroutine>();
    private Dictionary<Transform, Queue<TransicionPendiente>> transicionesPendientes = new Dictionary<Transform, Queue<TransicionPendiente>>();
    
    // Para comprobar si un objeto ya está en la lista de objetivos
    private Dictionary<Transform, bool> objetosEnGrupo = new Dictionary<Transform, bool>();

    // Control del estado de animación
    private bool estaEnIdle = true;
    private float tiempoEnEstadoActual = 0f;
    private Coroutine corrutinaCambioRadioJugador;
    private bool transicionRadioEnProgreso = false;
    
    // Variable para controlar si el componente está siendo destruido
    private bool seEstaDestruyendo = false;

    // Estructura para almacenar transiciones pendientes
    private struct TransicionPendiente
    {
        public bool esAñadir;
        public float peso;
        public float radio;
        public float tiempoTransicion;
        public TipoSuavizado tipoSuavizado;

        public TransicionPendiente(bool esAñadir, float peso, float radio, float tiempoTransicion, TipoSuavizado tipoSuavizado)
        {
            this.esAñadir = esAñadir;
            this.peso = peso;
            this.radio = radio;
            this.tiempoTransicion = tiempoTransicion;
            this.tipoSuavizado = tipoSuavizado;
        }
    }

    // Tipos de suavizado para la transición
    public enum TipoSuavizado
    {
        Lineal,
        SuaveEntrada,    // Ease In
        SuaveSalida,     // Ease Out
        SuaveEntradaSalida // Ease In-Out
    }

    private void Start()
    {
        // Inicializamos validando componentes
        if (targetGroup == null)
        {
            Debug.LogError("¡Necesitas asignar un CinemachineTargetGroup al GestorCamaraCinemachine!");
            return;
        }

        if (jugador == null)
        {
            Debug.LogError("¡Necesitas asignar el transform del jugador al GestorCamaraCinemachine!");
            return;
        }

        if (animadorJugador == null)
        {
            // Intentamos obtener el Animator del jugador si no está asignado
            animadorJugador = jugador.GetComponent<Animator>();
            if (animadorJugador == null)
            {
                Debug.LogWarning("No se encontró un Animator en el jugador. El cambio de radio según animación no funcionará.");
            }
        }

        // Verificamos si el jugador ya está en el grupo
        bool jugadorEncontrado = false;
        for (int i = 0; i < targetGroup.m_Targets.Length; i++)
        {
            if (targetGroup.m_Targets[i].target == jugador)
            {
                jugadorEncontrado = true;
                objetosEnGrupo[jugador] = true;
                // Configuramos el radio inicial según el estado idle
                var miembro = targetGroup.m_Targets[i];
                miembro.radius = radioJugadorIdle;
                targetGroup.m_Targets[i] = miembro;
                break;
            }
        }

        // Si el jugador no está en el grupo, lo añadimos
        if (!jugadorEncontrado)
        {
            targetGroup.AddMember(jugador, 1.0f, radioJugadorIdle);
            objetosEnGrupo[jugador] = true;
        }

        // Inicializamos el diccionario de transiciones pendientes para el jugador
        transicionesPendientes[jugador] = new Queue<TransicionPendiente>();
    }

    private void Update()
    {
        // Evitamos ejecución si el componente está siendo destruido
        if (seEstaDestruyendo || targetGroup == null || jugador == null)
            return;
            
        // Solo monitoreamos si tenemos un animator asignado
        if (animadorJugador != null)
        {
            // Verificamos el estado actual de la animación
            bool estadoIdleActual = EstaEnIdle();
            
            // Si el estado cambió, reiniciamos el contador
            if (estadoIdleActual != estaEnIdle)
            {
                estaEnIdle = estadoIdleActual;
                tiempoEnEstadoActual = 0f;
            }
            else
            {
                // Incrementamos el tiempo en el estado actual
                tiempoEnEstadoActual += Time.deltaTime;
                
                // Si ha permanecido suficiente tiempo y no hay una transición en progreso
                if (tiempoEnEstadoActual >= tiempoMinimoCambioEstado && !transicionRadioEnProgreso)
                {
                    // Actualizamos el radio según el estado
                    ActualizarRadioJugadorSegunEstado();
                }
            }
        }
    }
    
    private void OnDisable()
    {
        DetenerTodasLasCorrutinas();
    }
    
    private void OnDestroy()
    {
        seEstaDestruyendo = true;
        DetenerTodasLasCorrutinas();
    }
    
    private void DetenerTodasLasCorrutinas()
    {
        // Detenemos todas las corrutinas activas
        foreach (var corrutina in corrutinasActivas.Values)
        {
            if (corrutina != null)
                StopCoroutine(corrutina);
        }
        
        corrutinasActivas.Clear();
        
        // Detenemos la corrutina de cambio de radio si existe
        if (corrutinaCambioRadioJugador != null)
        {
            StopCoroutine(corrutinaCambioRadioJugador);
            corrutinaCambioRadioJugador = null;
        }
        
        // Limpiamos todas las transiciones pendientes
        foreach (var cola in transicionesPendientes.Values)
        {
            cola.Clear();
        }
        
        transicionesPendientes.Clear();
        transicionRadioEnProgreso = false;
    }

    /// Determina si el jugador está en estado idle basado en el Animator
    private bool EstaEnIdle()
    {
        if (animadorJugador == null)
            return true;

        float velocidadX = animadorJugador.GetFloat("MovimientoX");
        // Consideramos que está en idle si no se mueve
        return velocidadX < 0.1f;
    }


    /// Actualiza el radio del jugador según su estado de animación
    private void ActualizarRadioJugadorSegunEstado()
    {
        // Comprobamos que el jugador y el target group existen
        if (seEstaDestruyendo || targetGroup == null || jugador == null)
            return;
            
        float nuevoRadio = estaEnIdle ? radioJugadorIdle : radioJugadorMovimiento;
        
        // Verificamos el radio actual
        float radioActual = 0f;
        bool jugadorEncontrado = false;
        
        for (int i = 0; i < targetGroup.m_Targets.Length; i++)
        {
            if (targetGroup.m_Targets[i].target == jugador)
            {
                radioActual = targetGroup.m_Targets[i].radius;
                jugadorEncontrado = true;
                break;
            }
        }
        
        // Si no encontramos al jugador, salimos
        if (!jugadorEncontrado)
            return;
            
        // Si el radio ya es el deseado, no hacemos nada
        if (Mathf.Approximately(radioActual, nuevoRadio))
            return;
        
        // Cancelamos cualquier corrutina anterior
        if (corrutinaCambioRadioJugador != null)
        {
            StopCoroutine(corrutinaCambioRadioJugador);
        }
        
        // Iniciamos la transición suave
        corrutinaCambioRadioJugador = StartCoroutine(CambiarRadioJugadorGradualmente(nuevoRadio));
    }

    /// Corrutina para cambiar el radio del jugador gradualmente
    private IEnumerator CambiarRadioJugadorGradualmente(float nuevoRadio)
    {
        transicionRadioEnProgreso = true;
        
        float radioInicial = 0f;
        bool jugadorEncontrado = false;
        
        // Obtenemos el radio inicial
        if (targetGroup != null && jugador != null)
        {
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == jugador)
                {
                    radioInicial = targetGroup.m_Targets[i].radius;
                    jugadorEncontrado = true;
                    break;
                }
            }
        }
        
        // Si no encontramos al jugador o se está destruyendo el componente, salimos
        if (!jugadorEncontrado || seEstaDestruyendo)
        {
            transicionRadioEnProgreso = false;
            corrutinaCambioRadioJugador = null;
            yield break;
        }
        
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoTransicionRadio)
        {
            // Verificamos en cada iteración si el componente sigue existiendo
            if (seEstaDestruyendo || targetGroup == null || jugador == null)
            {
                transicionRadioEnProgreso = false;
                corrutinaCambioRadioJugador = null;
                yield break;
            }
            
            tiempoTranscurrido += Time.deltaTime;
            float factorT = Mathf.Clamp01(tiempoTranscurrido / tiempoTransicionRadio);
            float factorSuavizado = AplicarSuavizado(factorT, TipoSuavizado.SuaveEntradaSalida);
            
            float radioActual = Mathf.Lerp(radioInicial, nuevoRadio, factorSuavizado);
            
            // Actualizamos el radio del jugador
            bool actualizado = false;
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == jugador)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.radius = radioActual;
                    targetGroup.m_Targets[i] = miembro;
                    actualizado = true;
                    break;
                }
            }
            
            // Si no pudimos actualizar el objetivo, salimos
            if (!actualizado)
            {
                transicionRadioEnProgreso = false;
                corrutinaCambioRadioJugador = null;
                yield break;
            }
            
            yield return null;
        }
        
        // Aseguramos que el radio final sea exacto
        if (targetGroup != null && jugador != null)
        {
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == jugador)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.radius = nuevoRadio;
                    targetGroup.m_Targets[i] = miembro;
                    break;
                }
            }
        }
        
        transicionRadioEnProgreso = false;
        corrutinaCambioRadioJugador = null;
    }

    
    public void AñadirObjetivo(Transform objetivo, float peso, float radio, 
                             float tiempoTransicion = -1f, TipoSuavizado tipoSuavizado = TipoSuavizado.SuaveEntradaSalida)
    {
        // Evitamos ejecución si el componente está siendo destruido o si no hay objetivo
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            return;
            
        // Si no se especifica tiempo, usamos el predeterminado
        if (tiempoTransicion <= 0f)
            tiempoTransicion = tiempoTransicionPredeterminado;
            
        // Si no existe, creamos una cola de transiciones pendientes
        if (!transicionesPendientes.ContainsKey(objetivo))
        {
            transicionesPendientes[objetivo] = new Queue<TransicionPendiente>();
        }

        // Creamos una nueva transición pendiente
        TransicionPendiente nuevaTransicion = new TransicionPendiente(true, peso, radio, tiempoTransicion, tipoSuavizado);
        
        // Si no hay transición activa, la ejecutamos directamente
        if (!corrutinasActivas.ContainsKey(objetivo))
        {
            EjecutarTransicion(objetivo, nuevaTransicion);
        }
        else
        {
            // Si ya hay una transición en progreso, la añadimos a la cola
            transicionesPendientes[objetivo].Enqueue(nuevaTransicion);
        }
    }


    private void EjecutarTransicion(Transform objetivo, TransicionPendiente transicion)
    {
        // Comprobamos que el componente no está siendo destruido y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            return;
            
        // Cancelamos cualquier corrutina anterior si existe
        if (corrutinasActivas.ContainsKey(objetivo))
        {
            if (corrutinasActivas[objetivo] != null)
                StopCoroutine(corrutinasActivas[objetivo]);
            corrutinasActivas.Remove(objetivo);
        }

        Coroutine corrutina = null;
        
        if (transicion.esAñadir)
        {
            // Verificamos si el objetivo ya está en el grupo
            if (objetosEnGrupo.ContainsKey(objetivo) && objetosEnGrupo[objetivo])
            {
                // Actualizamos los parámetros del objetivo existente
                corrutina = StartCoroutine(ActualizarObjetivoGradualmente(objetivo, transicion.peso, transicion.radio, 
                                                                        transicion.tiempoTransicion, transicion.tipoSuavizado));
            }
            else
            {
                // Añadimos un nuevo objetivo
                corrutina = StartCoroutine(AñadirObjetivoGradualmente(objetivo, transicion.peso, transicion.radio, 
                                                                    transicion.tiempoTransicion, transicion.tipoSuavizado));
            }
        }
        else
        {
            // Eliminamos un objetivo existente
            corrutina = StartCoroutine(EliminarObjetivoGradualmente(objetivo, transicion.tiempoTransicion, transicion.tipoSuavizado));
        }
        
        if (corrutina != null)
            corrutinasActivas[objetivo] = corrutina;
    }


    private void ProcesarSiguienteTransicion(Transform objetivo)
    {
        // Comprobamos que no estamos siendo destruidos y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null)
            return;
            
        if (transicionesPendientes.ContainsKey(objetivo) && transicionesPendientes[objetivo].Count > 0)
        {
            // Obtenemos la siguiente transición de la cola
            TransicionPendiente siguienteTransicion = transicionesPendientes[objetivo].Dequeue();
            
            // La ejecutamos
            EjecutarTransicion(objetivo, siguienteTransicion);
        }
        else if (corrutinasActivas.ContainsKey(objetivo))
        {
            // Si no hay más transiciones pendientes, eliminamos la entrada de corrutinas activas
            corrutinasActivas.Remove(objetivo);
        }
    }
    
    public void ActualizarObjetivo(Transform objetivo, float nuevoPeso, float nuevoRadio, 
                                 float tiempoTransicion = -1f, TipoSuavizado tipoSuavizado = TipoSuavizado.SuaveEntradaSalida)
    {
        // Comprobamos que no estamos siendo destruidos y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            return;
            
        // Si no se especifica tiempo, actualizamos inmediatamente
        if (tiempoTransicion <= 0f)
        {
            bool actualizado = false;
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == objetivo)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.weight = nuevoPeso;
                    miembro.radius = nuevoRadio;
                    targetGroup.m_Targets[i] = miembro;
                    actualizado = true;
                    break;
                }
            }
            
            // Si no pudimos actualizar, quizás el objetivo no está en el grupo
            if (!actualizado && objetivo != null)
            {
                AñadirObjetivo(objetivo, nuevoPeso, nuevoRadio, 0f);
            }
        }
        else
        {
            // Creamos una nueva transición pendiente
            TransicionPendiente nuevaTransicion = new TransicionPendiente(true, nuevoPeso, nuevoRadio, tiempoTransicion, tipoSuavizado);
            
            // Si no hay transición activa, la ejecutamos directamente
            if (!corrutinasActivas.ContainsKey(objetivo))
            {
                EjecutarTransicion(objetivo, nuevaTransicion);
            }
            else
            {
                // Si no existe, creamos una cola de transiciones pendientes
                if (!transicionesPendientes.ContainsKey(objetivo))
                {
                    transicionesPendientes[objetivo] = new Queue<TransicionPendiente>();
                }
                
                // Añadimos a la cola
                transicionesPendientes[objetivo].Enqueue(nuevaTransicion);
            }
        }
    }

    public void EliminarObjetivo(Transform objetivo, float tiempoTransicion = -1f, 
                               TipoSuavizado tipoSuavizado = TipoSuavizado.SuaveEntradaSalida)
    {
        // Comprobamos que no estamos siendo destruidos y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            return;
            
        // Si no se especifica tiempo, usamos el predeterminado
        if (tiempoTransicion <= 0f)
            tiempoTransicion = tiempoTransicionPredeterminado;
            
        // No permitimos eliminar al jugador
        if (objetivo == jugador)
            return;
            
        // Verificamos si el objetivo realmente está en el grupo
        if (!objetosEnGrupo.ContainsKey(objetivo) || !objetosEnGrupo[objetivo])
            return;

        // Creamos una nueva transición pendiente para eliminación
        TransicionPendiente nuevaTransicion = new TransicionPendiente(false, 0f, 0f, tiempoTransicion, tipoSuavizado);
        
        // Si no hay transición activa, la ejecutamos directamente
        if (!corrutinasActivas.ContainsKey(objetivo))
        {
            EjecutarTransicion(objetivo, nuevaTransicion);
        }
        else
        {
            // Si no existe, creamos una cola de transiciones pendientes
            if (!transicionesPendientes.ContainsKey(objetivo))
            {
                transicionesPendientes[objetivo] = new Queue<TransicionPendiente>();
            }
            
            // Añadimos a la cola
            transicionesPendientes[objetivo].Enqueue(nuevaTransicion);
        }
    }

    private float AplicarSuavizado(float t, TipoSuavizado tipoSuavizado)
    {
        switch (tipoSuavizado)
        {
            case TipoSuavizado.Lineal:
                return t;
            case TipoSuavizado.SuaveEntrada:
                return t * t; // Ease In (cuadrático)
            case TipoSuavizado.SuaveSalida:
                return 1 - (1 - t) * (1 - t); // Ease Out (cuadrático)
            case TipoSuavizado.SuaveEntradaSalida:
                return t < 0.5f ? 2 * t * t : 1 - Mathf.Pow(-2 * t + 2, 2) / 2; // Ease In-Out (cuadrático)
            default:
                return t;
        }
    }

    private IEnumerator AñadirObjetivoGradualmente(Transform objetivo, float pesoFinal, float radio, 
                                                 float tiempoTransicion, TipoSuavizado tipoSuavizado)
    {
        // Comprobamos que no estamos siendo destruidos y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            yield break;
            
        // Añadimos el objetivo con peso 0 inicialmente
        targetGroup.AddMember(objetivo, 0f, radio);
        objetosEnGrupo[objetivo] = true;

        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoTransicion)
        {
            // Verificamos en cada iteración si el componente sigue existiendo
            if (seEstaDestruyendo || objetivo == null || targetGroup == null)
                yield break;
                
            tiempoTranscurrido += Time.deltaTime;
            float factorT = Mathf.Clamp01(tiempoTranscurrido / tiempoTransicion);
            float factorSuavizado = AplicarSuavizado(factorT, tipoSuavizado);
            float pesoActual = Mathf.Lerp(0f, pesoFinal, factorSuavizado);
            
            // Actualizamos el peso del objetivo
            bool actualizado = false;
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == objetivo)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.weight = pesoActual;
                    targetGroup.m_Targets[i] = miembro;
                    actualizado = true;
                    break;
                }
            }
            
            // Si no pudimos actualizar el objetivo, salimos
            if (!actualizado)
                yield break;
                
            yield return null;
        }

        // Aseguramos que el peso final sea exacto
        if (!seEstaDestruyendo && objetivo != null && targetGroup != null)
        {
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == objetivo)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.weight = pesoFinal;
                    targetGroup.m_Targets[i] = miembro;
                    break;
                }
            }
        }

        // Procesamos la siguiente transición pendiente si existe
        if (!seEstaDestruyendo && objetivo != null)
            ProcesarSiguienteTransicion(objetivo);
    }


    private IEnumerator ActualizarObjetivoGradualmente(Transform objetivo, float nuevoPeso, float nuevoRadio, 
                                                     float tiempoTransicion, TipoSuavizado tipoSuavizado)
    {
        // Comprobamos que no estamos siendo destruidos y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            yield break;
            
        float pesoInicial = 0f;
        float radioInicial = 0f;
        bool objetivoEncontrado = false;
        
        // Obtenemos el peso y radio iniciales del objetivo
        for (int i = 0; i < targetGroup.m_Targets.Length; i++)
        {
            if (targetGroup.m_Targets[i].target == objetivo)
            {
                pesoInicial = targetGroup.m_Targets[i].weight;
                radioInicial = targetGroup.m_Targets[i].radius;
                objetivoEncontrado = true;
                break;
            }
        }
        
        // Si no encontramos el objetivo, salimos
        if (!objetivoEncontrado)
            yield break;

        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoTransicion)
        {
            // Verificamos en cada iteración si el componente sigue existiendo
            if (seEstaDestruyendo || objetivo == null || targetGroup == null)
                yield break;
                
            tiempoTranscurrido += Time.deltaTime;
            float factorT = Mathf.Clamp01(tiempoTranscurrido / tiempoTransicion);
            float factorSuavizado = AplicarSuavizado(factorT, tipoSuavizado);
            
            float pesoActual = Mathf.Lerp(pesoInicial, nuevoPeso, factorSuavizado);
            float radioActual = Mathf.Lerp(radioInicial, nuevoRadio, factorSuavizado);
            
            // Actualizamos el peso y radio del objetivo
            bool actualizado = false;
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == objetivo)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.weight = pesoActual;
                    miembro.radius = radioActual;
                    targetGroup.m_Targets[i] = miembro;
                    actualizado = true;
                    break;
                }
            }
            
            // Si no pudimos actualizar el objetivo, salimos
            if (!actualizado)
                yield break;
                
            yield return null;
        }

        // Aseguramos que los valores finales sean exactos
        if (!seEstaDestruyendo && objetivo != null && targetGroup != null)
        {
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == objetivo)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.weight = nuevoPeso;
                    miembro.radius = nuevoRadio;
                    targetGroup.m_Targets[i] = miembro;
                    break;
                }
            }
        }

        // Procesamos la siguiente transición pendiente si existe
        if (!seEstaDestruyendo && objetivo != null)
            ProcesarSiguienteTransicion(objetivo);
    }

    private IEnumerator EliminarObjetivoGradualmente(Transform objetivo, float tiempoTransicion, TipoSuavizado tipoSuavizado)
    {
        // Comprobamos que no estamos siendo destruidos y que el objetivo existe
        if (seEstaDestruyendo || objetivo == null || targetGroup == null)
            yield break;
            
        float pesoInicial = 0f;
        bool objetivoEncontrado = false;
        
        // Obtenemos el peso inicial del objetivo
        for (int i = 0; i < targetGroup.m_Targets.Length; i++)
        {
            if (targetGroup.m_Targets[i].target == objetivo)
            {
                pesoInicial = targetGroup.m_Targets[i].weight;
                objetivoEncontrado = true;
                break;
            }
        }
        
        // Si no encontramos el objetivo, salimos
        if (!objetivoEncontrado)
            yield break;

        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoTransicion)
        {
            // Verificamos en cada iteración si el componente sigue existiendo
            if (seEstaDestruyendo || objetivo == null || targetGroup == null)
                yield break;
                
            tiempoTranscurrido += Time.deltaTime;
            float factorT = Mathf.Clamp01(tiempoTranscurrido / tiempoTransicion);
            float factorSuavizado = AplicarSuavizado(factorT, tipoSuavizado);
            float pesoActual = Mathf.Lerp(pesoInicial, 0f, factorSuavizado);
            
            // Actualizamos el peso del objetivo
            bool actualizado = false;
            for (int i = 0; i < targetGroup.m_Targets.Length; i++)
            {
                if (targetGroup.m_Targets[i].target == objetivo)
                {
                    var miembro = targetGroup.m_Targets[i];
                    miembro.weight = pesoActual;
                    targetGroup.m_Targets[i] = miembro;
                    actualizado = true;
                    break;
                }
            }
            
            // Si no pudimos actualizar el objetivo, salimos
            if (!actualizado)
                yield break;
                
            yield return null;
        }

        // Eliminamos completamente al objetivo del grupo si todavía existe
        if (!seEstaDestruyendo && objetivo != null && targetGroup != null)
        {
            targetGroup.RemoveMember(objetivo);
            objetosEnGrupo[objetivo] = false;
        }

        // Procesamos la siguiente transición pendiente si existe
        if (!seEstaDestruyendo && objetivo != null)
            ProcesarSiguienteTransicion(objetivo);
    }
}