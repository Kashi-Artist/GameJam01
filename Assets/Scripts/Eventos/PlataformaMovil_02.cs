using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlataformaMovil_02 : MonoBehaviour
{
    [Header("Detección")]
    public LayerMask capaJugador;
    public float distanciaDeteccion = 0.1f;
    
    [Header("Movimiento")]
    public float velocidadMovimiento = 3f;
    public Transform[] puntosRuta;
    public bool moverSoloCuandoHayJugador = true;
    public bool activadoPorPocion = false;
    public float tiempoEsperaEntrePuntos = 0.5f;
    
    [Header("Retorno")]
    public float tiempoEsperaParaRetorno = 2f;
    public int puntoRetornoSinJugador = 0;
    
    [Header("Desactivación")]
    public int puntoDestinoAlDesactivar = 0; // Nuevo punto destino al desactivar
    public bool irAPuntoAlDesactivar = true; // Control para habilitar/deshabilitar esta funcionalidad
    
    private int puntoActual = 0;
    [HideInInspector] public bool estaMoviendose = true;
    private bool jugadorEncima = false;
    private Vector3 ultimaPosicion;
    private List<Rigidbody2D> cuerposJugadoresEncima = new List<Rigidbody2D>();
    private Coroutine rutinaTiempoEspera = null;
    private Coroutine rutinaEsperaEnPunto = null;
    private bool retornandoAPunto = false;
    private bool detenidoPorDetector = false;
    private bool estadoMovimientoAnterior = true;
    
    private void Start()
    {
        // Guardar posición inicial para calcular desplazamiento
        ultimaPosicion = transform.position;
        
        // Si requiere activación por poción, desactivar inicialmente
        if (activadoPorPocion)
        {
            estaMoviendose = false;
        }
        
        // Guardar estado inicial
        estadoMovimientoAnterior = estaMoviendose;
    }
    
    private void FixedUpdate()
    {
        // Detectar jugadores encima de la plataforma
        DetectarJugadores();
        
        // Actualizar movimiento
        MoverPlataforma();
        
        // Guardar posición para el próximo frame
        ultimaPosicion = transform.position;
    }
    
    private void DetectarJugadores()
    {
        cuerposJugadoresEncima.Clear();

        // Suponiendo que tienes un BoxCollider2D en el mismo GameObject
        BoxCollider2D colPlataforma = GetComponent<BoxCollider2D>();
        Vector2 centro = (Vector2)transform.position + colPlataforma.offset + Vector2.up * (colPlataforma.size.y / 2 + distanciaDeteccion / 2);
        Vector2 tamano = new Vector2(
            colPlataforma.size.x * colPlataforma.transform.lossyScale.x,
            distanciaDeteccion
        );

        Collider2D[] colisionadores = Physics2D.OverlapBoxAll(
            centro,
            tamano,
            0f,
            capaJugador
        );

        bool jugadorDetectado = false;

        foreach (Collider2D col in colisionadores)
        {
            Rigidbody2D rb = col.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                cuerposJugadoresEncima.Add(rb);
                jugadorDetectado = true;
            }
        }

        // Manejo del estado como ya lo tienes
        if (jugadorEncima != jugadorDetectado)
        {
            jugadorEncima = jugadorDetectado;

            if (!jugadorEncima && estaMoviendose && !retornandoAPunto && !detenidoPorDetector)
            {
                if (rutinaEsperaEnPunto != null)
                {
                    StopCoroutine(rutinaEsperaEnPunto);
                    rutinaEsperaEnPunto = null;
                }

                if (rutinaTiempoEspera != null)
                {
                    StopCoroutine(rutinaTiempoEspera);
                }

                rutinaTiempoEspera = StartCoroutine(EsperarAntesDeRetornar());
            }
            else if (jugadorEncima && rutinaTiempoEspera != null)
            {
                StopCoroutine(rutinaTiempoEspera);
                rutinaTiempoEspera = null;
                retornandoAPunto = false;
            }
        }
    }

    private void MoverPlataforma()
    {
        // No hacer nada si no tiene puntos de ruta o no debe moverse
        if (puntosRuta.Length == 0 || !estaMoviendose) return;
        
        // Si la plataforma solo debe moverse con jugador y no hay nadie
        // PERO permitir movimiento si está retornando a un punto
        if (moverSoloCuandoHayJugador && !jugadorEncima && !retornandoAPunto) return;
        
        // Punto objetivo actual
        Transform puntoObjetivo = puntosRuta[puntoActual];
        
        // Mover hacia el punto objetivo
        transform.position = Vector3.MoveTowards(
            transform.position, 
            puntoObjetivo.position, 
            velocidadMovimiento * Time.fixedDeltaTime
        );
        
        // Calcular desplazamiento para mover jugadores
        Vector3 desplazamiento = transform.position - ultimaPosicion;
        
        // Mover jugadores con la plataforma
        if (desplazamiento.magnitude > 0.001f)
        {
            foreach (Rigidbody2D rb in cuerposJugadoresEncima)
            {
                if (rb != null)
                {
                    rb.position += new Vector2(desplazamiento.x, desplazamiento.y);
                }
            }
        }
        
        // Si llegamos al punto objetivo
        if (Vector3.Distance(transform.position, puntoObjetivo.position) < 0.05f)
        {
            // Si estábamos retornando al punto, ya no estamos retornando
            if (retornandoAPunto && puntoActual == puntoRetornoSinJugador)
            {
                retornandoAPunto = false;
                
                // Si debe estar quieta sin jugador, detenerse aquí
                if (moverSoloCuandoHayJugador && !jugadorEncima)
                {
                    return;
                }
            }
            
            // Pasar al siguiente punto
            puntoActual = (puntoActual + 1) % puntosRuta.Length;
            
            // Esperar un momento en este punto
            if (rutinaEsperaEnPunto != null)
            {
                StopCoroutine(rutinaEsperaEnPunto);
            }
            rutinaEsperaEnPunto = StartCoroutine(EsperarEnPunto());
        }
    }
    
    private IEnumerator EsperarEnPunto()
    {
        bool estadoMovimiento = estaMoviendose;
        estaMoviendose = false;
        yield return new WaitForSeconds(tiempoEsperaEntrePuntos);
        estaMoviendose = estadoMovimiento;
        rutinaEsperaEnPunto = null;
    }
    
    private IEnumerator EsperarAntesDeRetornar()
    {
        yield return new WaitForSeconds(tiempoEsperaParaRetorno);
        
        // Si después del tiempo de espera sigue sin haber jugador, ir al punto de retorno
        if (!jugadorEncima && puntoRetornoSinJugador >= 0 && puntoRetornoSinJugador < puntosRuta.Length)
        {
            puntoActual = puntoRetornoSinJugador;
            retornandoAPunto = true;
            estaMoviendose = true;  // Asegurarse de que se mueva
        }
        
        rutinaTiempoEspera = null;
    }
    
    // Activar la plataforma (para usar con eventos de pociones)
    public void Activar()
    {
        estaMoviendose = true;
    }
    
    // Desactivar la plataforma
    public void Desactivar()
    {
        // Cancelar cualquier rutina en progreso
        if (rutinaTiempoEspera != null)
        {
            StopCoroutine(rutinaTiempoEspera);
            rutinaTiempoEspera = null;
        }
        
        if (rutinaEsperaEnPunto != null)
        {
            StopCoroutine(rutinaEsperaEnPunto);
            rutinaEsperaEnPunto = null;
        }
        
        // Cuando se desactiva, si está configurado para ir a un punto específico
        if (irAPuntoAlDesactivar && puntoDestinoAlDesactivar >= 0 && puntoDestinoAlDesactivar < puntosRuta.Length)
        {
            // Establecer el punto destino al desactivar
            puntoActual = puntoDestinoAlDesactivar;
            
            // Mover inmediatamente a ese punto
            StartCoroutine(MoverADestinoDesactivacion());
        }
        else
        {
            // Detener el movimiento inmediatamente
            estaMoviendose = false;
        }
    }
    
    // Corrutina para mover la plataforma al punto de desactivación
    private IEnumerator MoverADestinoDesactivacion()
    {
        // Guardar el estado actual de movimiento
        bool estadoMovimientoOriginal = estaMoviendose;
        bool moverSoloConJugadorOriginal = moverSoloCuandoHayJugador;
        
        // Forzar movimiento sin importar condiciones originales
        estaMoviendose = true;
        moverSoloCuandoHayJugador = false;
        
        // Mover hasta llegar al punto de destino
        Transform puntoDestino = puntosRuta[puntoDestinoAlDesactivar];
        while (Vector3.Distance(transform.position, puntoDestino.position) > 0.05f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, 
                puntoDestino.position, 
                velocidadMovimiento * Time.deltaTime
            );
            
            yield return null;
        }
        
        // Restaurar condiciones originales
        estaMoviendose = false;  // Forzar que quede detenida
        moverSoloCuandoHayJugador = moverSoloConJugadorOriginal;
    }
    
    // Establecer un punto específico como destino
    public void IrAPunto(int indicePunto)
    {
        if (indicePunto >= 0 && indicePunto < puntosRuta.Length)
        {
            puntoActual = indicePunto;
        }
    }
    
    // NUEVOS MÉTODOS PARA CONTROL POR DETECTOR
    
    // Detener temporalmente la plataforma cuando un detector encuentra al jugador
    public void DetenerTemporalmente()
    {
        if (!detenidoPorDetector)
        {
            // Guardar estado anterior para restaurarlo después
            estadoMovimientoAnterior = estaMoviendose;
            estaMoviendose = false;
            detenidoPorDetector = true;
        }
    }
    
    // Reanudar el movimiento cuando el detector ya no detecta al jugador
    public void ReanudarMovimiento()
    {
        if (detenidoPorDetector)
        {
            // Restaurar al estado anterior
            estaMoviendose = estadoMovimientoAnterior;
            detenidoPorDetector = false;
        }
    }
    
    // Visualizar el área de detección de jugadores en el editor
    private void OnDrawGizmos()
    {
        BoxCollider2D colPlataforma = GetComponent<BoxCollider2D>();
        if (colPlataforma == null) return;

        // Calcular centro y tamaño igual que en DetectarJugadores()
        Vector2 centro = (Vector2)transform.position + colPlataforma.offset + Vector2.up * (colPlataforma.size.y / 2 + distanciaDeteccion / 2);
        Vector2 escala = colPlataforma.transform.lossyScale;

        Vector2 tamano = new Vector2(
            colPlataforma.size.x * escala.x,
            colPlataforma.size.y * escala.y
        );

        // Dibujar área
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(centro, tamano);
        
        // Dibujar ruta
        if (puntosRuta != null && puntosRuta.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < puntosRuta.Length; i++)
            {
                if (puntosRuta[i] != null)
                {
                    // Dibujar línea al siguiente punto
                    if (i < puntosRuta.Length - 1 && puntosRuta[i + 1] != null)
                    {
                        Gizmos.DrawLine(puntosRuta[i].position, puntosRuta[i + 1].position);
                    }
                    else if (i == puntosRuta.Length - 1 && puntosRuta[0] != null)
                    {
                        // Conectar el último con el primero
                        Gizmos.DrawLine(puntosRuta[i].position, puntosRuta[0].position);
                    }
                }
            }
        }
    }
}