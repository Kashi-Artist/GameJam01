using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;

public class ConfiguracionWebGL : MonoBehaviour
{
    [Header("Depuración")]
    public bool depuracionAlmacenamiento = true;
    public bool mostrarLogsJS = true;

    private static ConfiguracionWebGL _instancia;
    public static ConfiguracionWebGL Instancia
    {
        get { return _instancia; }
    }

    // Eventos para comunicar con otros componentes
    public event Action<string> OnDatosRecibidosJS;

    // Importar funciones de JavaScript
    #if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void guardarDatosJS(string clave, string valor);

    [DllImport("__Internal")]
    private static extern string cargarDatosJS(string clave);

    [DllImport("__Internal")]
    private static extern void eliminarDatosJS(string clave);
    #endif

    private void Awake()
    {
        if (_instancia == null)
        {
            _instancia = this;
            DontDestroyOnLoad(gameObject);
            RegistrarComponentes();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void RegistrarComponentes()
    {
        // Buscar los componentes relevantes
        GestorFirebase gestorFirebase = FindObjectOfType<GestorFirebase>();
        if (gestorFirebase == null)
        {
            Debug.LogWarning("[ConfiguracionWebGL] No se encontró GestorFirebase");
            // No lo creamos automáticamente ya que puede ser intencional no usarlo
        }
    }

    // Método para guardar datos en almacenamiento local o SessionStorage
    public void GuardarDatos(string clave, string valor)
    {
        if (string.IsNullOrEmpty(clave))
        {
            Debug.LogError("[ConfiguracionWebGL] Error: Clave vacía, no se pueden guardar datos");
            return;
        }

        try
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            guardarDatosJS(clave, valor);
            #else
            PlayerPrefs.SetString(clave, valor);
            PlayerPrefs.Save();
            #endif

            if (depuracionAlmacenamiento)
            {
                Debug.Log($"[ConfiguracionWebGL] Datos guardados - Clave: {clave}, Valor: {valor}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfiguracionWebGL] Error al guardar datos: {e.Message}");
        }
    }

    // Método para cargar datos desde almacenamiento local o SessionStorage
    public string CargarDatos(string clave, string valorPorDefecto = "")
    {
        if (string.IsNullOrEmpty(clave))
        {
            Debug.LogError("[ConfiguracionWebGL] Error: Clave vacía, no se pueden cargar datos");
            return valorPorDefecto;
        }

        try
        {
            string resultado;

            #if UNITY_WEBGL && !UNITY_EDITOR
            resultado = cargarDatosJS(clave);
            #else
            resultado = PlayerPrefs.GetString(clave, valorPorDefecto);
            #endif

            if (resultado == null || resultado == "null")
            {
                resultado = valorPorDefecto;
            }

            if (depuracionAlmacenamiento)
            {
                Debug.Log($"[ConfiguracionWebGL] Datos cargados - Clave: {clave}, Valor: {resultado}");
            }

            return resultado;
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfiguracionWebGL] Error al cargar datos: {e.Message}");
            return valorPorDefecto;
        }
    }

    // Método para eliminar datos del almacenamiento
    public void EliminarDatos(string clave)
    {
        if (string.IsNullOrEmpty(clave))
        {
            Debug.LogError("[ConfiguracionWebGL] Error: Clave vacía, no se pueden eliminar datos");
            return;
        }

        try
        {
            #if UNITY_WEBGL && !UNITY_EDITOR
            eliminarDatosJS(clave);
            #else
            PlayerPrefs.DeleteKey(clave);
            PlayerPrefs.Save();
            #endif

            if (depuracionAlmacenamiento)
            {
                Debug.Log($"[ConfiguracionWebGL] Datos eliminados - Clave: {clave}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfiguracionWebGL] Error al eliminar datos: {e.Message}");
        }
    }

    // Método para recibir datos desde JavaScript (llamado por JS)
    public void RecibirDatosJS(string jsonDatos)
    {
        if (mostrarLogsJS)
        {
            Debug.Log($"[ConfiguracionWebGL] Datos recibidos de JS: {jsonDatos}");
        }

        try
        {
            // Notificar a los listeners
            OnDatosRecibidosJS?.Invoke(jsonDatos);
            
            // Buscar GestorFirebase para procesar los datos
            GestorFirebase gestorFirebase = FindObjectOfType<GestorFirebase>();
            if (gestorFirebase != null)
            {
                gestorFirebase.ProcesarDatosDesdeJS(jsonDatos);
            }
            else
            {
                Debug.LogWarning("[ConfiguracionWebGL] No se pudo enviar datos a GestorFirebase");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConfiguracionWebGL] Error al procesar datos JS: {e.Message}");
        }
    }
}