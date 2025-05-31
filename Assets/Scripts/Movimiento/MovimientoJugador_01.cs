using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoTopDown : MonoBehaviour
{
    private Rigidbody2D rb2D;
    private Animator animator;

    [Header("Movimiento")]
    [SerializeField] private float velocidadDeMovimiento = 5f;
    [Range(0, 0.3f)][SerializeField] private float suavizadoDeMovimiento = 0.05f;
    private Vector3 velocidad = Vector3.zero;
    private Vector2 movimiento;
    
    [Header("Dirección")]
    private bool mirandoDerecha = true;
    
    [Header("Animación")]
    private Vector2 ultimaDireccion; // Para recordar la última dirección

    private void Start()
    {
        rb2D = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        // Configurar Rigidbody2D para top-down
        rb2D.gravityScale = 0f; // Sin gravedad en top-down
        rb2D.freezeRotation = true; // Evitar rotación no deseada
        
        // Inicializar última dirección
        ultimaDireccion = Vector2.down; // Por defecto mirando hacia abajo
    }

    private void Update()
    {
        // Obtener entrada de movimiento en ambos ejes
        float movimientoHorizontal = Input.GetAxisRaw("Horizontal1");
        float movimientoVertical = Input.GetAxisRaw("Vertical1");
        
        // Crear vector de movimiento
        movimiento = new Vector2(movimientoHorizontal, movimientoVertical);
        
        // Normalizar para movimiento diagonal consistente
        if (movimiento.magnitude > 1f)
        {
            movimiento = movimiento.normalized;
        }
        
        // Actualizar animaciones
        ActualizarAnimaciones();
        
        // Manejar dirección del personaje
        ManejarDireccion(movimientoHorizontal);
    }

    private void FixedUpdate()
    {
        // Aplicar movimiento
        Mover();
    }

    private void Mover()
    {
        // Calcular velocidad objetivo
        Vector2 velocidadObjetivo = movimiento * velocidadDeMovimiento;
        
        // Aplicar suavizado
        rb2D.velocity = Vector3.SmoothDamp(rb2D.velocity, velocidadObjetivo, ref velocidad, suavizadoDeMovimiento);
    }

    private void ActualizarAnimaciones()
    {
        bool estaMoviendose = movimiento.magnitude > 0.1f;
        
        if (estaMoviendose)
        {
            // Si se está moviendo, actualizar parámetros y guardar dirección
            animator.SetFloat("MovimientoX", movimiento.x);
            animator.SetFloat("MovimientoY", movimiento.y);
            ultimaDireccion = movimiento;
            
            // Activar animación de movimiento
            animator.SetBool("EstaMoviendo", true);
        }
        else
        {
            // Si está quieto, mantener la última dirección pero pausar la animación
            animator.SetFloat("MovimientoX", ultimaDireccion.x);
            animator.SetFloat("MovimientoY", ultimaDireccion.y);
            
            // Desactivar animación de movimiento (esto pausará en el último frame)
            animator.SetBool("EstaMoviendo", false);
        }
    }

    private void ManejarDireccion(float movimientoHorizontal)
    {
        // Girar el personaje según la dirección horizontal
        if (movimientoHorizontal > 0 && !mirandoDerecha)
        {
            Girar();
        }
        else if (movimientoHorizontal < 0 && mirandoDerecha)
        {
            Girar();
        }
    }

    private void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }

    // Métodos públicos para acceder al estado
    public bool EstaMoviendose()
    {
        return movimiento.magnitude > 0.1f;
    }
    
    public Vector2 GetDireccionMovimiento()
    {
        return movimiento;
    }
    
    public bool EsMirandoDerecha()
    {
        return mirandoDerecha;
    }
    
    public Vector2 GetUltimaDireccion()
    {
        return ultimaDireccion;
    }
}