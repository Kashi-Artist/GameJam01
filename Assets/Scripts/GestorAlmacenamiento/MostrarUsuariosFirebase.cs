using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MostrarUsuariosFirebase : MonoBehaviour
{
    [Header("Configuración")]
    public bool mostrarEnConsola = true;
    public bool mostrarUsuariosLocales = true;
    public bool mostrarRankingGlobal = true;

    private GestorFirebase gestorFirebase;
    private GestorUsuarios gestorUsuarios;

    private void Start()
    {
        // Obtener referencias a los gestores
        gestorFirebase = FindObjectOfType<GestorFirebase>();
        gestorUsuarios = FindObjectOfType<GestorUsuarios>();

        if (gestorFirebase == null || gestorUsuarios == null)
        {
            Debug.LogError("No se encontraron los gestores necesarios");
            return;
        }

        // Suscribirse a eventos de Firebase
        gestorFirebase.OnFirebaseInicializado += OnFirebaseInicializado;
        gestorFirebase.OnErrorFirebase += OnErrorFirebase;
        gestorFirebase.OnRankingCargado += OnRankingCargado; // ¡Esta línea es crucial!
    }
    
    private void OnFirebaseInicializado()
    {
        Debug.Log("Firebase inicializado correctamente");
        
        if (mostrarRankingGlobal)
        {
            gestorFirebase.CargarRanking();
        }
        
        if (mostrarUsuariosLocales)
        {
            MostrarUsuariosLocales();
        }
    }

    private void OnErrorFirebase(string mensaje)
    {
        Debug.LogError("Error de Firebase: " + mensaje);
        
        if (mostrarUsuariosLocales)
        {
            MostrarUsuariosLocales();
        }
    }

    private void OnRankingCargado(List<RankingItem> ranking)
    {
        if (mostrarRankingGlobal && ranking != null)
        {
            string informacion = "=== RANKING GLOBAL (FIREBASE) ===\n";
            informacion += $"Total de usuarios: {ranking.Count}\n\n";

            for (int i = 0; i < ranking.Count; i++)
            {
                var usuario = ranking[i];
                informacion += $"{i + 1}. {usuario.nombre}\n";
                informacion += $"   Puntaje máximo: {usuario.puntajeMaximo}\n";
                informacion += $"   Última actualización: {usuario.ultimaActualizacion}\n\n";
            }

            if (mostrarEnConsola)
            {
                Debug.Log(informacion);
            }
        }
    }

    private void MostrarUsuariosLocales()
    {
        List<DatosUsuario> usuarios = gestorUsuarios.ObtenerListaUsuariosOrdenada();
        
        string informacion = "=== USUARIOS LOCALES ===\n";
        informacion += $"Total de usuarios: {usuarios.Count}\n\n";

        for (int i = 0; i < usuarios.Count; i++)
        {
            var usuario = usuarios[i];
            informacion += $"{i + 1}. {usuario.nombre}\n";
            informacion += $"   Puntaje máximo: {usuario.puntajeMaximo}\n";
            informacion += $"   Último puntaje: {usuario.ultimoPuntaje}\n";
            informacion += $"   Porcentaje exploración: {usuario.porcentajeExploracion:P}\n";
            informacion += $"   Última partida: {usuario.ultimaPartida}\n\n";
        }

        if (mostrarEnConsola)
        {
            Debug.Log(informacion);
        }
    }

    // Método para probar manualmente
    [ContextMenu("Mostrar Usuarios Ahora")]
    public void MostrarUsuariosAhora()
    {
        if (gestorFirebase.EstaFirebaseDisponible())
        {
            gestorFirebase.CargarRanking();
        }
        MostrarUsuariosLocales();
    }

    private void OnDestroy()
    {
        // Limpiar suscripciones a eventos
        if (gestorFirebase != null)
        {
            gestorFirebase.OnFirebaseInicializado -= OnFirebaseInicializado;
            gestorFirebase.OnErrorFirebase -= OnErrorFirebase;
            gestorFirebase.OnRankingCargado -= OnRankingCargado;
        }
    }
}