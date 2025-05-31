using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemPuntajeRanking : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI textoRanking;      // Texto para mostrar el número de ranking (01, 02, etc.)
    public TextMeshProUGUI textoNombre;       // Nombre del jugador
    public TextMeshProUGUI textoUltimoPuntaje;
    public TextMeshProUGUI textoPuntajeMaximo; // Puntaje máximo del jugador
    public Image imagenFondo;
    
    [Header("Colores")]
    public Color colorUsuarioActual = new Color(0.9f, 0.9f, 0.5f, 0.8f); // Color amarillo semi-transparente
    public Color colorNormal = new Color(1f, 1f, 1f, 0.6f); // Color normal semi-transparente
    
    // Configurar los datos del ítem con posición de ranking
    public void ConfigurarDatos(DatosUsuario datos, int posicionRanking, bool esUsuarioActual)
    {
        // Configurar texto de ranking con formato de dos dígitos (01, 02, etc.)
        if (textoRanking != null)
        {
            textoRanking.text = posicionRanking.ToString("00");
        }
        
        if (textoNombre != null)
        {
            textoNombre.text = datos.nombre;
        }
        
        if (textoUltimoPuntaje != null)
        {
            // Añadir el símbolo % al último puntaje
            textoUltimoPuntaje.text = datos.ultimoPuntaje.ToString() + "%";
        }
        
        if (textoPuntajeMaximo != null)
        {
            // Añadir el símbolo % al puntaje máximo
            textoPuntajeMaximo.text = datos.puntajeMaximo.ToString() + "%";
        }
        
        // Destacar al usuario actual
        if (imagenFondo != null)
        {
            imagenFondo.color = esUsuarioActual ? colorUsuarioActual : colorNormal;
        }
    }
}