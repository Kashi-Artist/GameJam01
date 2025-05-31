// MusicZone.cs
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MusicZone : MonoBehaviour
{
    [Header("Configuración de zona")]
    public string zoneName;
    
    [Header("Configuración Direct Play (Opcional)")]
    [Tooltip("Para reproducir un clip directamente sin usar las zonas del SimpleAudioSystem")]
    public AudioClip directPlayClip;
    [Range(0f, 1f)]
    public float volumeScale = 1f;
    
    private void Awake()
    {
        // Verificar que el collider esté configurado como trigger
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            Debug.LogWarning("[MusicZone] El Collider2D debe estar configurado como Trigger. Ajustando automáticamente.");
            col.isTrigger = true;
        }
    }
    
    private void Start()
    {
        // Verificaciones iniciales
        if (SimpleAudioSystem.Instance == null)
        {
            Debug.LogError("[MusicZone] No se encontró SimpleAudioSystem en la escena. Asegúrate que existe un GameObject con este componente.");
            return;
        }

        // Validar configuración
        if (string.IsNullOrEmpty(zoneName) && directPlayClip == null)
        {
            Debug.LogError("[MusicZone] Configuración incompleta: se requiere zoneName o directPlayClip");
            return;
        }
        
        // Log de configuración
        string playMethod = !string.IsNullOrEmpty(zoneName) ? "Zona: " + zoneName : "Clip directo: " + directPlayClip.name;
        Debug.Log("[MusicZone] Zona de música inicializada - " + playMethod);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Verificar si es el jugador
        if (other.CompareTag("Player"))
        {
            if (SimpleAudioSystem.Instance == null)
            {
                Debug.LogError("[MusicZone] SimpleAudioSystem no está disponible");
                return;
            }
            
            Debug.Log("[MusicZone] Jugador entró en la zona: " + gameObject.name);
            
            // Determinar qué método usar para reproducir música
            if (!string.IsNullOrEmpty(zoneName))
            {
                // Reproducir por zona
                SimpleAudioSystem.Instance.EnterMusicZone(zoneName);
            }
            else if (directPlayClip != null)
            {
                // Reproducir directamente el clip
                SimpleAudioSystem.Instance.PlayMusic(directPlayClip, volumeScale);
            }
        }
    }
    
    // Método para visualizar la zona en el editor
    private void OnDrawGizmos()
    {
        // Visualizar la zona con un color diferente según el tipo
        Color zoneColor = !string.IsNullOrEmpty(zoneName) ? 
            new Color(0.3f, 0.8f, 0.4f, 0.3f) : // Verde para zonas por nombre
            new Color(0.8f, 0.3f, 0.4f, 0.3f);  // Rojo para zonas de reproducción directa
            
        Gizmos.color = zoneColor;
        
        // Dibujar una representación visual según el tipo de collider
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            if (col is BoxCollider2D boxCol)
            {
                // Dibujar caja para BoxCollider2D
                Vector3 size = new Vector3(
                    boxCol.size.x * transform.lossyScale.x, 
                    boxCol.size.y * transform.lossyScale.y,
                    0.1f
                );
                Vector3 center = transform.TransformPoint(boxCol.offset);
                Gizmos.DrawCube(center, size);
                
                // Dibujar contorno
                Gizmos.color = Color.white;
                Gizmos.DrawWireCube(center, size);
            }
        }
    }
}