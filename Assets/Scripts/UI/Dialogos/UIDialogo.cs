using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIDialogo : MonoBehaviour
{
    [Header("Referencias de UI")]
    public GameObject panelDialogo; // Panel principal del diálogo
    public TextMeshProUGUI textoDialogo; // Texto del diálogo
    public Image imagenPersonaje; // Imagen del personaje (opcional)
    public Button botonSiguiente; // Botón siguiente (opcional)
    public GameObject indicadorSaltar; // Indicador de "presiona para continuar"
    
    [Header("Configuración de Escritura")]
    public float velocidadMaquinaEscribir = 0.05f; // Velocidad del efecto máquina de escribir
    public bool usarEfectoMaquinaEscribir = true;
    
    [Header("Configuración de Audio")]
    public AudioSource fuenteAudio;
    public AudioClip sonidoEscritura;
    public AudioClip sonidoSiguienteMensaje;
    
    // Variables privadas
    private Queue<MensajeDialogo> colaMensajes;
    private bool estaEscribiendo = false;
    private bool dialogoActivo = false;
    private Coroutine corrutinaEscritura;
    private MensajeDialogo mensajeActual;
    
    void Start()
    {
        // Inicializar cola de mensajes
        colaMensajes = new Queue<MensajeDialogo>();
        
        // Ocultar panel al inicio
        if (panelDialogo != null)
            panelDialogo.SetActive(false);
            
        // Configurar botón si existe
        if (botonSiguiente != null)
            botonSiguiente.onClick.AddListener(SiguienteMensaje);
    }
    
    void Update()
    {
        if (dialogoActivo)
        {
            // Detectar input para avanzar
            if (Input.GetKeyDown(KeyCode.Space) || 
                Input.GetKeyDown(KeyCode.Return) || 
                Input.GetKeyDown(KeyCode.KeypadEnter) ||
                Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.DownArrow) ||
                Input.GetKeyDown(KeyCode.LeftArrow) ||
                Input.GetKeyDown(KeyCode.RightArrow))
            {
                if (estaEscribiendo)
                {
                    // Si está escribiendo, completar el texto
                    CompletarMensajeActual();
                }
                else
                {
                    // Si no está escribiendo, pasar al siguiente mensaje
                    SiguienteMensaje();
                }
            }
        }
    }
    
    public IEnumerator MostrarSecuenciaDialogo(List<MensajeDialogo> mensajes)
    {
        // Activar el sistema de diálogo
        dialogoActivo = true;
        
        // Pausar el juego (opcional)
        Time.timeScale = 0f;
        
        // Mostrar panel
        if (panelDialogo != null)
            panelDialogo.SetActive(true);
            
        // Cargar mensajes en la cola
        colaMensajes.Clear();
        foreach (var mensaje in mensajes)
        {
            colaMensajes.Enqueue(mensaje);
        }
        
        // Mostrar primer mensaje
        MostrarSiguienteMensaje();
        
        // Esperar hasta que todos los mensajes sean mostrados
        while (colaMensajes.Count > 0 || estaEscribiendo)
        {
            yield return null;
        }
        
        // Esperar input final para cerrar
        yield return new WaitUntil(() => 
            Input.GetKeyDown(KeyCode.Space) || 
            Input.GetKeyDown(KeyCode.Return) || 
            Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.RightArrow));
        
        // Ocultar diálogo
        OcultarDialogo();
    }
    
    private void MostrarSiguienteMensaje()
    {
        if (colaMensajes.Count == 0)
        {
            return;
        }
        
        mensajeActual = colaMensajes.Dequeue();
        
        // Reproducir sonido de nuevo mensaje
        if (fuenteAudio != null && sonidoSiguienteMensaje != null)
        {
            fuenteAudio.PlayOneShot(sonidoSiguienteMensaje);
        }
        
        // Mostrar el texto
        if (usarEfectoMaquinaEscribir)
        {
            corrutinaEscritura = StartCoroutine(EfectoMaquinaEscribir(mensajeActual.mensaje));
        }
        else
        {
            textoDialogo.text = mensajeActual.mensaje;
            estaEscribiendo = false;
        }
        
        // Actualizar indicador de saltar
        ActualizarIndicadorSaltar();
    }
    
    private IEnumerator EfectoMaquinaEscribir(string mensaje)
    {
        estaEscribiendo = true;
        textoDialogo.text = "";
        
        foreach (char letra in mensaje.ToCharArray())
        {
            textoDialogo.text += letra;
            
            // Reproducir sonido de escritura
            if (fuenteAudio != null && sonidoEscritura != null && letra != ' ')
            {
                fuenteAudio.PlayOneShot(sonidoEscritura);
            }
            
            yield return new WaitForSecondsRealtime(velocidadMaquinaEscribir);
        }
        
        estaEscribiendo = false;
        ActualizarIndicadorSaltar();
    }
    
    private void CompletarMensajeActual()
    {
        if (corrutinaEscritura != null)
        {
            StopCoroutine(corrutinaEscritura);
        }
        
        textoDialogo.text = mensajeActual.mensaje;
        estaEscribiendo = false;
        ActualizarIndicadorSaltar();
    }
    
    private void SiguienteMensaje()
    {
        if (estaEscribiendo)
        {
            CompletarMensajeActual();
            return;
        }
        
        if (colaMensajes.Count > 0)
        {
            MostrarSiguienteMensaje();
        }
    }
    
    private void ActualizarIndicadorSaltar()
    {
        if (indicadorSaltar != null)
        {
            // Mostrar indicador solo si no está escribiendo y hay más mensajes o es el último
            indicadorSaltar.SetActive(!estaEscribiendo);
        }
    }
    
    private void OcultarDialogo()
    {
        dialogoActivo = false;
        
        // Ocultar panel
        if (panelDialogo != null)
            panelDialogo.SetActive(false);
            
        // Reanudar el juego
        Time.timeScale = 1f;
        
        // Limpiar referencias
        colaMensajes.Clear();
        mensajeActual = null;
    }
    
    // Método público para mostrar un diálogo simple
    public void MostrarDialogoSimple(string mensaje)
    {
        var mensajeDialogo = new MensajeDialogo { mensaje = mensaje };
        var mensajes = new List<MensajeDialogo> { mensajeDialogo };
        StartCoroutine(MostrarSecuenciaDialogo(mensajes));
    }
    
    // Método para verificar si hay un diálogo activo
    public bool EstaDialogoActivo()
    {
        return dialogoActivo;
    }
}