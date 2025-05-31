using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuPuntajes : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject panelPuntajes;
    public TablasPuntajes tablaPuntajes;
    public Button botonVolver;
    private void OnEnable()
    {
        if (tablaPuntajes != null)
        {
            tablaPuntajes.CargarPuntajes();
        }
    }    
    private void Start()
    {
        // Verificar que el botón volver esté asignado
        if (botonVolver == null)
        {
            Debug.LogWarning("No se ha asignado el Botón Volver.");
        }
        else
        {
            // Configurar el botón volver
            botonVolver.onClick.RemoveAllListeners();
            botonVolver.onClick.AddListener(VolverAMenuPrincipal);
        }
        
        // Recargar los puntajes si existe la referencia
        if (tablaPuntajes != null)
        {
            tablaPuntajes.CargarPuntajes();
        }
        else
        {
            Debug.LogWarning("No se ha asignado el componente TablasPuntajes.");
        }
    }
    
    public void VolverAMenuPrincipal()
    {
        Debug.Log("Volviendo al menú principal...");
        SceneManager.LoadScene("MenuPrincipal");
    }
}