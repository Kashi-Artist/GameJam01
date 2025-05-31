using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class DatosUsuario
{
    public string nombre;
    public int ultimoPuntaje;
    public int puntajeMaximo;
    public DateTime ultimaPartida;
    public float porcentajeExploracion;
    public DatosUsuario(string nombre)
    {
        this.nombre = nombre;
        this.ultimoPuntaje = 0;
        this.puntajeMaximo = 0;
        this.ultimaPartida = DateTime.Now;
    }
}


[Serializable]
public class ListaUsuarios
{
    public List<DatosUsuario> usuarios = new List<DatosUsuario>();
}

public class GestorUsuarios : MonoBehaviour

{
    public static GestorUsuarios Instancia { get; private set; }

    [Header("Configuración")]
    [Tooltip("Activar para borrar todos los datos guardados al iniciar")]
    public bool modoEjecutarPruebas = false;
    
    [Tooltip("Si está habilitado, siempre pedirá crear o seleccionar usuario al iniciar")]
    public bool pedirSiempreUsuario = true;
    
    [Tooltip("Prefijo único para las claves de almacenamiento")]
    public string prefijoAlmacenamiento = "MiJuego_";

    private string usuarioActual = "";
    private bool inicializacionCompleta = false;

    private ListaUsuarios listaUsuarios = new ListaUsuarios();

    // Añado el prefijo a todas las claves
    private string CLAVE_USUARIO_ACTUAL => $"{prefijoAlmacenamiento}UsuarioActual";
    private string CLAVE_LISTA_USUARIOS => $"{prefijoAlmacenamiento}ListaUsuarios";
    private string CLAVE_PRIMER_USO => $"{prefijoAlmacenamiento}PrimerUso";
    private string CLAVE_FORZAR_REGISTRO => $"{prefijoAlmacenamiento}ForzarRegistro";

    private ConfiguracionWebGL configWebGL;
    private GestorFirebase gestorFirebase;

