using UnityEngine;
using System.Runtime.InteropServices;
using System;

public class GestorFirebaseHelper : MonoBehaviour
{
    private static GestorFirebaseHelper _instancia;
    public static GestorFirebaseHelper Instancia
    {
        get
        {
            if (_instancia == null)
            {
                GameObject obj = new GameObject("GestorFirebaseHelper");
                _instancia = obj.AddComponent<GestorFirebaseHelper>();
                DontDestroyOnLoad(obj);
            }
            return _instancia;
        }
    }

    [Header("Configuración")]
    [Tooltip("Habilitar logs detallados")]
    public bool mostrarLogs = true;

    [Tooltip("Número de intentos de inicialización")]
    public int intentosInicializacion = 3;

    // Variables para seguimiento
    private bool firebaseInicializado = false;
    private int intentosActuales = 0;
    private float tiempoUltimoIntento = 0f;

    // Delegado y evento para comunicar estado
    public delegate void FirebaseEventHandler(string estado);
    public event FirebaseEventHandler OnFirebaseEstadoCambiado;

    // Importar funciones nativas de JavaScript
#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool inicializarFirebase();
#endif

    private void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        _instancia = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Comenzar intentos de inicialización
        InicializarFirebaseSiNecesario();
    }

    private void Update()
    {
        // Si no está inicializado y han pasado 3 segundos desde el último intento
        if (!firebaseInicializado && intentosActuales < intentosInicializacion && 
            Time.time - tiempoUltimoIntento > 3f)
        {
            InicializarFirebaseSiNecesario();
        }
    }

    public void InicializarFirebaseSiNecesario()
    {
        if (firebaseInicializado)
        {
            return;
        }

        tiempoUltimoIntento = Time.time;
        intentosActuales++;

        Log($"Intentando inicializar Firebase (intento {intentosActuales}/{intentosInicializacion})");

        try
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            bool resultado = inicializarFirebase();
            Log($"Llamada a inicializarFirebase completada: {resultado}");
#else
            // En el editor, simular inicialización exitosa
            SimularInicializacionExitosa();
#endif
        }
        catch (Exception ex)
        {
            LogError($"Error al inicializar Firebase: {ex.Message}");
            OnFirebaseEstadoCambiado?.Invoke("error");
        }
    }

#if UNITY_EDITOR
    private void SimularInicializacionExitosa()
    {
        Log("Simulando inicialización de Firebase en el editor");
        firebaseInicializado = true;
        OnFirebaseEstadoCambiado?.Invoke("inicializado");

        // Simular recepción de mensaje desde JS
        GestorFirebase gestorFirebase = FindObjectOfType<GestorFirebase>();
        if (gestorFirebase != null)
        {
            string mensaje = "{\"evento\":\"firebase_inicializado\",\"estado\":\"correcto\"}";
            gestorFirebase.ProcesarDatosDesdeJS(mensaje);
        }
    }
#endif

    // Método llamado por JS cuando Firebase esté listo
    public void NotificarFirebaseInicializado()
    {
        Log("Firebase ha sido inicializado correctamente");
        firebaseInicializado = true;
        OnFirebaseEstadoCambiado?.Invoke("inicializado");
    }

    // Método para marcar Firebase como inicializado (puede llamarse desde GestorFirebase)
    public void MarcarFirebaseComoInicializado()
    {
        firebaseInicializado = true;
    }

    // Método para verificar si Firebase está inicializado
    public bool EstaFirebaseInicializado()
    {
        return firebaseInicializado;
    }

    // Métodos de logging
    private void Log(string mensaje)
    {
        if (mostrarLogs)
        {
            Debug.Log($"[GestorFirebaseHelper] {mensaje}");
        }
    }

    private void LogError(string mensaje)
    {
        Debug.LogError($"[GestorFirebaseHelper] {mensaje}");
    }
}