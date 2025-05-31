using UnityEngine;
using Cinemachine;
using System.Collections;

[System.Serializable]
public class ParallaxLayer
{
    public Transform layerTransform;
    [Range(0f, 1f)] public float parallaxEffectX = 0.5f;
    [Range(0f, 1f)] public float parallaxEffectY = 0.25f;
    [HideInInspector] public Vector3 startPos;
    [HideInInspector] public float layerLength;
}

[System.Serializable]
public class BackgroundSet
{
    public string setName;
    public ParallaxLayer[] layers;
    public bool isActive = false;
}

public class MovimientoFondos_02 : MonoBehaviour
{
    [Header("Cámara")]
    public CinemachineVirtualCamera virtualCam;
    
    [Header("Configuración General")]
    [Range(0.01f, 1f)] public float smoothing = 1f;
    
    [Header("Movimiento en Y")]
    public bool enableYMovement = true;
    public float yThreshold = 2.5f;
    
    [Header("Fondos")]
    public ParallaxLayer[] mainBackgroundLayers;
    public BackgroundSet[] backgroundSets;

    [Header("Control de Día/Noche")]
    public bool switchStatus = false;  // Variable para controlar el cambio día/noche
    private bool previousSwitchStatus = false;  // Para detectar cambios
    [Range(0.5f, 3.0f)] public float tiempoTransicion = 1.0f;  // Tiempo de transición entre día y noche
    public bool transicionSuave = true;  // Habilitar transición suave entre día y noche
    
    [Header("Sprite Nocturno")]
    public SpriteNocturno spriteNocturno;  // Referencia al componente SpriteNocturno
    
    private Transform camTransform;  // La cámara siempre determina el movimiento
    private Vector3 lastCamPos;      // Última posición de la cámara

    // Singleton para acceso desde otras clases
    public static MovimientoFondos_02 Instance { get; private set; }

    void Awake()
    {
        // Configurar singleton
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Inicializar referencia de cámara
        if (virtualCam != null)
        {
            camTransform = virtualCam.transform;
        }
        else
        {
            camTransform = Camera.main.transform;
            Debug.LogWarning("MovimientoFondos_02: No se ha asignado una cámara virtual, usando la cámara principal");
        }
        
        lastCamPos = camTransform.position;
        
        // Inicializar capas
        InitializeLayers(mainBackgroundLayers);
        foreach (var bgSet in backgroundSets)
        {
            InitializeLayers(bgSet.layers);
        }
        
        // Activar el primer conjunto por defecto, pero no desactivar el otro
        if (backgroundSets.Length > 0)
        {
            SetInitialVisibility();
        }
        
        // Guardar estado inicial
        previousSwitchStatus = switchStatus;
        
        // Si comienza en modo nocturno, mostrar el sprite nocturno
        if (switchStatus && spriteNocturno != null)
        {
            spriteNocturno.MostrarSprite();
        }
    }

