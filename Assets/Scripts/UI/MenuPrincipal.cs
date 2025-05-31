using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class MenuPrincipal : MonoBehaviour
{   
    [Header("Panel de Registro de Usuario")]
    public GameObject panelMenuPrincipal;
    public GameObject panelRegistroUsuario;
    public TMP_InputField campoNombreUsuario;
    public Button botonConfirmarUsuario;
    public TextMeshProUGUI textoErrorNombre;

    [Header("Variación de Color del Fondo")]
    public Image imagenFondoMenuPrincipal;
    public Color colorA = Color.white;
    public Color colorB = Color.gray;
    public float velocidadColor = 1.0f;

    
    [Header("Efecto de Zoom para Fondo")]
    public RectTransform fondoMenu;
    public float velocidadZoom = 0.5f;
    public float escalaFinal = 1.5f;
    public float velocidadFade = 1.0f;  // Nueva velocidad para el efecto de fade
    
    [Header("Componentes de Fade")]
    public CanvasGroup panelRegistroCanvasGroup; // ya está
    public CanvasGroup fondoCanvasGroup; // nuevo: fade solo del fondo

    
    // Referencia a los gestores
    private GestorUsuarios gestorUsuarios;
    private bool zoomActivo = false;
    private Vector3 escalaOriginal;
    
    private void Awake()
    {
        // Guardar escala original del fondo
        if (fondoMenu != null)
        {
            escalaOriginal = fondoMenu.localScale;
        }
        
        // Si no se especificó un CanvasGroup, intentar obtenerlo
        if (panelRegistroCanvasGroup == null && panelRegistroUsuario != null)
        {
            panelRegistroCanvasGroup = panelRegistroUsuario.GetComponent<CanvasGroup>();
            if (panelRegistroCanvasGroup == null)
            {
                // Añadir el componente si no existe
                panelRegistroCanvasGroup = panelRegistroUsuario.AddComponent<CanvasGroup>();
            }
        }
    }
    
    private void Start()
    {
        // Buscar gestores
        gestorUsuarios = FindObjectOfType<GestorUsuarios>();
        
        // Configurar estado inicial
        if (panelRegistroUsuario != null)
        {
            panelRegistroUsuario.SetActive(false);
        }
        
        if (textoErrorNombre != null)
        {
            textoErrorNombre.gameObject.SetActive(false);
        }
        
        // Asegurarse de que el menú principal esté visible por defecto
        if (panelMenuPrincipal != null)
        {
            panelMenuPrincipal.SetActive(true);
        }
  
        // Comprobar si necesitamos mostrar el registro inmediatamente
        if (gestorUsuarios != null)
        {
            // Suscribirse al evento de inicialización
            gestorUsuarios.OnInicializacionCompleta += ConfigurarPantallaInicial;
            
            // Si ya se ha inicializado, configurar pantalla
            ConfigurarPantallaInicial();
        }
        
        // Configurar el botón de confirmar
        if (botonConfirmarUsuario != null)
        {
            botonConfirmarUsuario.onClick.AddListener(ConfirmarNombreUsuario);
        }
        
        // Configurar evento para tecla Enter en el campo de texto
        if (campoNombreUsuario != null)
        {
            campoNombreUsuario.onSubmit.AddListener((value) => ConfirmarNombreUsuario());
        }
    }

    private void Update()
    {
        AnimarFondo();
    }

    private void AnimarFondo()
    {


        // 2. Color cíclico
        if (imagenFondoMenuPrincipal != null)
        {
            float t = (Mathf.Sin(Time.time * velocidadColor) + 1f) / 2f;
            imagenFondoMenuPrincipal.color = Color.Lerp(colorA, colorB, t);
        }
    }
    
    // Método para configurar la pantalla inicial
    private void ConfigurarPantallaInicial()
    {
        if (gestorUsuarios == null) return;
        
        // Si es primer uso forzado, mostrar directamente la pantalla de registro
        if (gestorUsuarios.EsPrimerUso())
        {
            MostrarPantallaRegistro();
        }
        else
        {
            // Siempre mostrar menú principal, verificar usuario solo al hacer acciones
            MostrarMenuPrincipal();
        }
    }
    
    // Método para mostrar la pantalla de registro
    private void MostrarPantallaRegistro()
    {
        if (panelRegistroUsuario != null)
        {
            // Preparar el panel antes de mostrarlo
            if (panelRegistroCanvasGroup != null)
            {
                panelRegistroCanvasGroup.alpha = 0f;
            }
            
            panelRegistroUsuario.SetActive(true);
            
            // Limpiar el campo de nombre
            if (campoNombreUsuario != null)
            {
                campoNombreUsuario.text = "";
            }
            
            // Iniciar efecto de fade in y luego zoom
            StartCoroutine(MostrarPanelRegistroConEfectos());
        }
    }
    
    private IEnumerator MostrarPanelRegistroConEfectos()
    {   
        // 0. Fade in del fondo primero
        if (fondoCanvasGroup != null)
        {
            fondoCanvasGroup.alpha = 0f;
            float tiempoFondo = 0f;
            while (tiempoFondo < 1f)
            {
                tiempoFondo += Time.deltaTime * velocidadFade;
                fondoCanvasGroup.alpha = Mathf.Clamp01(tiempoFondo);
                yield return null;
            }
            fondoCanvasGroup.alpha = 1f;
        }

        // 1. Iniciar el zoom del fondo inmediatamente después del fade-in
        if (fondoMenu != null && !zoomActivo)
        {
            zoomActivo = true;
            StartCoroutine(EfectoZoom());
        }

        // 2. Fade in del panel de registro (en paralelo al zoom)
        if (panelRegistroCanvasGroup != null)
        {
            panelRegistroCanvasGroup.alpha = 0f;
            float tiempo = 0;
            while (tiempo < 1)
            {
                tiempo += Time.deltaTime * velocidadFade;
                panelRegistroCanvasGroup.alpha = Mathf.Clamp01(tiempo);
                yield return null;
            }
            panelRegistroCanvasGroup.alpha = 1f;
        }

        // 3. Activar el campo de texto
        if (campoNombreUsuario != null)
        {
            campoNombreUsuario.Select();
            campoNombreUsuario.ActivateInputField();
        }
    }
    private IEnumerator OcultarPanelRegistroConFadeOut()
    {
        // 1. Fade out del contenido (panelRegistroCanvasGroup)
        if (panelRegistroCanvasGroup != null)
        {
            float tiempo = 1.5f;
            while (tiempo > 0)
            {
                tiempo -= Time.deltaTime * velocidadFade;
                panelRegistroCanvasGroup.alpha = Mathf.Clamp01(tiempo);
                yield return null;
            }
            panelRegistroCanvasGroup.alpha = 0f;
        }

        // 2. Fade out del fondo (opcional, si lo tienes)
        if (fondoCanvasGroup != null)
        {
            float tiempoFondo = 1.0f;
            while (tiempoFondo > 0)
            {
                tiempoFondo -= Time.deltaTime * velocidadFade;
                fondoCanvasGroup.alpha = Mathf.Clamp01(tiempoFondo);
                yield return null;
            }
            fondoCanvasGroup.alpha = 0f;
        }

        // 3. Ocultar el panel y mostrar el menú principal
        MostrarMenuPrincipal();
    }

    
    // Método para volver al menú principal
    private void MostrarMenuPrincipal()
    {
        if (panelMenuPrincipal != null)
        {
            panelMenuPrincipal.SetActive(true);
        }
        
        if (panelRegistroUsuario != null)
        {
            panelRegistroUsuario.SetActive(false);
        }
    }
    
    // Método para confirmar el nombre de usuario
    public void ConfirmarNombreUsuario()
    {
        if (campoNombreUsuario != null && gestorUsuarios != null)
        {
            string nombreUsuario = campoNombreUsuario.text.Trim();
            
            // Validar el nombre
            if (string.IsNullOrEmpty(nombreUsuario))
            {
                if (textoErrorNombre != null)
                {
                    textoErrorNombre.gameObject.SetActive(true);
                    textoErrorNombre.text = "Introduce un nombre";
                }
                
                // Volver a activar el campo para permitir escribir de nuevo
                if (campoNombreUsuario != null)
                {
                    campoNombreUsuario.Select();
                    campoNombreUsuario.ActivateInputField();
                }
                return;
            }
            
            // Comprobar si el usuario ya existe
            DatosUsuario usuarioExistente = gestorUsuarios.ObtenerUsuario(nombreUsuario);
            
            // Guardar el nombre de usuario (si es nuevo, se creará; si existe, se usará el existente)
            gestorUsuarios.EstablecerUsuario(nombreUsuario);
            
            // Marcar que ya no es el primer uso
            gestorUsuarios.MarcarComoUsado();
            
            // Mostrar mensaje breve de confirmación
            if (textoErrorNombre != null)
            {
                textoErrorNombre.gameObject.SetActive(true);
                
                if (usuarioExistente != null)
                {
                textoErrorNombre.text = $"{nombreUsuario}! Tu mejor exploración ha sido del {usuarioExistente.puntajeMaximo}%";
                }
                else
                {
                    textoErrorNombre.text = $"{nombreUsuario}! Ya estas horneado para iniciar tu viaje";
                }
                
                // Ocultar el mensaje después de un tiempo
                StartCoroutine(OcultarMensajeErrorDespuesDe(3f));
            }
            
            // Volver al menú principal
            StartCoroutine(OcultarPanelRegistroConFadeOut());
        }
    }
    
    // Método para ocultar el mensaje de error después de un tiempo
    private IEnumerator OcultarMensajeErrorDespuesDe(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        
        if (textoErrorNombre != null && textoErrorNombre.gameObject != null)
        {
            textoErrorNombre.gameObject.SetActive(false);
        }
    }
    
    // Efecto de zoom para el fondo
    private System.Collections.IEnumerator EfectoZoom()
    {
        float tiempo = 0;
        Vector3 escalaInicial = fondoMenu.localScale;
        Vector3 escalaObjetivo = escalaOriginal * escalaFinal;
        
        while (tiempo < 1)
        {
            tiempo += Time.deltaTime * velocidadZoom;
            fondoMenu.localScale = Vector3.Lerp(escalaInicial, escalaObjetivo, tiempo);
            yield return null;
        }
        
        fondoMenu.localScale = escalaObjetivo;
    }
    


    // Métodos de navegación
    public void VerificarPrimerUso(string accion)
    {
        // Comprobar si hay un usuario activo antes de realizar cualquier acción
        if (gestorUsuarios != null && 
            (string.IsNullOrEmpty(gestorUsuarios.ObtenerNombreUsuarioActual()) || gestorUsuarios.DebeRegistrarUsuario()))
        {
            // Si no hay usuario activo, mostrar pantalla de registro
            MostrarPantallaRegistro();
        }
        else
        {
            // Si hay un usuario o no hay gestor, realizar la acción
            switch (accion)
            {
                case "IniciarPartido":
                    IniciarPartido();
                    break;
                case "IrAEscenarios":
                    IrAEscenarios();
                    break;
                case "IrAPuntajes":
                    IrAPuntajes();
                    break;
                case "Salir":
                    // Al salir, configurar para pedir usuario en la próxima sesión
                    if (gestorUsuarios != null && gestorUsuarios.pedirSiempreUsuario)
                    {
                        gestorUsuarios.ForzarRegistroProximaSesion();
                    }
                    Salir();
                    break;
            }
        }
    }
    
    public void IniciarPartido()
    {
        SceneManager.LoadScene("Level_01");
    }

    public void IniciarPartido5()
    {
        SceneManager.LoadScene("Level_05");
    }

    public void IrAEscenarios()
    {
        SceneManager.LoadScene("MenuEscenarios");
    }

    public void IrAPuntajes()
    {
        SceneManager.LoadScene("MenuPuntajes");
    }

    public void IrAMenuPrincipal()
    {
        SceneManager.LoadScene("MenuPrincipal");
    }

    public void Salir()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
    
    private void OnDestroy()
    {
        if (gestorUsuarios != null)
        {
            gestorUsuarios.OnInicializacionCompleta -= ConfigurarPantallaInicial;
        }
    }
}