    public event Action<string> OnCambioUsuario;
    public event Action<int> OnActualizacionPuntaje;
    public event Action OnInicializacionCompleta;

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject);
            inicializacionCompleta = false; 


            configWebGL = FindObjectOfType<ConfiguracionWebGL>();
            if (configWebGL == null)
            {
                GameObject webGLConfig = new GameObject("ConfiguracionWebGL");
                configWebGL = webGLConfig.AddComponent<ConfiguracionWebGL>();
                configWebGL.depuracionAlmacenamiento = true; // Activar depuración para diagnosticar problemas
                DontDestroyOnLoad(webGLConfig);
            }

            if (modoEjecutarPruebas)
            {
                Debug.Log("[GestorUsuarios] Modo de pruebas activado. Borrando todos los datos guardados.");
                EliminarTodosLosDatos();
                configWebGL.GuardarDatos(CLAVE_PRIMER_USO, "true");
            }

            // Buscar o crear el gestor de Firebase
            gestorFirebase = FindObjectOfType<GestorFirebase>();
            if (gestorFirebase == null && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                GameObject firebaseObj = new GameObject("GestorFirebase");
                gestorFirebase = firebaseObj.AddComponent<GestorFirebase>();
                DontDestroyOnLoad(firebaseObj);
            }

            // Subscribirse a eventos de Firebase
            if (gestorFirebase != null)
            {
                gestorFirebase.OnFirebaseInicializado += OnFirebaseInicializado;
                gestorFirebase.OnErrorFirebase += OnErrorFirebase;
            }

            CargarDatos();
            // Si pedirSiempreUsuario está activo, forzar el registro al iniciar
            if (pedirSiempreUsuario)
            {
                configWebGL.GuardarDatos(CLAVE_FORZAR_REGISTRO, "true");
            }

            // Marcar la inicialización como completa y notificar a los oyentes
            inicializacionCompleta = true;
            OnInicializacionCompleta?.Invoke();
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
     // Método para comprobar si la inicialización está completa
    public bool EstaInicializado()
    {
        return inicializacionCompleta;
    }

    private void OnFirebaseInicializado()
    {
        Debug.Log("[GestorUsuarios] Firebase inicializado, intentando cargar datos de usuario actual");
        if (!string.IsNullOrEmpty(usuarioActual) && gestorFirebase != null)
        {
            gestorFirebase.CargarUsuario(usuarioActual);
        }
    }

    private void OnErrorFirebase(string mensaje)
    {
        Debug.LogWarning($"[GestorUsuarios] Error en Firebase: {mensaje}. Usando almacenamiento local como respaldo.");
    }

    public bool EsPrimerUso()
    {
        string valor = configWebGL.CargarDatos(CLAVE_PRIMER_USO, "true");
        return valor == "true";
    }

    public bool DebeRegistrarUsuario()
    {
        // Devuelve true si es primer uso o si se debe forzar el registro
        return EsPrimerUso() || 
               (pedirSiempreUsuario && configWebGL.CargarDatos(CLAVE_FORZAR_REGISTRO, "false") == "true");
    }

    public void MarcarComoUsado()
    {
        configWebGL.GuardarDatos(CLAVE_PRIMER_USO, "false");
        // Desactivar el forzado de registro después de usar
        configWebGL.GuardarDatos(CLAVE_FORZAR_REGISTRO, "false");
    }

    public void EstablecerUsuario(string nombre)
    {
        if (string.IsNullOrEmpty(nombre))
        {
            nombre = "Jugador";
        }
        // Convertir nombre a mayúsculas antes de guardar y buscar
        usuarioActual = nombre.ToUpperInvariant();
        configWebGL.GuardarDatos(CLAVE_USUARIO_ACTUAL, usuarioActual);

        DatosUsuario usuario = ObtenerUsuario(nombre);
        if (usuario == null)
        {
            // Es un usuario nuevo
            usuario = new DatosUsuario(nombre);
            listaUsuarios.usuarios.Add(usuario);
            Debug.Log($"[GestorUsuarios] Usuario nuevo creado: {nombre}");
        }
        else
        {
            Debug.Log($"[GestorUsuarios] Usuario existente seleccionado: {nombre} - Puntaje máximo: {usuario.puntajeMaximo}");
        }
        
        GuardarListaUsuarios();
        
        // Intentar cargar datos de Firebase para este usuario
        if (gestorFirebase != null && gestorFirebase.EstaFirebaseDisponible())
        {
            gestorFirebase.CargarUsuario(usuarioActual);
        }
        
        OnCambioUsuario?.Invoke(usuarioActual);
    }

    public string ObtenerNombreUsuarioActual()
    {
        return usuarioActual;
    }

    public DatosUsuario ObtenerDatosUsuarioActual()
    {
        return ObtenerUsuario(usuarioActual);
    }

    public DatosUsuario ObtenerUsuario(string nombre)
    {
        return listaUsuarios.usuarios.Find(u => u.nombre.ToUpperInvariant() == nombre.ToUpperInvariant());
    }

    public void ActualizarPuntaje(int nuevoPuntaje)
    {
        DatosUsuario usuario = ObtenerDatosUsuarioActual();
        if (usuario != null)
        {
            usuario.ultimoPuntaje = nuevoPuntaje;
            usuario.porcentajeExploracion = nuevoPuntaje / 100f;
            usuario.ultimaPartida = DateTime.Now;

            if (nuevoPuntaje > usuario.puntajeMaximo)
            {
                usuario.puntajeMaximo = nuevoPuntaje;
            }

            GuardarListaUsuarios();
            Debug.Log($"[GestorUsuarios] Puntaje actualizado para {usuario.nombre}: {nuevoPuntaje}/{usuario.puntajeMaximo}");

            // Guardar en Firebase si está disponible
            if (gestorFirebase != null && gestorFirebase.EstaFirebaseDisponible())
            {
                gestorFirebase.GuardarUsuario(usuario);
            }

            OnActualizacionPuntaje?.Invoke(nuevoPuntaje);
        }
        else
        {
            Debug.LogError("[GestorUsuarios] No se pudo actualizar puntaje: Usuario actual no encontrado");
        }
    }

    // Método para actualizar datos desde Firebase
    public void ActualizarDatosDesdeFirebase(DatosUsuario datosFirebase)
    {
        if (datosFirebase == null) return;
        
        DatosUsuario usuarioLocal = ObtenerUsuario(datosFirebase.nombre);
        
        if (usuarioLocal != null)
        {
            // Actualizar solo si los datos de Firebase son mejores que los locales
            if (datosFirebase.puntajeMaximo > usuarioLocal.puntajeMaximo)
            {
                usuarioLocal.puntajeMaximo = datosFirebase.puntajeMaximo;
                usuarioLocal.ultimoPuntaje = datosFirebase.ultimoPuntaje;
                usuarioLocal.porcentajeExploracion = datosFirebase.porcentajeExploracion;
                
                // La fecha puede ser problemática en la serialización, así que la mantenemos local
                usuarioLocal.ultimaPartida = DateTime.Now;
                
                Debug.Log($"[GestorUsuarios] Datos actualizados desde Firebase para {usuarioLocal.nombre}");
                GuardarListaUsuarios();
                
                if (usuarioLocal.nombre.ToUpperInvariant() == usuarioActual.ToUpperInvariant())
                {
                    OnActualizacionPuntaje?.Invoke(usuarioLocal.ultimoPuntaje);
                }
            }
        }
        else
        {
            // Si no existe, añadirlo a la lista local
            DatosUsuario nuevoUsuario = new DatosUsuario(datosFirebase.nombre)
            {
                ultimoPuntaje = datosFirebase.ultimoPuntaje,
                puntajeMaximo = datosFirebase.puntajeMaximo,
                porcentajeExploracion = datosFirebase.porcentajeExploracion,
                ultimaPartida = DateTime.Now
            };
            
            listaUsuarios.usuarios.Add(nuevoUsuario);
            Debug.Log($"[GestorUsuarios] Usuario nuevo añadido desde Firebase: {nuevoUsuario.nombre}");
            GuardarListaUsuarios();
        }
    }

    public List<DatosUsuario> ObtenerListaUsuariosOrdenada()
    {
        List<DatosUsuario> listaOrdenada = new List<DatosUsuario>(listaUsuarios.usuarios);
        listaOrdenada.Sort((a, b) => b.puntajeMaximo.CompareTo(a.puntajeMaximo));
        return listaOrdenada;
    }

    private void CargarDatos()
    {
        try
        {
            usuarioActual = configWebGL.CargarDatos(CLAVE_USUARIO_ACTUAL, "");
            Debug.Log($"[GestorUsuarios] Usuario actual cargado: {usuarioActual}");

            string jsonUsuarios = configWebGL.CargarDatos(CLAVE_LISTA_USUARIOS, "");
            Debug.Log($"[GestorUsuarios] JSON cargado: {(string.IsNullOrEmpty(jsonUsuarios) ? "vacío" : "datos encontrados")}");
            
            if (!string.IsNullOrEmpty(jsonUsuarios))
            {
                listaUsuarios = JsonUtility.FromJson<ListaUsuarios>(jsonUsuarios);
                Debug.Log($"[GestorUsuarios] Datos cargados: {listaUsuarios.usuarios.Count} usuarios encontrados");
            }
            else
            {
                listaUsuarios = new ListaUsuarios();
                Debug.Log("[GestorUsuarios] No se encontraron datos de usuarios, se inició una lista vacía");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[GestorUsuarios] Error al cargar datos: " + e.Message);
            listaUsuarios = new ListaUsuarios();
        }
    }

    private void GuardarListaUsuarios()
    {
        try
        {
            string jsonUsuarios = JsonUtility.ToJson(listaUsuarios);
            configWebGL.GuardarDatos(CLAVE_LISTA_USUARIOS, jsonUsuarios);
            Debug.Log($"[GestorUsuarios] Datos guardados: {listaUsuarios.usuarios.Count} usuarios");
            
            // Verificación de guardado
            string verificacion = configWebGL.CargarDatos(CLAVE_LISTA_USUARIOS, "");
            if (string.IsNullOrEmpty(verificacion))
            {
                Debug.LogError("[GestorUsuarios] ADVERTENCIA: La verificación de guardado falló. No se encontraron datos después de guardar.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[GestorUsuarios] Error al guardar lista de usuarios: " + e.Message);
        }
    }

    public void EliminarTodosLosDatos()
    {
        configWebGL.EliminarDatos(CLAVE_USUARIO_ACTUAL);
        configWebGL.EliminarDatos(CLAVE_LISTA_USUARIOS);
        configWebGL.EliminarDatos(CLAVE_PRIMER_USO);
        configWebGL.EliminarDatos(CLAVE_FORZAR_REGISTRO);

        usuarioActual = "";
        listaUsuarios = new ListaUsuarios();
        
        // También eliminar datos de Firebase si está disponible
        if (gestorFirebase != null && gestorFirebase.EstaFirebaseDisponible())
        {
            gestorFirebase.EliminarUsuario(usuarioActual);
        }
        
        Debug.Log("[GestorUsuarios] Todos los datos han sido eliminados");
    }

    // Método para reiniciar los datos desde el inspector o por código
    public void ReiniciarDatos()
    {
        EliminarTodosLosDatos();
        configWebGL.GuardarDatos(CLAVE_PRIMER_USO, "true");
        configWebGL.GuardarDatos(CLAVE_FORZAR_REGISTRO, "true");
        Debug.Log("[GestorUsuarios] Datos reiniciados, se forzará el registro en la próxima sesión");
    }

    // Método para forzar que se pida un usuario en la próxima sesión
    public void ForzarRegistroProximaSesion()
    {
        configWebGL.GuardarDatos(CLAVE_FORZAR_REGISTRO, "true");
        Debug.Log("[GestorUsuarios] Se forzará el registro en la próxima sesión");
    }
}