using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuPausa : MonoBehaviour
{
    [Header("Configuración General")]
    public KeyCode teclaPausa = KeyCode.Escape;
    public KeyCode teclaPausaAlternativa = KeyCode.P; // Nueva tecla alternativa para pausar
    public float velocidadAnimacion = 2.0f;
    public bool guardarAlPausar = true;

    [Header("Referencias UI")]
    public RectTransform panelMenuPausa;
    public Button botonContinuar;
    public Button botonSalir;
    public CanvasGroup fondoOverlay;

    // Variables privadas
    private bool menuAbierto = false;
    private Vector2 posicionOculta;
    private Vector2 posicionVisible;
    private GestorJuego gestorJuego;

    private void Awake()
    {
        // Buscar el gestor de juego si no está asignado
        if (gestorJuego == null)
        {
            gestorJuego = FindObjectOfType<GestorJuego>();
        }

        // Configurar posiciones para la animación
        if (panelMenuPausa != null)
        {
            // Posición fuera de pantalla (abajo)
            posicionOculta = new Vector2(0, -panelMenuPausa.rect.height);
            // Posición en pantalla (centrado verticalmente)
            posicionVisible = Vector2.zero;
            
            // Comenzar con el menú oculto
            panelMenuPausa.anchoredPosition = posicionOculta;
            panelMenuPausa.gameObject.SetActive(false);
        }

        // Inicializar el overlay con transparencia
        if (fondoOverlay != null)
        {
            fondoOverlay.alpha = 0;
            fondoOverlay.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // Configurar los botones
        if (botonContinuar != null)
        {
            botonContinuar.onClick.AddListener(ContinuarJuego);
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.AddListener(SalirAlMenuPrincipal);
        }
    }

    private void Update()
    {
        // Detectar tecla principal o alternativa para mostrar/ocultar el menú
        if (Input.GetKeyDown(teclaPausa) || Input.GetKeyDown(teclaPausaAlternativa))
        {
            ToggleMenuPausa();
        }
    }

    public void ToggleMenuPausa()
    {
        if (menuAbierto)
        {
            CerrarMenuPausa();
        }
        else
        {
            AbrirMenuPausa();
        }
    }

    public void AbrirMenuPausa()
    {
        // Si ya está abierto, no hacer nada
        if (menuAbierto) return;

        menuAbierto = true;

        // Activar el panel antes de animar
        panelMenuPausa.gameObject.SetActive(true);
        
        // Activar el fondo overlay
        if (fondoOverlay != null)
        {
            fondoOverlay.gameObject.SetActive(true);
        }

        // Pausar el tiempo de juego
        Time.timeScale = 0f;

        // Guardar progreso si está habilitado
        if (guardarAlPausar && gestorJuego != null)
        {
            gestorJuego.ActualizarInterfazPuntuacion();
            if (gestorJuego.guardarEnAlmacenamientoLocal)
            {
                gestorJuego.GuardarEnAlmacenamientoLocal();
            }
        }

        // Iniciar animación
        StartCoroutine(AnimarMenu(true));
    }

    public void CerrarMenuPausa()
    {
        // Si ya está cerrado, no hacer nada
        if (!menuAbierto) return;

        menuAbierto = false;

        // Reanudar el tiempo de juego
        Time.timeScale = 1f;

        // Iniciar animación de cierre
        StartCoroutine(AnimarMenu(false));
    }

    private IEnumerator AnimarMenu(bool abriendo)
    {
        float tiempo = 0;
        Vector2 posInicial = abriendo ? posicionOculta : posicionVisible;
        Vector2 posFinal = abriendo ? posicionVisible : posicionOculta;
        
        // Valores iniciales y finales para el overlay
        float alphaInicial = abriendo ? 0 : 1f;
        float alphaFinal = abriendo ? 1f : 0;
        
        // Establecer alpha inicial del overlay
        if (fondoOverlay != null)
        {
            fondoOverlay.alpha = alphaInicial;
        }

        while (tiempo < 1)
        {
            tiempo += Time.unscaledDeltaTime * velocidadAnimacion;
            float t = Mathf.SmoothStep(0, 1, tiempo); // Suavizar la transición
            
            // Animar posición del panel
            panelMenuPausa.anchoredPosition = Vector2.Lerp(posInicial, posFinal, t);
            
            // Animar transparencia del overlay
            if (fondoOverlay != null)
            {
                fondoOverlay.alpha = Mathf.Lerp(alphaInicial, alphaFinal, t);
            }
            
            yield return null;
        }
        
        // Asegurar valores finales exactos
        panelMenuPausa.anchoredPosition = posFinal;
        
        if (fondoOverlay != null)
        {
            fondoOverlay.alpha = alphaFinal;
        }
        
        // Si estamos cerrando, desactivar objetos
        if (!abriendo)
        {
            panelMenuPausa.gameObject.SetActive(false);
            if (fondoOverlay != null)
            {
                fondoOverlay.gameObject.SetActive(false);
            }
        }
    }

    public void ContinuarJuego()
    {
        CerrarMenuPausa();
    }

    public void SalirAlMenuPrincipal()
    {
        // Asegurarse de restaurar el tiempo antes de cambiar de escena
        Time.timeScale = 1f;
        
        // Usar el GestorJuego para volver al menú principal
        if (gestorJuego != null)
        {
            gestorJuego.CargarNivel("MenuPrincipal");
        }
        else
        {
            // Fallback directo si no hay gestor
            UnityEngine.SceneManagement.SceneManager.LoadScene("MenuPrincipal");
        }
    }

    private void OnDestroy()
    {
        // Asegurarse de restaurar el tiempo si se destruye este objeto
        Time.timeScale = 1f;
    }
}