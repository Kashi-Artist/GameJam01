using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public class RankingItem
{
    public string nombre;
    public int puntajeMaximo;
    public string ultimaActualizacion;
}

[Serializable]
public class RankingData
{
    public string evento;
    public List<RankingItem> datos = new List<RankingItem>();
}

[Serializable]
public class UsuarioData
{
    public string evento;
    public DatosUsuario datos;
}

[Serializable]
public class EventoFirebase
{
    public string evento;
    public string estado;
    public string mensaje;
}

public class GestorFirebase : MonoBehaviour
{
    public static GestorFirebase Instancia { get; private set; }

    [Header("Configuración")]
    [Tooltip("Activar para usar Firebase en lugar de almacenamiento local")]
    public bool usarFirebase = true;

    [Tooltip("Tiempo de espera para la inicialización de Firebase (segundos)")]
    public float tiempoEsperaInicializacion = 5f;

    private bool firebaseInicializado = false;
    private ConfiguracionWebGL configWebGL;
    private GestorUsuarios gestorUsuarios;

    public event Action<List<RankingItem>> OnRankingCargado;
    public event Action OnFirebaseInicializado;
    public event Action<string> OnErrorFirebase;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern int inicializarFirebase();

    [DllImport("__Internal")]
    private static extern int guardarUsuarioFirebase(string jsonUsuario);

    // Estas dos no devuelven una cadena sino un entero (0/1), la respuesta real llega por JS -> SendMessage
    [DllImport("__Internal")]
    private static extern int cargarUsuarioFirebase(string nombreUsuario);

    [DllImport("__Internal")]
    private static extern int cargarRankingFirebase();

    [DllImport("__Internal")]
    private static extern int eliminarUsuarioFirebase(string nombreUsuario);

    [DllImport("__Internal")]
    private static extern void verificarObjetosFirebaseJS(
        out int firebaseExists,
        out int firebaseAppExists,
        out int firebaseAuthExists,
        out int firebaseDatabaseExists);
#endif

