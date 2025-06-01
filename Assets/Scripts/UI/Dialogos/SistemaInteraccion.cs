using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MensajeDialogo
{
    [TextArea(3, 5)]
    public string mensaje;
    public float duracionVisualizacion = 3f; // Duración opcional para auto-avance
}

[System.Serializable]
public class SecuenciaDialogo
{
    public string nombreSecuencia;
    public List<MensajeDialogo> mensajes = new List<MensajeDialogo>();
    public bool yaSeEjecuto = false; // Para controlar si ya se ejecutó
}

public class SistemaInteraccion : MonoBehaviour
{
    [Header("Configuración de Zona")]
    public LayerMask capaJugador = 1;
    public bool requierePresionarTecla = true; // Si requiere presionar E para activar

    [Header("Diálogos")]
    public SecuenciaDialogo dialogoAlEntrar; // Diálogo al entrar a la zona
    public SecuenciaDialogo dialogoAlInteractuar; // Diálogo al presionar E

    [Header("Indicador de Interacción")]
    public GameObject indicadorInteraccion; // Sprite del indicador (ej: tecla E)
    public float retrasoIndicador = 2f; // Tiempo antes de mostrar el indicador
    public float velocidadDesvanecimiento = 2f; // Velocidad de fade in/out

    [Header("Configuración del Diálogo")]
    public bool permitirSaltarConFlechas = true;
    public bool permitirSaltarConEspacio = true;
    public bool permitirSaltarConEnter = true;

    // Variables privadas
    private bool jugadorEnZona = false;
    private bool interaccionUsada = false;
    private Coroutine corrutinaIndicador;
    private Coroutine corrutinaDialogo;
    private SpriteRenderer renderizadorIndicador;

    // Referencias
    private UIDialogo uiDialogo;

    void Start()
    {
        // Buscar el sistema de UI de diálogos
        uiDialogo = FindObjectOfType<UIDialogo>();
        if (uiDialogo == null)
        {
            Debug.LogError("No se encontró UIDialogo en la escena. Asegúrate de tener el componente UIDialogo.");
        }

        // Configurar el indicador
        if (indicadorInteraccion != null)
        {
            renderizadorIndicador = indicadorInteraccion.GetComponent<SpriteRenderer>();
            if (renderizadorIndicador == null)
            {
                renderizadorIndicador = indicadorInteraccion.GetComponentInChildren<SpriteRenderer>();
            }

            indicadorInteraccion.SetActive(false);
        }
    }

    void Update()
    {
        if (jugadorEnZona && requierePresionarTecla && !interaccionUsada)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                ActivarInteraccion();
            }
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (EsJugador(otro))
        {
            jugadorEnZona = true;

            // Ejecutar diálogo de entrada si existe y no se ha ejecutado
            if (dialogoAlEntrar.mensajes.Count > 0 && !dialogoAlEntrar.yaSeEjecuto)
            {
                IniciarDialogo(dialogoAlEntrar);
            }

            // Iniciar corrutina del indicador si se requiere interacción
            if (requierePresionarTecla && indicadorInteraccion != null && !interaccionUsada)
            {
                corrutinaIndicador = StartCoroutine(MostrarIndicadorConRetraso());
            }
        }
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (EsJugador(otro))
        {
            jugadorEnZona = false;

            // Ocultar indicador
            if (corrutinaIndicador != null)
            {
                StopCoroutine(corrutinaIndicador);
            }

            if (indicadorInteraccion != null)
            {
                StartCoroutine(DesvaneciendoIndicador());
            }
        }
    }

    private bool EsJugador(Collider2D colisionador)
    {
        return (capaJugador.value & (1 << colisionador.gameObject.layer)) > 0;
    }

    private void ActivarInteraccion()
    {
        interaccionUsada = true;

        // Ocultar indicador
        if (indicadorInteraccion != null)
        {
            StartCoroutine(DesvaneciendoIndicador());
        }

        // Ejecutar diálogo de interacción
        if (dialogoAlInteractuar.mensajes.Count > 0)
        {
            IniciarDialogo(dialogoAlInteractuar);
        }
    }

    private void IniciarDialogo(SecuenciaDialogo secuencia)
    {
        if (uiDialogo != null)
        {
            secuencia.yaSeEjecuto = true;
            corrutinaDialogo = StartCoroutine(uiDialogo.MostrarSecuenciaDialogo(secuencia.mensajes));
        }
    }

    private IEnumerator MostrarIndicadorConRetraso()
    {
        yield return new WaitForSeconds(retrasoIndicador);

        if (jugadorEnZona && !interaccionUsada && indicadorInteraccion != null)
        {
            indicadorInteraccion.SetActive(true);
            yield return StartCoroutine(ApareciendoIndicador());
        }
    }

    private IEnumerator ApareciendoIndicador()
    {
        if (renderizadorIndicador == null) yield break;

        float alfa = 0f;
        Color color = renderizadorIndicador.color;

        while (alfa < 1f)
        {
            alfa += Time.deltaTime * velocidadDesvanecimiento;
            color.a = alfa;
            renderizadorIndicador.color = color;
            yield return null;
        }

        color.a = 1f;
        renderizadorIndicador.color = color;
    }

    private IEnumerator DesvaneciendoIndicador()
    {
        if (renderizadorIndicador == null || !indicadorInteraccion.activeInHierarchy) yield break;

        float alfa = renderizadorIndicador.color.a;
        Color color = renderizadorIndicador.color;

        while (alfa > 0f)
        {
            alfa -= Time.deltaTime * velocidadDesvanecimiento;
            color.a = alfa;
            renderizadorIndicador.color = color;
            yield return null;
        }

        color.a = 0f;
        renderizadorIndicador.color = color;
        indicadorInteraccion.SetActive(false);
    }

    // Método público para resetear la interacción (útil para objetos reutilizables)
    public void ReiniciarInteraccion()
    {
        interaccionUsada = false;
        dialogoAlEntrar.yaSeEjecuto = false;
        dialogoAlInteractuar.yaSeEjecuto = false;
    }

    // Método para forzar un diálogo desde código
    public void ForzarDialogo(SecuenciaDialogo secuencia)
    {
        IniciarDialogo(secuencia);
    }
    
    // Para visualizar el área de detección en el editor
    private void OnDrawGizmos()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            Gizmos.color = new Color(0.2f, 1.0f, 0f, 0.3f); // Verde
            
            if (collider is BoxCollider2D)
            {
                BoxCollider2D boxCollider = collider as BoxCollider2D;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(boxCollider.offset, boxCollider.size);
            }
            
        }
        
    }
}