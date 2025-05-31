using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Enumeración para los tipos de pociones
public enum TipoPocion
{
    Blanca,
    BlancaOculta,  // Ya estaba definido, lo mantenemos
    Oro,
    Azul,
    Roja
}

public class Pocion : MonoBehaviour
{
    [Header("Configuración")]
    public TipoPocion tipo = TipoPocion.Blanca;
    public int valorPuntos = 1;
    public float velocidadEscalado = 0.1f;
    public float velocidadFlotacion = 0.4f;
    public float amplitudFlotacion = 0.5f;

    [Header("Efectos")]
    public ParticleSystem efectoRecoleccion;
    public ParticleSystem efectoAmbiental; // Nueva referencia para las partículas ambientales
    public AudioClip sonidoRecoleccion;

    private Vector3 posicionInicial;
    private float tiempoOffset;
    private ParticleSystem instanciaEfectoAmbiental; // Para guardar la instancia

    private void Start()
    {
        // Guardar posición inicial para el efecto de flotación
        posicionInicial = transform.position;

        // Offset aleatorio para que no todas las pociones floten al mismo tiempo
        tiempoOffset = Random.Range(0f, 2f * Mathf.PI);

        // Instanciar efecto ambiental si está configurado
        if (efectoAmbiental != null)
        {
            instanciaEfectoAmbiental = Instantiate(efectoAmbiental, transform.position, Quaternion.identity, transform);
            // Ajustar posición si es necesario
            instanciaEfectoAmbiental.transform.localPosition = Vector3.zero;
        }
    }

    private void Update()
    {
        // Escalado suave
        float escalaFactor = 1f + Mathf.Sin((Time.time + tiempoOffset) * velocidadEscalado) * 0.05f;
        transform.localScale = new Vector3(escalaFactor, escalaFactor, escalaFactor);

        // Efecto de flotación suave
        float nuevaY = posicionInicial.y + Mathf.Sin((Time.time + tiempoOffset) * velocidadFlotacion) * amplitudFlotacion;
        transform.position = new Vector3(transform.position.x, nuevaY, transform.position.z);
    }

    private void OnDestroy()
    {
        // Destruir las partículas ambientales cuando la poción se destruya
        if (instanciaEfectoAmbiental != null)
        {
            // Opcional: Detener la emisión y dejar que las partículas restantes terminen
            instanciaEfectoAmbiental.Stop();
            Destroy(instanciaEfectoAmbiental.gameObject, instanciaEfectoAmbiental.main.duration);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar si el collider pertenece a un jugador
        MovimientoTopDown jugador = other.GetComponent<MovimientoTopDown>();
        if (jugador != null)
        {
            Recolectar(other.gameObject);
        }
    }

    private void Recolectar(GameObject jugador)
    {
        // Buscar el colector de pociones en el jugador
        ColectorPociones colector = jugador.GetComponent<ColectorPociones>();
        if (colector != null)
        {
            // Informar al colector
            colector.RecolectarPocion(this);

            // Reproducir efectos
            if (efectoRecoleccion != null)
            {
                ParticleSystem efecto = Instantiate(efectoRecoleccion, transform.position, Quaternion.identity);
                Destroy(efecto.gameObject, 2f); // Destruir después de reproducirse
            }

            if (sonidoRecoleccion != null && Camera.main != null)
            {
                AudioSource.PlayClipAtPoint(sonidoRecoleccion, Camera.main.transform.position);
            }

            // Destruir la poción
            Destroy(gameObject);
        }
    }
}