    public void VerificarEstadoFirebase()
    {
        #if UNITY_WEBGL && !UNITY_EDITOR
        int existsInt, appInt, authInt, dbInt;
        verificarObjetosFirebaseJS(
            out existsInt,
            out appInt,
            out authInt,
            out dbInt
        );
        bool firebaseExists         = existsInt != 0;
        bool firebaseAppExists      = appInt    != 0;
        bool firebaseAuthExists     = authInt   != 0;
        bool firebaseDatabaseExists = dbInt     != 0;

        Debug.Log($"[GestorFirebase] Verificación de objetos: " +
                $"Firebase={firebaseExists}, " +
                $"App={firebaseAppExists}, " +
                $"Auth={firebaseAuthExists}, " +
                $"Database={firebaseDatabaseExists}");
        #endif
    }

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);

            configWebGL = FindObjectOfType<ConfiguracionWebGL>();
            if (configWebGL == null)
            {
                Debug.LogError("[GestorFirebase] No se encontró ConfiguracionWebGL");
                return;
            }

            gestorUsuarios = FindObjectOfType<GestorUsuarios>();
            if (gestorUsuarios == null)
            {
                Debug.LogError("[GestorFirebase] No se encontró GestorUsuarios");
                return;
            }

            if (usarFirebase)
            {
                StartCoroutine(InicializarFirebaseConTimeout());
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator InicializarFirebaseConTimeout()
    {
        Debug.Log("[GestorFirebase] Inicializando Firebase...");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        // Verificar estado actual antes de intentar inicializar
        VerificarEstadoFirebase();
        
        // Llamar a la función de inicialización
        inicializarFirebase();
        
        // Esperar respuesta asíncrona
        float tiempoEspera = 0f;
        while (!firebaseInicializado && tiempoEspera < tiempoEsperaInicializacion)
        {
            yield return new WaitForSeconds(0.5f);
            tiempoEspera += 0.5f;
            Debug.Log($"[GestorFirebase] Esperando inicialización... {tiempoEspera}s");
            
            // Verificar estado periódicamente
            if (tiempoEspera % 2 == 0) // Cada 2 segundos
            {
                VerificarEstadoFirebase();
            }
        }
        #else
        // Simulación para editor
        yield return new WaitForSeconds(1f);
        ProcesarDatosDesdeJS(JsonUtility.ToJson(new EventoFirebase {
            evento = "firebase_inicializado",
            estado = "correcto",
            mensaje = "Simulación en editor"
        }));
        #endif

        if (!firebaseInicializado)
        {
            Debug.LogWarning("[GestorFirebase] Tiempo de espera agotado para inicializar Firebase");
            OnErrorFirebase?.Invoke("Tiempo de espera agotado para inicializar Firebase");
        }
        
    }

    // Método para procesar datos recibidos desde JavaScript
    public void ProcesarDatosDesdeJS(string jsonDatos)
    {
        try
        {
            Debug.Log($"[GestorFirebase] Datos recibidos: {jsonDatos}");

             if (jsonDatos.Contains("ranking_global"))
            {
                RankingData rankingData = JsonUtility.FromJson<RankingData>(jsonDatos);
                Debug.Log($"[GestorFirebase] Ranking cargado - Total usuarios: {rankingData.datos?.Count ?? 0}");
                
                if (rankingData.datos != null)
                {
                    foreach (var user in rankingData.datos)
                    {
                        Debug.Log($"[Usuario] {user.nombre} - Puntaje: {user.puntajeMaximo}");
                    }
                }
            }
            // Detectar el tipo de evento
            if (jsonDatos.Contains("firebase_inicializado"))
            {
                EventoFirebase evento = JsonUtility.FromJson<EventoFirebase>(jsonDatos);
                if (evento.estado == "correcto")
                {
                    firebaseInicializado = true;
                    Debug.Log("[GestorFirebase] Firebase inicializado correctamente");
                    OnFirebaseInicializado?.Invoke();

                    // Cargar ranking inmediatamente
                    CargarRanking();
                }
            }
            else if (jsonDatos.Contains("firebase_error"))
            {
                EventoFirebase evento = JsonUtility.FromJson<EventoFirebase>(jsonDatos);
                Debug.LogError($"[GestorFirebase] Error: {evento.mensaje}");
                OnErrorFirebase?.Invoke(evento.mensaje);
            }
            else if (jsonDatos.Contains("ranking_global"))
            {
                RankingData rankingData = JsonUtility.FromJson<RankingData>(jsonDatos);
                Debug.Log($"[GestorFirebase] Ranking cargado: {rankingData.datos.Count} elementos");
                OnRankingCargado?.Invoke(rankingData.datos);
            }
            else if (jsonDatos.Contains("datos_usuario"))
            {
                UsuarioData usuarioData = JsonUtility.FromJson<UsuarioData>(jsonDatos);
                if (usuarioData.datos != null)
                {
                    Debug.Log($"[GestorFirebase] Datos de usuario cargados: {usuarioData.datos.nombre}");
                    gestorUsuarios.ActualizarDatosDesdeFirebase(usuarioData.datos);
                }
            }
            else if (jsonDatos.Contains("ranking_global"))
            {
                RankingData rankingData = JsonUtility.FromJson<RankingData>(jsonDatos);
                Debug.Log($"[GestorFirebase] Ranking cargado: {rankingData.datos.Count} elementos con nombres: ");
                OnRankingCargado?.Invoke(rankingData.datos);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[GestorFirebase] Error al procesar datos JS: {e.Message}");
        }
    }

    public void GuardarUsuario(DatosUsuario usuario)
    {
        if (!usarFirebase || !firebaseInicializado)
        {
            Debug.Log("[GestorFirebase] Firebase no disponible, utilizando almacenamiento local");
            return;
        }

        try
        {
            string jsonUsuario = JsonUtility.ToJson(usuario);
            Debug.Log($"[GestorFirebase] Guardando usuario en Firebase: {usuario.nombre}");
            
            #if UNITY_WEBGL && !UNITY_EDITOR
            guardarUsuarioFirebase(jsonUsuario);
            #else
            Debug.Log("[GestorFirebase] Simulación: Usuario guardado en Firebase");
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[GestorFirebase] Error al guardar usuario: {e.Message}");
        }
    }

    public void CargarUsuario(string nombreUsuario)
    {
        if (!usarFirebase || !firebaseInicializado)
        {
            Debug.Log("[GestorFirebase] Firebase no disponible, utilizando almacenamiento local");
            return;
        }

        Debug.Log($"[GestorFirebase] Cargando usuario desde Firebase: {nombreUsuario}");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        cargarUsuarioFirebase(nombreUsuario);
        #else
        Debug.Log("[GestorFirebase] Simulación: Datos de usuario cargados desde Firebase");
        #endif
    }

    public void CargarRanking()
    {
        if (!usarFirebase || !firebaseInicializado)
        {
            Debug.Log("[GestorFirebase] Firebase no disponible, utilizando ranking local");
            // Usar el ranking local
            List<RankingItem> rankingLocal = new List<RankingItem>();
            foreach (var usuario in gestorUsuarios.ObtenerListaUsuariosOrdenada())
            {
                rankingLocal.Add(new RankingItem
                {
                    nombre = usuario.nombre,
                    puntajeMaximo = usuario.puntajeMaximo,
                    ultimaActualizacion = usuario.ultimaPartida.ToString()
                });
            }
            OnRankingCargado?.Invoke(rankingLocal);
            return;
        }

        Debug.Log("[GestorFirebase] Cargando ranking desde Firebase");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        cargarRankingFirebase();
        #else
        Debug.Log("[GestorFirebase] Simulación: Ranking cargado desde Firebase");
        List<RankingItem> rankingSimulado = new List<RankingItem>();
        rankingSimulado.Add(new RankingItem { nombre = "Jugador1", puntajeMaximo = 1000, ultimaActualizacion = DateTime.Now.ToString() });
        rankingSimulado.Add(new RankingItem { nombre = "Jugador2", puntajeMaximo = 850, ultimaActualizacion = DateTime.Now.ToString() });
        rankingSimulado.Add(new RankingItem { nombre = "Jugador3", puntajeMaximo = 720, ultimaActualizacion = DateTime.Now.ToString() });
        OnRankingCargado?.Invoke(rankingSimulado);
        #endif
    }

    public void EliminarUsuario(string nombreUsuario)
    {
        if (!usarFirebase || !firebaseInicializado)
        {
            Debug.Log("[GestorFirebase] Firebase no disponible, utilizando almacenamiento local");
            return;
        }

        Debug.Log($"[GestorFirebase] Eliminando usuario de Firebase: {nombreUsuario}");
        
        #if UNITY_WEBGL && !UNITY_EDITOR
        eliminarUsuarioFirebase(nombreUsuario);
        #else
        Debug.Log("[GestorFirebase] Simulación: Usuario eliminado de Firebase");
        #endif
    }

    public bool EstaFirebaseDisponible()
    {
        return usarFirebase && firebaseInicializado;
    }
}