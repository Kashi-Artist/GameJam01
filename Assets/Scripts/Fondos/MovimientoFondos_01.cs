/* using UnityEngine;
using Cinemachine;

public class MovimientoFondos_01 : MonoBehaviour
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
    
    private Transform cam;
    private Vector3 lastCamPos;

    void Start()
    {
        // Inicializar cámara
        if (virtualCam != null) cam = virtualCam.transform;
        if (cam == null) cam = Camera.main.transform;
        
        lastCamPos = cam.position;
        
        // Inicializar capas
        InitializeLayers(mainBackgroundLayers);
        foreach (var bgSet in backgroundSets)
        {
            InitializeLayers(bgSet.layers);
        }
        
        // Activar el primer conjunto por defecto
        if (backgroundSets.Length > 0)
        {
            SetActiveBackgroundSet(0);
        }
        
        // Guardar estado inicial
        previousSwitchStatus = switchStatus;
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
        if (cam == null) return;
        
        // Verificar cambios en switchStatus
        if (switchStatus != previousSwitchStatus)
        {
            ToggleDayNight();
            previousSwitchStatus = switchStatus;
        }
        
        // Calcular movimiento
        Vector3 deltaMovement = cam.position - lastCamPos;
        
        // Aplicar parallax a las capas principales
        UpdateLayers(mainBackgroundLayers, deltaMovement);
        
        // Aplicar parallax a capas de fondos alternativos activos
        foreach (var bgSet in backgroundSets)
        {
            if (bgSet.isActive)
            {
                UpdateLayers(bgSet.layers, deltaMovement);
            }
        }
        
        lastCamPos = cam.position;
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
                if (Mathf.Abs(cam.position.x - layer.layerTransform.position.x) >= layer.layerLength)
                {
                    float offset = (cam.position.x - layer.layerTransform.position.x) % layer.layerLength;
                    newPosition.x = cam.position.x + offset;
                    layer.layerTransform.position = newPosition;
                }
            }
        }
    }

    void SetActiveBackgroundSet(int index)
    {
        if (index < 0 || index >= backgroundSets.Length) return;
        
        // Desactivar todos los sets
        for (int i = 0; i < backgroundSets.Length; i++)
        {
            bool activate = (i == index);
            backgroundSets[i].isActive = activate;
            
            foreach (var layer in backgroundSets[i].layers)
            {
                if (layer.layerTransform != null)
                {
                    layer.layerTransform.gameObject.SetActive(activate);
                }
            }
        }
    }

    void ToggleDayNight()
    {
        // Si hay al menos 2 sets (día y noche)
        if (backgroundSets.Length >= 2)
        {
            // switchStatus true = noche (índice 1), false = día (índice 0)
            int index = switchStatus ? 1 : 0;
            SetActiveBackgroundSet(index);
        }
    }
} */