    void InitializeLayers(ParallaxLayer[] layers)
    {
        foreach (var layer in layers)
        {
            if (layer.layerTransform != null)
            {
                layer.startPos = layer.layerTransform.position;
                
                SpriteRenderer sr = layer.layerTransform.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    layer.layerLength = sr.bounds.size.x;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (camTransform == null) return;
        
        // Verificar cambios en switchStatus
        if (switchStatus != previousSwitchStatus)
        {
            ToggleDayNight();
            previousSwitchStatus = switchStatus;
        }
        
        // Calcular movimiento de la cámara
        Vector3 deltaMovement = camTransform.position - lastCamPos;
        
        // Si la cámara se ha movido, actualizar los fondos
        if (deltaMovement != Vector3.zero)
        {
            // Aplicar parallax a las capas principales
            UpdateLayers(mainBackgroundLayers, deltaMovement);
            
            // Aplicar parallax a TODAS las capas de fondos, estén activas o no
            foreach (var bgSet in backgroundSets)
            {
                UpdateLayers(bgSet.layers, deltaMovement);
            }
        }
        
        // Actualizar la última posición conocida
        lastCamPos = camTransform.position;
    }

    void UpdateLayers(ParallaxLayer[] layers, Vector3 deltaMovement)
    {
        foreach (var layer in layers)
        {
            if (layer.layerTransform == null) continue;
            
            // Calcular movimiento parallax
            float parallaxX = deltaMovement.x * layer.parallaxEffectX;
            float parallaxY = enableYMovement ? deltaMovement.y * layer.parallaxEffectY : 0f;
            
            // Aplicar movimiento
            Vector3 newPosition = layer.layerTransform.position;
            newPosition.x += parallaxX;
            if (enableYMovement) newPosition.y += parallaxY;
            
            layer.layerTransform.position = newPosition;
            
            // Implementar bucle infinito si es necesario
            if (layer.layerLength > 0)
            {
                if (Mathf.Abs(camTransform.position.x - layer.layerTransform.position.x) >= layer.layerLength)
                {
                    float offset = (camTransform.position.x - layer.layerTransform.position.x) % layer.layerLength;
                    newPosition.x = camTransform.position.x + offset;
                    layer.layerTransform.position = newPosition;
                }
            }
        }
    }

    // Configuración inicial de visibilidad
    void SetInitialVisibility()
    {
        // Definir qué conjunto debe estar visible al inicio
        for (int i = 0; i < backgroundSets.Length; i++)
        {
            bool shouldBeVisible = (i == 0 && !switchStatus) || (i == 1 && switchStatus);
            backgroundSets[i].isActive = shouldBeVisible;
            
            // Cambiar visibilidad pero no desactivar objetos
            foreach (var layer in backgroundSets[i].layers)
            {
                if (layer.layerTransform != null)
                {
                    // Establecer visibilidad usando el renderer en lugar de activar/desactivar GameObject
                    Renderer renderer = layer.layerTransform.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = shouldBeVisible;
                    }
                }
            }
        }
    }

    // Cambiar entre día y noche
    public void ToggleDayNight()
    {
        // Gestionar el sprite nocturno
        if (spriteNocturno != null)
        {
            if (switchStatus)
            {
                // Si cambiamos a modo nocturno, mostrar el sprite
                spriteNocturno.MostrarSprite();
            }
            else
            {
                // Si cambiamos a modo diurno, ocultar el sprite
                spriteNocturno.OcultarSprite();
            }
        }
        
        // Si hay al menos 2 sets (día y noche)
        if (backgroundSets.Length >= 2)
        {
            if (transicionSuave)
            {
                StartCoroutine(TransicionSuaveDiaNoche());
            }
            else
            {
                CambiarVisibilidadFondos();
            }
        }
    }
    
    // Función para cambiar la visibilidad de los fondos inmediatamente
    private void CambiarVisibilidadFondos()
    {
        for (int i = 0; i < backgroundSets.Length; i++)
        {
            bool shouldBeVisible = (i == 0 && !switchStatus) || (i == 1 && switchStatus);
            backgroundSets[i].isActive = shouldBeVisible;
            
            // Cambiar solo la visibilidad de los fondos, manteniéndolos activos
            foreach (var layer in backgroundSets[i].layers)
            {
                if (layer.layerTransform != null)
                {
                    // Cambiar visibilidad del renderer en lugar del GameObject
                    Renderer renderer = layer.layerTransform.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.enabled = shouldBeVisible;
                    }
                }
            }
        }
    }
    
    // Corrutina para transición suave entre día y noche
    private IEnumerator TransicionSuaveDiaNoche()
    {
        int setActual = switchStatus ? 1 : 0;
        int setAnterior = switchStatus ? 0 : 1;
        
        // Asegurarse de que ambos sets sean visibles para la transición
        foreach (var layer in backgroundSets[setActual].layers)
        {
            if (layer.layerTransform != null)
            {
                SpriteRenderer renderer = layer.layerTransform.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = true;
                    Color color = renderer.color;
                    color.a = 0f;
                    renderer.color = color;
                }
            }
        }
        
        // Asegurarse de que el set anterior esté completamente visible
        foreach (var layer in backgroundSets[setAnterior].layers)
        {
            if (layer.layerTransform != null)
            {
                SpriteRenderer renderer = layer.layerTransform.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = true;
                    Color color = renderer.color;
                    color.a = 1f;
                    renderer.color = color;
                }
            }
        }
        
        float tiempoTranscurrido = 0f;
        
        while (tiempoTranscurrido < tiempoTransicion)
        {
            float alpha = tiempoTranscurrido / tiempoTransicion;
            
            // Desvanecer el conjunto anterior
            foreach (var layer in backgroundSets[setAnterior].layers)
            {
                if (layer.layerTransform != null)
                {
                    SpriteRenderer renderer = layer.layerTransform.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        Color color = renderer.color;
                        color.a = 1f - alpha;
                        renderer.color = color;
                    }
                }
            }
            
            // Aparecer el nuevo conjunto
            foreach (var layer in backgroundSets[setActual].layers)
            {
                if (layer.layerTransform != null)
                {
                    SpriteRenderer renderer = layer.layerTransform.GetComponent<SpriteRenderer>();
                    if (renderer != null)
                    {
                        Color color = renderer.color;
                        color.a = alpha;
                        renderer.color = color;
                    }
                }
            }
            
            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }
        
        // Asegurarse de que el estado final sea correcto
        foreach (var layer in backgroundSets[setAnterior].layers)
        {
            if (layer.layerTransform != null)
            {
                SpriteRenderer renderer = layer.layerTransform.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.enabled = false;
                    Color color = renderer.color;
                    color.a = 1f;  // Restaurar alpha para el próximo uso
                    renderer.color = color;
                }
            }
        }
        
        backgroundSets[setActual].isActive = true;
        backgroundSets[setAnterior].isActive = false;
    }

    // Método público para cambiar entre día y noche desde otras clases
    public void CambiarDiaNoche()
    {
        switchStatus = !switchStatus;
    }
}