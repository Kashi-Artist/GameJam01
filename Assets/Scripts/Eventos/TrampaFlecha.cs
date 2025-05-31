using System.Collections;
using UnityEngine;

public class TrampaFlecha : MonoBehaviour
{
    [Header("Referencias")]
    public Transform propulsor;                       // Punto desde donde sale la flecha
    public GameObject prefabFlecha;                   // Prefab de la flecha
    
    [Header("Configuración de la Flecha")]
    public float velocidadFlecha = 15f;               // Velocidad de la flecha
    public float escalaMinima = 0.3f;                 // Escala mínima de la flecha (al inicio)
    public float escalaMaxima = 1.5f;                 // Escala máxima de la flecha (al llegar al jugador)
    public float fuerzaImpacto = 20f;                 // Fuerza aplicada al jugador al impactar
    
    [Header("Configuración de Activación")]
    public float tiempoEsperaAntes = 0.5f;            // Tiempo antes de disparar la flecha
    public bool puedeReactivarse = false;             // Si la trampa puede activarse múltiples veces
    public float tiempoEsperaReactivacion = 3f;       // Tiempo antes de poder reactivarse
    
    [Header("Efectos")]
    public ParticleSystem efectoDeteccion;            // Efecto al detectar al jugador
    public ParticleSystem efectoDisparo;              // Efecto al disparar desde el propulsor
    public AudioClip sonidoDeteccion;                 // Sonido al detectar al jugador
    public AudioClip sonidoDisparo;                   // Sonido al disparar la flecha
    public AudioClip sonidoImpacto;                   // Sonido al impactar al jugador
    
    // Variables privadas
    private bool trampaActivada = false;
    private bool jugadorEnRango = false;
    private Transform jugadorTransform;
    private bool enCooldown = false;
    
    private void Start()
    {
        // Validar referencias
        if (propulsor == null)
        {
            Debug.LogError("TrampaFlecha: Falta asignar el propulsor!");
            return;
        }
        
        if (prefabFlecha == null)
        {
            Debug.LogError("TrampaFlecha: Falta asignar el prefab de la flecha!");
            return;
        }
        
        // Detener efectos al inicio
        DetenerEfectos();
    }
    
