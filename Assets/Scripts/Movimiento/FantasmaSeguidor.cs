using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FantasmaSeguidor : MonoBehaviour
{
    [Header("Configuración de Seguimiento")]
    public Transform jugador; // Referencia al transform del jugador
    public float velocidadSeguimiento = 2f; // Velocidad del fantasma
    public float distanciaMinima = 1.5f; // Distancia mínima al jugador
    public float distanciaMaxima = 15f; // Distancia máxima antes de teletransportarse (aumentada)
    
    [Header("Configuración de Movimiento")]
    public bool movimientoSuave = true; // Usar interpolación suave
    public float suavidadMovimiento = 2f; // Qué tan suave es el movimiento (reducido para más suavidad)
    [Header("Configuración de Aceleración")]
    public float velocidadInicial = 0.1f; // Velocidad mínima cuando está lejos
    public float velocidadMaxima = 3f; // Velocidad máxima cuando está cerca
    public float distanciaAceleracion = 6f; // Distancia a la que empieza a acelerar
    public AnimationCurve curvaAceleracion = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // Curva de aceleración
    [Header("Configuración de Transición")]
    public float intensidadSeguimiento = 0.3f; // Qué tan fuerte es el seguimiento (0-1)
    public float tiempoReaccion = 1.5f; // Tiempo antes de empezar a seguir al salir de zona mínima
    public float suavidadTransicion = 2f; // Qué tan suave es la transición entre comportamientos
    [Header("Configuración de Flotación")]
    public bool flotarVerticalmente = true; // Efecto de flotación
    public float amplitudFlotacion = 0.3f; // Qué tanto flota arriba/abajo
    public float velocidadFlotacion = 2f; // Velocidad de la flotación
    
    [Header("Configuración Visual")]
    public bool voltearSegunDireccion = true; // Voltear sprite según dirección
    public bool desvanecerConDistancia = true; // Cambiar alpha según distancia
    public float alphaMinimo = 0.3f; // Alpha mínimo cuando está lejos
    public float alphaMaximo = 1f; // Alpha máximo cuando está cerca
    
    [Header("Configuración de Obstáculos")]
    public LayerMask capasObstaculos; // Capas que bloquean al fantasma
    public bool evitarObstaculos = false; // Si debe evitar obstáculos
    public float radioDeteccion = 0.5f; // Radio para detectar obstáculos
    
    // Variables privadas
    private Vector3 posicionObjetivo;
    private Vector3 posicionBaseMovimiento; // Nueva: posición base sin flotación
    private float tiempoFlotacion;
    private SpriteRenderer renderizadorSprite;
    private bool jugadorEncontrado = false;
    private Vector3 ultimaDireccion;
    private float tiempoUltimoTeletransporte; // Nuevo: evitar teletransportes frecuentes
    private const float COOLDOWN_TELETRANSPORTE = 2f; // 2 segundos entre teletransportes
    private float velocidadActual; // Velocidad actual calculada dinámicamente
    
    // Variables para transición suave
    private float tiempoSalidaZonaMinima; // Cuándo salió de la zona mínima
    private bool estabaEnZonaMinima = false; // Si estaba en zona mínima el frame anterior
    private Vector3 objetivoSuavizado; // Objetivo con transición suave
    private float factorSeguimientoActual; // Factor actual de seguimiento (0-1)
    
    void Start()
    {
        // Buscar al jugador automáticamente si no está asignado
        if (jugador == null)
        {
            BuscarJugador();
        }
        
        // Obtener componentes
        renderizadorSprite = GetComponent<SpriteRenderer>();
        if (renderizadorSprite == null)
        {
            renderizadorSprite = GetComponentInChildren<SpriteRenderer>();
        }
        
        // Configurar posición inicial
        posicionBaseMovimiento = transform.position;
        posicionObjetivo = transform.position;
        objetivoSuavizado = transform.position;
        tiempoFlotacion = 0f;
        ultimaDireccion = Vector3.right;
        tiempoUltimoTeletransporte = 0f;
        velocidadActual = velocidadInicial;
        tiempoSalidaZonaMinima = 0f;
        factorSeguimientoActual = 0f;
        
        if (jugador != null)
        {
            jugadorEncontrado = true;
        }
    }
    
    void Update()
    {
        if (!jugadorEncontrado || jugador == null)
        {
            BuscarJugador();
            return;
        }
        
        ActualizarPosicionObjetivo();
        ActualizarTransicionSuave();
        MoverFantasma();
        AplicarEfectoFlotacion();
        ActualizarAparienciaVisual();
    }
    
    private void BuscarJugador()
    {
        // Buscar por tag
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            jugadorEncontrado = true;
            Debug.Log("Fantasma: Jugador encontrado por tag 'Player'");
            return;
        }
        
        // Buscar por nombre común
        jugadorObj = GameObject.Find("Jugador");
        if (jugadorObj == null) jugadorObj = GameObject.Find("Player");
        if (jugadorObj == null) jugadorObj = GameObject.Find("Character");
        
        if (jugadorObj != null)
        {
            jugador = jugadorObj.transform;
            jugadorEncontrado = true;
            Debug.Log($"Fantasma: Jugador encontrado por nombre '{jugadorObj.name}'");
        }
        else
        {
            Debug.LogWarning("Fantasma: No se puede encontrar al jugador. Asigna manualmente la referencia 'jugador' en el inspector.");
        }
    }
    
    private void ActualizarPosicionObjetivo()
    {
        float distanciaAlJugador = Vector3.Distance(posicionBaseMovimiento, jugador.position);
        
        // Solo teletransportarse si está MUY lejos y ha pasado suficiente tiempo
        if (distanciaAlJugador > distanciaMaxima && Time.time - tiempoUltimoTeletransporte > COOLDOWN_TELETRANSPORTE)
        {
            Vector2 offset2D = Random.insideUnitCircle.normalized * distanciaMinima;
            Vector3 offset3D = new Vector3(offset2D.x, offset2D.y, 0);

            Vector3 posicionTeletransporte = jugador.position + offset3D;
            posicionTeletransporte.z = transform.position.z;

            posicionBaseMovimiento = posicionTeletransporte;
            objetivoSuavizado = posicionTeletransporte;
            tiempoUltimoTeletransporte = Time.time;
            
            Debug.Log($"Fantasma teletransportado - Distancia era: {distanciaAlJugador:F1}");
            return;
        }
        
        // Detectar si está en zona mínima
        bool estaEnZonaMinima = distanciaAlJugador < distanciaMinima;
        
        // Detectar cuando sale de la zona mínima
        if (estabaEnZonaMinima && !estaEnZonaMinima)
        {
            tiempoSalidaZonaMinima = Time.time;
        }
        
        estabaEnZonaMinima = estaEnZonaMinima;
        
        // ZONA DE CONFORT AMPLIADA con comportamiento más suave
        float zonaConfortMinima = distanciaMinima * 0.7f;
        float zonaConfortMaxima = distanciaMinima * 1.8f; // Zona más amplia
        
        if (distanciaAlJugador < zonaConfortMinima)
        {
            // Está MUY cerca, alejarse gradualmente
            Vector3 direccionAlejarse = (posicionBaseMovimiento - jugador.position).normalized;
            posicionObjetivo = jugador.position + (direccionAlejarse * distanciaMinima);
            factorSeguimientoActual = 0.2f; // Seguimiento muy bajo cuando se aleja
        }
        else if (distanciaAlJugador > zonaConfortMaxima)
        {
            // Está lejos, pero verificar tiempo de reacción
            float tiempoDesdeReaccion = Time.time - tiempoSalidaZonaMinima;
            
            if (tiempoDesdeReaccion > tiempoReaccion || !estabaEnZonaMinima)
            {
                // Ya pasó el tiempo de reacción O nunca estuvo en zona mínima
                posicionObjetivo = jugador.position;
                factorSeguimientoActual = intensidadSeguimiento;
            }
            else
            {
                // Aún en período de reacción, mantener posición actual
                posicionObjetivo = posicionBaseMovimiento;
                factorSeguimientoActual = 0.05f; // Casi sin movimiento
            }
        }
        else
        {
            // ZONA DE CONFORT: mantener distancia con movimiento muy sutil
            Vector3 direccionHaciaJugador = (jugador.position - posicionBaseMovimiento).normalized;
            float distanciaIdeal = distanciaMinima * 1.1f; // Un poco más lejos que el mínimo
            Vector3 posicionIdeal = jugador.position - (direccionHaciaJugador * distanciaIdeal);
            
            posicionObjetivo = posicionIdeal;
            factorSeguimientoActual = 0.1f; // Seguimiento muy sutil
        }
        
        // Mantener la misma coordenada Z
        posicionObjetivo.z = posicionBaseMovimiento.z;
    }
    
    private void ActualizarTransicionSuave()
    {
        // Suavizar el objetivo con interpolación lenta
        float velocidadTransicion = suavidadTransicion * Time.deltaTime;
        objetivoSuavizado = Vector3.Lerp(objetivoSuavizado, posicionObjetivo, velocidadTransicion);
    }
    
    private void MoverFantasma()
    {
        Vector3 direccionMovimiento = (objetivoSuavizado - posicionBaseMovimiento).normalized;
        float distanciaObjetivo = Vector3.Distance(posicionBaseMovimiento, objetivoSuavizado);
        float distanciaAlJugador = Vector3.Distance(posicionBaseMovimiento, jugador.position);
        
        // Calcular velocidad basada en la distancia al jugador
        CalcularVelocidadPorDistancia(distanciaAlJugador);
        
        // Aplicar factor de seguimiento para reducir intensidad
        float velocidadConIntensidad = velocidadActual * factorSeguimientoActual;
        
        // Solo moverse si hay una distancia significativa
        if (distanciaObjetivo > 0.05f) // Umbral más bajo para movimientos más sutiles
        {
            if (evitarObstaculos)
            {
                direccionMovimiento = EvitarObstaculos(direccionMovimiento);
            }
            
            Vector3 nuevaPosicion;
            
            if (movimientoSuave)
            {
                // Movimiento suave con Lerp usando velocidad con intensidad reducida
                float velocidadAjustada = velocidadConIntensidad * Time.deltaTime;
                nuevaPosicion = Vector3.Lerp(posicionBaseMovimiento, objetivoSuavizado, velocidadAjustada);
            }
            else
            {
                // Movimiento directo con velocidad con intensidad reducida
                nuevaPosicion = posicionBaseMovimiento + (direccionMovimiento * velocidadConIntensidad * Time.deltaTime);
            }
            
            // Actualizar la posición base del movimiento
            posicionBaseMovimiento = nuevaPosicion;
            
            // Actualizar dirección solo si se movió significativamente
            if (direccionMovimiento.magnitude > 0.05f)
            {
                ultimaDireccion = direccionMovimiento;
            }
        }
    }
    
    private void CalcularVelocidadPorDistancia(float distanciaAlJugador)
    {
        // Si está muy cerca, velocidad mínima
        if (distanciaAlJugador <= distanciaMinima)
        {
            velocidadActual = velocidadInicial;
            return;
        }
        
        // Si está más lejos que la distancia de aceleración, usar velocidad inicial
        if (distanciaAlJugador >= distanciaAceleracion)
        {
            velocidadActual = velocidadInicial;
            return;
        }
        
        // Calcular el porcentaje de aceleración basado en la distancia
        float porcentajeDistancia = (distanciaAceleracion - distanciaAlJugador) / (distanciaAceleracion - distanciaMinima);
        porcentajeDistancia = Mathf.Clamp01(porcentajeDistancia);
        
        // Aplicar la curva de aceleración
        float factorCurva = curvaAceleracion.Evaluate(porcentajeDistancia);
        
        // Interpolar entre velocidad inicial y máxima
        velocidadActual = Mathf.Lerp(velocidadInicial, velocidadMaxima, factorCurva);
        
        // Debug para ajustar valores (comentar en build final)
        // Debug.Log($"Distancia: {distanciaAlJugador:F2}, Velocidad: {velocidadActual:F3}, Factor: {factorCurva:F2}, Intensidad: {factorSeguimientoActual:F2}");
    }
    
    private Vector3 EvitarObstaculos(Vector3 direccionOriginal)
    {
        // Raycast para detectar obstáculos
        RaycastHit2D hit = Physics2D.CircleCast(posicionBaseMovimiento, radioDeteccion, 
            direccionOriginal, velocidadActual * Time.deltaTime, capasObstaculos);
            
        if (hit.collider != null)
        {
            // Calcular dirección alternativa
            Vector3 direccionAlternativa = Vector3.Reflect(direccionOriginal, hit.normal);
            return direccionAlternativa.normalized;
        }
        
        return direccionOriginal;
    }
    
    private void AplicarEfectoFlotacion()
    {
        if (!flotarVerticalmente)
        {
            // Si no hay flotación, usar directamente la posición base
            transform.position = posicionBaseMovimiento;
            return;
        }
        
        tiempoFlotacion += Time.deltaTime * velocidadFlotacion;
        float offsetFlotacion = Mathf.Sin(tiempoFlotacion) * amplitudFlotacion;
        
        // CORREGIDO: Aplicar flotación sobre la posición base de movimiento
        Vector3 posicionFinal = posicionBaseMovimiento;
        posicionFinal.y += offsetFlotacion;
        
        transform.position = posicionFinal;
    }
    
    private void ActualizarAparienciaVisual()
    {
        if (renderizadorSprite == null) return;
        
        // Voltear sprite según dirección
        if (voltearSegunDireccion && ultimaDireccion != Vector3.zero)
        {
            if (ultimaDireccion.x < 0)
            {
                renderizadorSprite.flipX = true;
            }
            else if (ultimaDireccion.x > 0)
            {
                renderizadorSprite.flipX = false;
            }
        }
        
        // Cambiar transparencia según distancia
        if (desvanecerConDistancia && jugador != null)
        {
            float distancia = Vector3.Distance(transform.position, jugador.position);
            float porcentajeDistancia = Mathf.Clamp01(distancia / distanciaMaxima);
            
            // Invertir el porcentaje para que sea más opaco cuando está cerca
            float alpha = Mathf.Lerp(alphaMaximo, alphaMinimo, porcentajeDistancia);
            
            Color colorActual = renderizadorSprite.color;
            colorActual.a = alpha;
            renderizadorSprite.color = colorActual;
        }
    }
    
    // Método público para cambiar el objetivo temporalmente
    public void EstablecerObjetivoTemporal(Vector3 posicion, float duracion = 0f)
    {
        StartCoroutine(SeguirObjetivoTemporal(posicion, duracion));
    }
    
    private IEnumerator SeguirObjetivoTemporal(Vector3 objetivo, float duracion)
    {
        Transform jugadorOriginal = jugador;
        
        // Crear un transform temporal
        GameObject objetivoTemporal = new GameObject("ObjetivoTemporal");
        objetivoTemporal.transform.position = objetivo;
        jugador = objetivoTemporal.transform;
        
        if (duracion > 0)
        {
            yield return new WaitForSeconds(duracion);
        }
        else
        {
            // Esperar hasta que llegue al objetivo
            while (Vector3.Distance(transform.position, objetivo) > 0.5f)
            {
                yield return null;
            }
        }
        
        // Restaurar jugador original
        jugador = jugadorOriginal;
        Destroy(objetivoTemporal);
    }
    
    // Métodos públicos para control externo
    public void PausarSeguimiento()
    {
        jugadorEncontrado = false;
    }
    
    public void ReanudarSeguimiento()
    {
        jugadorEncontrado = true;
    }
    
    public void TeletransportarAJugador()
    {
        if (jugador != null)
        {
            Vector3 offset = new Vector3(
                Random.insideUnitCircle.normalized.x,
                Random.insideUnitCircle.normalized.y,
                0
            ) * distanciaMinima;

            Vector3 posicionTeletransporte = jugador.position + offset;
            posicionTeletransporte.z = transform.position.z;

            posicionBaseMovimiento = posicionTeletransporte;
            tiempoUltimoTeletransporte = Time.time;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        // Dibujar rangos de distancia en el editor
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, distanciaMinima);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaMaxima);
        
        if (evitarObstaculos)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radioDeteccion);
        }
        
        // Mostrar posición base sin flotación
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(posicionBaseMovimiento, Vector3.one * 0.2f);
    }
}