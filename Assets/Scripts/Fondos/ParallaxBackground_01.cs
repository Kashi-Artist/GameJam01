using UnityEngine;

public class ParallaxBackground_01 : MonoBehaviour
{
    [System.Serializable]
    public class ParallaxLayer
    {
        public Transform layerTransform;
        public float parallaxEffect;
        [HideInInspector] public float layerLength;
    }

    public ParallaxLayer[] layers;
    public Transform cam; // Referencia a la cámara principal
    private Vector3 lastCamPos;

    void Start()
    {
        if (cam == null)
        {
            // Si no asignamos manualmente la cámara, tomamos la principal
            cam = Camera.main.transform;
        }
        
        lastCamPos = cam.position;
        
        // Calculamos el ancho de cada capa basado en el SpriteRenderer
        foreach (var layer in layers)
        {
            if (layer.layerTransform != null)
            {
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
        Vector3 deltaMovement = cam.position - lastCamPos;
        
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].layerTransform == null) continue;
            
            // Movemos la capa según el efecto parallax
            Vector3 layerPosition = layers[i].layerTransform.position;
            layerPosition.x += deltaMovement.x * layers[i].parallaxEffect;
            layers[i].layerTransform.position = layerPosition;
            
            // Efecto de bucle infinito (opcional)
            if (Mathf.Abs(cam.position.x - layerPosition.x) >= layers[i].layerLength)
            {
                float offset = (cam.position.x - layerPosition.x) % layers[i].layerLength;
                layerPosition.x = cam.position.x + offset;
                layers[i].layerTransform.position = layerPosition;
            }
        }
        
        lastCamPos = cam.position;
    }
}