    private void Update()
    {
        // Solo activar si hay un jugador en rango, no está en cooldown y no está invisible
        if (!jugadorEnRango || enCooldown ) 
            return;
        
        // Verificar el jugador
        if ( !trampaActivada)
        {
            ActivarTrampa();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Guardar referencias
        jugadorTransform = other.transform;
        jugadorEnRango = true;
        
        // Verificar inmediatamente si el jugador está visible
        if (!trampaActivada && !enCooldown)
        {
            ActivarTrampa();
        }
        else if (!enCooldown)
        {
            // Activar efecto de detección (jugador invisible)
            ActivarEfectoDeteccion();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        jugadorEnRango = false;
        jugadorTransform = null;
        
        // Detener efecto de detección si no está activada
        if (!trampaActivada)
        {
            DetenerEfectos();
        }
    }
    
    private void ActivarTrampa()
    {
        if (trampaActivada || enCooldown) return;
        
        trampaActivada = true;
        
        // Detener efecto de detección
        DetenerEfectos();
        
        // Iniciar secuencia de disparo
        StartCoroutine(SecuenciaDisparo());
    }
    
    private IEnumerator SecuenciaDisparo()
    {
        // Reproducir sonido de detección
        ReproducirSonido(sonidoDeteccion);
        
        // Esperar antes de disparar
        yield return new WaitForSeconds(tiempoEsperaAntes);
        
        // Disparar la flecha
        DispararFlecha();
        
        // Configurar cooldown si es necesario
        if (puedeReactivarse)
        {
            yield return new WaitForSeconds(tiempoEsperaReactivacion);
            enCooldown = false;
            trampaActivada = false;
        }
        else
        {
            enCooldown = true; // Trampa permanentemente desactivada
        }
    }
    
    private void DispararFlecha()
    {
        if (jugadorTransform == null || propulsor == null || prefabFlecha == null) return;
        
        // Crear la flecha
        GameObject flecha = Instantiate(prefabFlecha, propulsor.position, Quaternion.identity);
        
        // Configurar escala inicial
        flecha.transform.localScale = Vector3.one * escalaMinima;
        
        // Activar efecto de disparo
        if (efectoDisparo != null)
        {
            efectoDisparo.transform.position = propulsor.position;
            efectoDisparo.Play();
        }
        
        // Reproducir sonido de disparo
        ReproducirSonido(sonidoDisparo);
        
        // Iniciar movimiento de la flecha
        StartCoroutine(MoverFlecha(flecha));
    }
    
    private IEnumerator MoverFlecha(GameObject flecha)
    {
        Vector3 posicionInicial = flecha.transform.position;
        Vector3 posicionObjetivo = jugadorTransform.position;
        
        // Calcular dirección y rotación
        Vector3 direccion = Vector3.down; // Siempre hacia abajo
        
        float distanciaTotal = Vector3.Distance(posicionInicial, posicionObjetivo);
        float tiempoViaje = distanciaTotal / velocidadFlecha;
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoViaje)
        {
            // Actualizar posición objetivo (seguir al jugador)
            if (jugadorTransform != null)
            {
                posicionObjetivo = jugadorTransform.position;
                direccion = (posicionObjetivo - flecha.transform.position).normalized;
                
            }
            
            // Mover la flecha
            flecha.transform.position += direccion * velocidadFlecha * Time.deltaTime;
            
            // Escalar la flecha según la proximidad al jugador
            float distanciaActual = Vector3.Distance(flecha.transform.position, posicionObjetivo);
            float progreso = 1f - (distanciaActual / distanciaTotal);
            float escalaActual = Mathf.Lerp(escalaMinima, escalaMaxima, progreso);
            flecha.transform.localScale = Vector3.one * escalaActual;
            
            // Verificar si llegó al jugador
            if (distanciaActual <= 0.5f)
            {
                ImpactarJugador(direccion);
                break;
            }
            
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        
        // Destruir la flecha
        if (flecha != null)
        {
            Destroy(flecha);
        }
    }
    
    private void ImpactarJugador(Vector3 direccionFlecha)
    {
        if (jugadorTransform == null) return;
        
        // Reproducir sonido de impacto
        ReproducirSonido(sonidoImpacto);
        
        // Aplicar fuerza al jugador
        Rigidbody2D jugadorRb = jugadorTransform.GetComponent<Rigidbody2D>();
        if (jugadorRb != null)
        {
            jugadorRb.AddForce(direccionFlecha * fuerzaImpacto, ForceMode2D.Impulse);
        }
        
        // También puedes añadir daño aquí si es necesario
        // SistemaVida vida = jugadorTransform.GetComponent<SistemaVida>();
        // if (vida != null) vida.RecibirDaño(dañoFlecha);
    }
    
    private void ActivarEfectoDeteccion()
    {
        if (efectoDeteccion != null)
        {
            efectoDeteccion.Play();
        }
    }
    
    private void DetenerEfectos()
    {
        if (efectoDeteccion != null)
        {
            efectoDeteccion.Stop();
        }
    }
    
    private void ReproducirSonido(AudioClip sonido)
    {
        if (sonido != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(sonido, Camera.main.transform.position);
        }
    }
    
    // Método público para activar manualmente
    public void ActivarTrampaManualmente()
    {
        if (!trampaActivada && !enCooldown)
        {
            ActivarTrampa();
        }
    }
    
    // Para visualizar el área de detección en el editor
    private void OnDrawGizmos()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Naranja semitransparente
            
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = collider as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            else if (collider is CircleCollider2D)
            {
                CircleCollider2D circleCollider = collider as CircleCollider2D;
                Gizmos.DrawSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
        
        // Mostrar línea desde propulsor
        if (propulsor != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(propulsor.position, 0.2f);
        }
    }
}