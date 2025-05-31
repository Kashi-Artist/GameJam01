using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class GestorJuego : MonoBehaviour
{
    [Header("Referencias UI")]
    public TextMeshProUGUI textoPuntuacionActual;
    public TextMeshProUGUI textoPuntuacionMaxima;
    public TextMeshProUGUI textoNombreUsuario;
    public TextMeshProUGUI textoPorcentajeExploracion; // Nuevo: Texto para mostrar porcentaje

    [Header("Referencias")]
    public ColectorPociones[] colectoresJugadores;

    [Header("Web/Itch.io")]
    public bool guardarEnAlmacenamientoLocal = true;

    // Singleton para acceso global
    public static GestorJuego Instancia { get; private set; }

    // Referencia al gestor de usuarios
    private GestorUsuarios gestorUsuarios;

    private void Awake()
    {
        // Configuración del singleton
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Buscar o crear el gestor de usuarios
        gestorUsuarios = FindObjectOfType<GestorUsuarios>();
        if (gestorUsuarios == null)
        {
            GameObject gestorUsuariosObj = new GameObject("GestorUsuarios");
            gestorUsuarios = gestorUsuariosObj.AddComponent<GestorUsuarios>();
            DontDestroyOnLoad(gestorUsuariosObj);
        }

        // Buscar colectores de pociones en la escena si no están asignados
        if (colectoresJugadores == null || colectoresJugadores.Length == 0)
        {
            colectoresJugadores = FindObjectsOfType<ColectorPociones>();
        }

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void Start()
    {
        // Configurar UI inicial
        ActualizarInterfazPuntuacion();
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Buscar colectores de pociones en la nueva escena
        colectoresJugadores = FindObjectsOfType<ColectorPociones>();

        // Configurar UI si existe
        ActualizarInterfazPuntuacion();

        // Suscribirse a eventos de puntuación
        foreach (var colector in colectoresJugadores)
        {
            colector.alCambiarPuntuacion.AddListener(AlCambiarPuntuacion);
        }

        // Actualizar nombre de usuario en la UI si existe
        ActualizarInterfazUsuario();
    }

    private void AlCambiarPuntuacion(int nuevaPuntuacion)
    {
        ActualizarInterfazPuntuacion();

        // Actualizar el puntaje en el gestor de usuarios
        if (gestorUsuarios != null)
        {
            gestorUsuarios.ActualizarPuntaje(nuevaPuntuacion);
        }
    }

    public void ActualizarInterfazPuntuacion()
    {
        if (textoPuntuacionActual != null)
        {
            int puntuacionActual = 0;
            foreach (var colector in colectoresJugadores)
            {
                puntuacionActual += colector.puntuacionActual;
            }
            textoPuntuacionActual.text = "Puntuación: " + puntuacionActual.ToString();
        }

        if (textoPuntuacionMaxima != null)
        {
            int puntuacionMaxima = ObtenerPuntuacionMaxima();
            textoPuntuacionMaxima.text = "Máximo: " + puntuacionMaxima.ToString();
        }

        // Actualizar texto de porcentaje de exploración si está disponible
        if (textoPorcentajeExploracion != null && colectoresJugadores.Length > 0)
        {
            float porcentajeTotal = 0f;
            foreach (var colector in colectoresJugadores)
            {
                porcentajeTotal += colector.porcentajeExploracion;
            }
            // Si hay múltiples colectores, mostrar el promedio
            if (colectoresJugadores.Length > 1)
            {
                porcentajeTotal /= colectoresJugadores.Length;
            }
            textoPorcentajeExploracion.text = "Exploración: " + porcentajeTotal.ToString("0.0") + "%";
        }
    }

    public void ActualizarInterfazUsuario()
    {
        if (textoNombreUsuario != null && gestorUsuarios != null)
        {
            string nombreUsuario = gestorUsuarios.ObtenerNombreUsuarioActual();
            if (!string.IsNullOrEmpty(nombreUsuario))
            {
                textoNombreUsuario.text = "Jugador: " + nombreUsuario;
            }
            else
            {
                textoNombreUsuario.text = "Jugador: Invitado";
            }
        }
    }

    public int ObtenerPuntuacionMaxima()
    {
        if (gestorUsuarios != null)
        {
            DatosUsuario usuario = gestorUsuarios.ObtenerDatosUsuarioActual();
            if (usuario != null)
            {
                return usuario.puntajeMaximo;
            }
        }

        return PlayerPrefs.GetInt("PuntuacionMaxima", 0);
    }

    public void GuardarEnAlmacenamientoLocal()
    {
        PlayerPrefs.Save();
    }

    public void CargarNivel(int indiceNivel)
    {
        SceneManager.LoadScene(indiceNivel);
    }

    public void CargarNivel(string nombreNivel)
    {
        SceneManager.LoadScene(nombreNivel);
    }

    public void ReiniciarNivelActual()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SiguienteNivel()
    {
        int siguienteNivel = SceneManager.GetActiveScene().buildIndex + 1;
        if (siguienteNivel < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(siguienteNivel);
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }
}