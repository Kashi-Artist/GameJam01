// SceneMusicManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(SimpleAudioSystem))]
public class SceneMusicManager : MonoBehaviour
{
    [System.Serializable]
    public class SceneMusicMapping
    {
        public string sceneName;      // Nombre exacto de la escena
        public AudioClip musicToPlay;  // Audio clip que se puede arrastrar directamente al inspector
        [Range(0f, 1f)]
        public float volume = 1f;      // Volumen específico para este clip
    }

    [Header("Configuración de Música por Escena")]
    [Tooltip("Mapeo de escenas a archivos de audio")]
    public List<SceneMusicMapping> sceneMusicMappings = new List<SceneMusicMapping>();

    [Header("Opciones")]
    [Tooltip("Si está activado, se asignará música automáticamente al cambiar de escena")]
    public bool autoChangeMusic = true;

    private SimpleAudioSystem audioSystem;
    private string currentSceneName;

    private void Awake()
    {
        // Obtener referencia al SimpleAudioSystem
        audioSystem = GetComponent<SimpleAudioSystem>();
        
        if (audioSystem == null)
        {
            Debug.LogError("[SceneMusicManager] No se encontró el componente SimpleAudioSystem requerido");
            enabled = false;
            return;
        }

        // Asegurarse de que este objeto persista entre escenas, igual que SimpleAudioSystem
        // Nota: no es necesario llamar a DontDestroyOnLoad aquí ya que SimpleAudioSystem ya lo hace

        // Configurar el SimpleAudioSystem para que persista entre escenas
        audioSystem.persistMusicBetweenScenes = true;
        audioSystem.stopMusicOnSceneChange = false;  // Importante para evitar que se detenga al cambiar de escena
        
        Debug.Log("[SceneMusicManager] Inicializado correctamente");
    }

    private void Start()
    {
        // Obtener la escena actual
        currentSceneName = SceneManager.GetActiveScene().name;
        
        // Reproducir la música correspondiente a la escena inicial
        PlayMusicForCurrentScene();

        // Suscribirse al evento de cambio de escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento al destruir
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Actualizar el nombre de la escena actual
        currentSceneName = scene.name;

        // Necesitamos un breve retraso para asegurar que la escena se ha cargado completamente
        // y que cualquier transición en proceso se haya completado adecuadamente
        Invoke("HandleSceneLoadedMusic", 0.2f);
    }
    
    private void HandleSceneLoadedMusic()
    {
        // Si está habilitado el cambio automático, reproducir la música correspondiente
        if (autoChangeMusic)
        {
            PlayMusicForCurrentScene();
        }
    }

    // Método público para reproducir la música de la escena actual
    public void PlayMusicForCurrentScene()
    {
        SceneMusicMapping mapping = GetMusicMappingForScene(currentSceneName);
        
        if (mapping != null && mapping.musicToPlay != null)
        {
            Debug.Log($"[SceneMusicManager] Reproduciendo música para la escena '{currentSceneName}': {mapping.musicToPlay.name}");
            
            // Usar la mitad del tiempo de fade para las transiciones entre escenas
            float sceneFadeTime = audioSystem.fadeTime * 0.15f;
            SetSceneFadeTime(sceneFadeTime);
            
            audioSystem.PlayMusic(mapping.musicToPlay, mapping.volume);
        }
        else
        {
            Debug.LogWarning($"[SceneMusicManager] No se encontró mapeo de música para la escena '{currentSceneName}' o el AudioClip es nulo");
        }
    }

    // Método para reproducir música para una escena específica
    public void PlayMusicForScene(string sceneName)
    {
        SceneMusicMapping mapping = GetMusicMappingForScene(sceneName);
        
        if (mapping != null && mapping.musicToPlay != null)
        {
            Debug.Log($"[SceneMusicManager] Reproduciendo música para la escena '{sceneName}': {mapping.musicToPlay.name}");
            
            // Usar la mitad del tiempo de fade para las transiciones entre escenas
            float sceneFadeTime = audioSystem.fadeTime * 0.15f;
            SetSceneFadeTime(sceneFadeTime);
            
            audioSystem.PlayMusic(mapping.musicToPlay, mapping.volume);
        }
        else
        {
            Debug.LogWarning($"[SceneMusicManager] No se encontró mapeo de música para la escena '{sceneName}' o el AudioClip es nulo");
        }
    }

    // Obtener el mapeo musical correspondiente a una escena
    private SceneMusicMapping GetMusicMappingForScene(string sceneName)
    {
        foreach (SceneMusicMapping mapping in sceneMusicMappings)
        {
            if (mapping.sceneName == sceneName)
            {
                return mapping;
            }
        }
        
        return null;
    }

    // Método para añadir un nuevo mapeo en tiempo de ejecución
    public void AddSceneMusicMapping(string sceneName, AudioClip musicClip, float volume = 1f)
    {
        // Comprobar si ya existe este mapeo
        bool exists = false;
        foreach (SceneMusicMapping mapping in sceneMusicMappings)
        {
            if (mapping.sceneName == sceneName)
            {
                mapping.musicToPlay = musicClip;
                mapping.volume = volume;
                exists = true;
                break;
            }
        }
        
        // Si no existe, crear uno nuevo
        if (!exists)
        {
            SceneMusicMapping newMapping = new SceneMusicMapping
            {
                sceneName = sceneName,
                musicToPlay = musicClip,
                volume = volume
            };
            
            sceneMusicMappings.Add(newMapping);
        }
        
        Debug.Log($"[SceneMusicManager] Nuevo mapeo añadido: Escena '{sceneName}' -> Audio '{musicClip.name}'");
    }

    // Método para eliminar un mapeo
    public void RemoveSceneMusicMapping(string sceneName)
    {
        for (int i = 0; i < sceneMusicMappings.Count; i++)
        {
            if (sceneMusicMappings[i].sceneName == sceneName)
            {
                sceneMusicMappings.RemoveAt(i);
                Debug.Log($"[SceneMusicManager] Mapeo eliminado para la escena '{sceneName}'");
                return;
            }
        }
    }

    // Implementación alternativa para pausar/reanudar la música actual
    public void PauseMusic()
    {
        audioSystem.PauseMusic();
    }

    public void ResumeMusic()
    {
        audioSystem.ResumeMusic();
    }

    public void StopMusic()
    {
        audioSystem.StopMusic();
    }

    // Ajustar temporalmente el tiempo de fade
    private float originalFadeTime;
    private bool fadeTimeChanged = false;
    
    private void SetSceneFadeTime(float newFadeTime)
    {
        if (!fadeTimeChanged)
        {
            originalFadeTime = audioSystem.fadeTime;
            fadeTimeChanged = true;
        }
        
        audioSystem.fadeTime = newFadeTime;
        
        // Restaurar el tiempo original después de un breve periodo
        // para que solo afecte a esta transición específica
        CancelInvoke("RestoreFadeTime");
        Invoke("RestoreFadeTime", newFadeTime * 1.5f);
    }
    
    private void RestoreFadeTime()
    {
        if (fadeTimeChanged)
        {
            audioSystem.fadeTime = originalFadeTime;
            fadeTimeChanged = false;
        }
    }
}