using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class TablasPuntajes : MonoBehaviour
{
    [Header("Referencias")]
    public Transform contenedorItems;
    public GameObject prefabItemPuntaje;
    public int maximoItemsRanking = 20;  // Máximo número de ítems en el ranking
    public float alturaItem = 80f;       // Altura de cada ítem en píxeles

    [Header("Textos")]
    public TextMeshProUGUI textoUltimoPuntaje;
    public TextMeshProUGUI textoPuntajeMaximo;

    private GestorUsuarios gestorUsuarios;
    private List<GameObject> itemsInstanciados = new List<GameObject>();

    private void Start()
    {
        // Buscar el gestor de usuarios
        gestorUsuarios = FindObjectOfType<GestorUsuarios>();
        if (gestorUsuarios == null)
        {
            Debug.LogWarning("No se encontró el GestorUsuarios. La tabla de puntajes no funcionará correctamente.");
            return;
        }

        // Cargar puntajes
        CargarPuntajes();
    }

    public void CargarPuntajes()
    {
        if (gestorUsuarios == null) return;

        LimpiarItems();

        DatosUsuario usuarioActual = gestorUsuarios.ObtenerDatosUsuarioActual();
        if (usuarioActual == null) return;

        // Mostrar último puntaje como porcentaje
        if (textoUltimoPuntaje != null)
        {
            textoUltimoPuntaje.text = $"Último puntaje: {usuarioActual.ultimoPuntaje}%";
        }

        // Mostrar puntaje máximo como porcentaje
        if (textoPuntajeMaximo != null)
        {
            textoPuntajeMaximo.text = $"Puntaje máximo: {usuarioActual.puntajeMaximo}%";
        }
        
        // Obtener todos los usuarios ordenados por puntaje máximo
        List<DatosUsuario> usuariosOrdenados = gestorUsuarios.ObtenerListaUsuariosOrdenada()
            .OrderByDescending(u => u.puntajeMaximo)
            .Take(maximoItemsRanking)  // Limitar a los 20 mejores
            .ToList();

        // Crear ítems para el ranking
        for (int i = 0; i < usuariosOrdenados.Count; i++)
        {
            GameObject nuevoItem = Instantiate(prefabItemPuntaje, contenedorItems);
            itemsInstanciados.Add(nuevoItem);

            // Configurar el ítem con datos
            ItemPuntajeRanking itemRanking = nuevoItem.GetComponent<ItemPuntajeRanking>();
            if (itemRanking != null)
            {
                // Configurar ítem con ranking
                itemRanking.ConfigurarDatos(usuariosOrdenados[i], i + 1, 
                    usuariosOrdenados[i].nombre == usuarioActual.nombre);
            }
            
            // Asegurarse de que el ítem esté activo y visible
            nuevoItem.SetActive(true);
        }
    }

    public void CargarMejoresPuntajes(int cantidad)
    {
        if (gestorUsuarios == null) return;

        LimpiarItems();

        List<DatosUsuario> mejoresUsuarios = gestorUsuarios.ObtenerListaUsuariosOrdenada()
            .OrderByDescending(u => u.puntajeMaximo)
            .Take(cantidad)
            .ToList();

        DatosUsuario usuarioActual = gestorUsuarios.ObtenerDatosUsuarioActual();

        for (int i = 0; i < mejoresUsuarios.Count; i++)
        {
            GameObject nuevoItem = Instantiate(prefabItemPuntaje, contenedorItems);
            itemsInstanciados.Add(nuevoItem);
            
            ItemPuntajeRanking itemRanking = nuevoItem.GetComponent<ItemPuntajeRanking>();
            if (itemRanking != null)
            {
                itemRanking.ConfigurarDatos(mejoresUsuarios[i], i + 1, 
                    usuarioActual != null && mejoresUsuarios[i].nombre == usuarioActual.nombre);
            }
            
            // Asegurarse de que el ítem esté activo y visible
            nuevoItem.SetActive(true);
        }
    }

    public void CargarDatosUsuarioActual()
    {
        if (gestorUsuarios == null) return;

        LimpiarItems();

        DatosUsuario usuarioActual = gestorUsuarios.ObtenerDatosUsuarioActual();
        if (usuarioActual == null) return;

        GameObject nuevoItem = Instantiate(prefabItemPuntaje, contenedorItems);
        itemsInstanciados.Add(nuevoItem);

        ItemPuntajeRanking itemRanking = nuevoItem.GetComponent<ItemPuntajeRanking>();
        if (itemRanking != null)
        {
            itemRanking.ConfigurarDatos(usuarioActual, 1, true);
        }
        
        // Asegurarse de que el ítem esté activo y visible
        nuevoItem.SetActive(true);
    }

    private void LimpiarItems()
    {
        // Destruir todos los ítems instanciados
        foreach (GameObject item in itemsInstanciados)
        {
            Destroy(item);
        }
        itemsInstanciados.Clear();
    }
}