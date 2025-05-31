// SimpleAudioSystem.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleAudioSystem : MonoBehaviour
{
    // Referencia estática para acceso global fácil
    public static SimpleAudioSystem Instance { get; private set; }

    [System.Serializable]
    public class MusicTrack
    {
        public string zoneName;
        public AudioClip clip;
        [Range(0f, 1f)]
        public float volume = 1f;
    }
    [Header("Sonido de Botón")]
    public AudioClip buttonClickSound;

    [Header("Configuración")]
    [Range(0f, 1f)]
    public float masterVolume = 1f;
    public float fadeTime = 1.0f;
    public List<MusicTrack> musicTracks = new List<MusicTrack>();
    
    [Header("Configuración de Escena")]
    [Tooltip("Si está activado, la música continuará entre escenas")]
    public bool persistMusicBetweenScenes = false;
    [Tooltip("Si está activado, se detendrá la música al cambiar de escena. Si no, solo se pausará")]
    public bool stopMusicOnSceneChange = true;

    [Header("Referencias")]
    // Usamos dos fuentes para hacer crossfade
    public AudioSource musicSource1;
    public AudioSource musicSource2;
    public AudioSource sfxSource;

    // Tracking
    private string currentZone = "";
    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private bool isFading = false;
    private string lastScene = "";
    
    private void Awake()
    {
        // Configuración singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SimpleAudioSystem] Inicializado correctamente");

            // Suscribirse al evento de cambio de escena
            SceneManager.sceneLoaded += OnSceneLoaded;
            
            // Guardar la escena inicial
            lastScene = SceneManager.GetActiveScene().name;
        }
        else
        {
            Debug.Log("[SimpleAudioSystem] Ya existe una instancia. Destruyendo este objeto.");
            Destroy(gameObject);
            return;
        }

        // Configurar fuentes de audio si no existen
        ConfigureAudioSources();
    }

    private void ConfigureAudioSources()
    {
        // Crear musicSource1 si no existe
        if (musicSource1 == null)
        {
            GameObject sourceObj1 = new GameObject("Music Source 1");
            sourceObj1.transform.SetParent(transform);
            musicSource1 = sourceObj1.AddComponent<AudioSource>();
            musicSource1.playOnAwake = false;
            musicSource1.loop = true;
        }

        // Crear musicSource2 si no existe
        if (musicSource2 == null)
        {
            GameObject sourceObj2 = new GameObject("Music Source 2");
            sourceObj2.transform.SetParent(transform);
            musicSource2 = sourceObj2.AddComponent<AudioSource>();
            musicSource2.playOnAwake = false;
            musicSource2.loop = true;
            musicSource2.volume = 0f; // Inicialmente silenciado
        }

        // Crear sfxSource si no existe
        if (sfxSource == null)
        {
            GameObject sfxObj = new GameObject("SFX Source");
            sfxObj.transform.SetParent(transform);
            sfxSource = sfxObj.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        // Configuración inicial
        activeSource = musicSource1;
        inactiveSource = musicSource2;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Al cargar una nueva escena
        string newSceneName = scene.name;
        Debug.Log("[SimpleAudioSystem] Nueva escena cargada: " + newSceneName);

        // Verificar si la escena ha cambiado
        if (newSceneName != lastScene)
        {
            // Solo hacer algo si la escena es diferente
            lastScene = newSceneName;
            HandleSceneChange();
        }
    }

    // Método para manejar el cambio de escena
    private void HandleSceneChange()
    {
        // Si no queremos que la música persista entre escenas
        if (!persistMusicBetweenScenes)
        {
            if (stopMusicOnSceneChange)
            {
                // Detener completamente la música con fade out
                Debug.Log("[SimpleAudioSystem] Deteniendo música por cambio de escena");
                StopMusic();
            }
            else
            {
                // Solo pausar la música (más óptimo para reanudar después)
                Debug.Log("[SimpleAudioSystem] Pausando música por cambio de escena");
                PauseMusic();
            }
        }
        else
        {
            Debug.Log("[SimpleAudioSystem] La música continúa durante el cambio de escena");
        }
    }

    private void OnDestroy()
    {
        // Desuscribirse del evento al destruir
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Método para reproducir un efecto de sonido
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        
        sfxSource.PlayOneShot(clip, masterVolume * volumeScale);
    }

    // Método principal para cambiar de zona musical
    public void EnterMusicZone(string zoneName)
    {
        // Si ya estamos en esta zona, no hacer nada
        if (zoneName == currentZone) return;

        Debug.Log("[SimpleAudioSystem] Cambiando a zona musical: " + zoneName);
        currentZone = zoneName;

        // Buscar la pista correspondiente
        MusicTrack track = FindMusicTrack(zoneName);
        if (track == null)
        {
            Debug.LogWarning("[SimpleAudioSystem] No se encontró la pista para la zona: " + zoneName);
            return;
        }

        // Iniciar transición
        StartCoroutine(FadeMusicCrossfade(track.clip, track.volume * masterVolume));
    }

    // Método alternativo que toma directamente un AudioClip
    public void PlayMusic(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null) return;
        
        Debug.Log("[SimpleAudioSystem] Reproduciendo clip directamente: " + clip.name);
        StartCoroutine(FadeMusicCrossfade(clip, volumeScale * masterVolume));
    }

    // Método para buscar una pista por nombre de zona
    private MusicTrack FindMusicTrack(string zoneName)
    {
        foreach (MusicTrack track in musicTracks)
        {
            if (track.zoneName == zoneName)
            {
                return track;
            }
        }
        return null;
    }

    // Corrutina para crossfade entre fuentes de audio
    private System.Collections.IEnumerator FadeMusicCrossfade(AudioClip newClip, float targetVolume)
    {
        // Si ya estamos en transición, no iniciar otra
        if (isFading)
        {
            Debug.Log("[SimpleAudioSystem] Ya hay una transición en curso. Esperando...");
            yield return null;
        }

        isFading = true;
        
        // Preparar la fuente inactiva con el nuevo clip
        inactiveSource.clip = newClip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        // Realizar el crossfade
        float elapsedTime = 0f;
        float startVolumeActive = activeSource.volume;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / fadeTime;
            
            // Fade out de la fuente activa
            activeSource.volume = Mathf.Lerp(startVolumeActive, 0f, t);
            
            // Fade in de la fuente inactiva
            inactiveSource.volume = Mathf.Lerp(0f, targetVolume, t);
            
            yield return null;
        }

        // Finalizar la transición
        activeSource.Stop();
        activeSource.clip = null;
        
        // Intercambiar las fuentes
        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
        
        // Asegurar volúmenes finales correctos
        activeSource.volume = targetVolume;
        inactiveSource.volume = 0f;
        
        Debug.Log("[SimpleAudioSystem] Transición completada a: " + newClip.name);
        isFading = false;
    }

    // Actualizar el volumen maestro
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        
        // Actualizar la fuente activa
        if (activeSource != null && activeSource.clip != null)
        {
            // Buscar el track actual para obtener su volumen específico
            float trackVolume = 1f;
            foreach (MusicTrack track in musicTracks)
            {
                if (track.clip == activeSource.clip)
                {
                    trackVolume = track.volume;
                    break;
                }
            }
            activeSource.volume = trackVolume * masterVolume;
        }
        
        Debug.Log("[SimpleAudioSystem] Volumen maestro actualizado: " + masterVolume);
    }

    // Silenciar/activar toda la música
    public void SetMusicMuted(bool muted)
    {
        if (activeSource != null)
        {
            activeSource.mute = muted;
        }
        if (inactiveSource != null)
        {
            inactiveSource.mute = muted;
        }
    }

    // Método para detener toda la música
    public void StopMusic()
    {
        StartCoroutine(FadeOut());
    }

    // Método para pausar la música (se puede reanudar más tarde)
    public void PauseMusic()
    {
        if (activeSource != null && activeSource.isPlaying)
        {
            activeSource.Pause();
            Debug.Log("[SimpleAudioSystem] Música pausada");
        }
        
        if (inactiveSource != null && inactiveSource.isPlaying)
        {
            inactiveSource.Pause();
        }
    }

    // Método para reanudar la música pausada
    public void ResumeMusic()
    {
        if (activeSource != null && !activeSource.isPlaying && activeSource.clip != null)
        {
            activeSource.UnPause();
            Debug.Log("[SimpleAudioSystem] Música reanudada");
        }
    }

    private System.Collections.IEnumerator FadeOut()
    {
        if (activeSource == null || !activeSource.isPlaying) yield break;
        
        float startVolume = activeSource.volume;
        float elapsedTime = 0f;
        
        while (elapsedTime < fadeTime)
        {
            elapsedTime += Time.deltaTime;
            activeSource.volume = Mathf.Lerp(startVolume, 0f, elapsedTime / fadeTime);
            yield return null;
        }
        
        activeSource.Stop();
        activeSource.clip = null;
        currentZone = "";
    }

    // Método para reproducir un sonido de botón
    public void PlayButtonSound()
    {
        if (sfxSource != null && buttonClickSound != null)
        {
            sfxSource.PlayOneShot(buttonClickSound, masterVolume);
        }
    }
}