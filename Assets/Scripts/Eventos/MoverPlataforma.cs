using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class MoverPlataforma : MonoBehaviour
{
    [System.Serializable]
    public class ActivacionPocion
    {
        [Header("Requisito")]
        public TipoPocion tipoPocion;
        public int cantidadRequerida;
        
        [Header("Objetivos")]
        public GameObject[] objetivosActivar;
        public PlataformaMovil_02 plataformaMovilNueva;
        
        [Header("Comportamiento")]
        public bool desactivarDespuesDeUso = false;
        public float retrasoDesactivacion = 0f;
        public int puntoDestinoAlActivar = -1;
        public int puntoDestinoAlDesactivar = 0; // Nueva variable para el punto destino al desactivar
        
        [Header("Efectos")]
        public UnityEvent alActivar;
        public AudioClip sonidoActivacion;
        public GameObject efectoVisual;
        public float duracionEfectoVisual = 2f;
    }
    
    [Header("Configuración")]
    public List<ActivacionPocion> activaciones = new List<ActivacionPocion>();
    
    [Header("Referencias")]
    public ColectorPociones colector;
    
    // Para evitar activaciones duplicadas
    private HashSet<int> activacionesRealizadas = new HashSet<int>();
    
    private void Start()
    {
        // Buscar colector si no está asignado
        if (colector == null)
        {
            colector = FindObjectOfType<ColectorPociones>();
        }
        
        if (colector != null)
        {
            // Suscribirse al evento de recolección
            colector.alRecolectarPocion.AddListener(VerificarActivaciones);
            
            // Verificar estado actual para activaciones inmediatas
            VerificarActivacionesIniciales();
        }
        else
        {
            Debug.LogError("No se encontró un ColectorPociones en la escena.", this);
        }
        
        // Inicialmente desactivar los objetivos
        foreach (var activacion in activaciones)
        {
            // Desactivamos los objetos inicialmente
            foreach (var objetivo in activacion.objetivosActivar)
            {
                if (objetivo != null)
                {
                    objetivo.SetActive(false);
                }
            }
            
            // Plataforma móvil nueva
            if (activacion.plataformaMovilNueva != null)
            {
                activacion.plataformaMovilNueva.Desactivar();
            }
        }
    }
    
    private void VerificarActivacionesIniciales()
    {
        // Comprobar cada activación contra el estado actual
        for (int i = 0; i < activaciones.Count; i++)
        {
            var activacion = activaciones[i];
            int cantidadActual = colector.ObtenerCantidadPociones(activacion.tipoPocion);
            
            if (cantidadActual >= activacion.cantidadRequerida)
            {
                // Activar inmediatamente si ya se tiene la cantidad
                ActivarObjetivo(i);
            }
        }
    }
    
    private void VerificarActivaciones(TipoPocion tipo, int cantidad)
    {
        // Verificar todas las activaciones configuradas
        for (int i = 0; i < activaciones.Count; i++)
        {
            var activacion = activaciones[i];
            
            // Si coincide el tipo y cumple o supera la cantidad requerida
            if (activacion.tipoPocion == tipo && cantidad >= activacion.cantidadRequerida)
            {
                // Activar si no se ha hecho ya
                if (!activacionesRealizadas.Contains(i))
                {
                    ActivarObjetivo(i);
                }
            }
        }
    }
    
    private void ActivarObjetivo(int indiceActivacion)
    {
        if (indiceActivacion < 0 || indiceActivacion >= activaciones.Count) return;
        
        // Marcar como realizada
        activacionesRealizadas.Add(indiceActivacion);
        
        var activacion = activaciones[indiceActivacion];
        
        // Activar GameObjects
        foreach (var objetivo in activacion.objetivosActivar)
        {
            if (objetivo != null)
            {
                objetivo.SetActive(true);
                
                if (activacion.desactivarDespuesDeUso)
                {
                    StartCoroutine(DesactivarDespues(objetivo, activacion.retrasoDesactivacion));
                }
            }
        }
       
        // Activar plataforma nueva si está configurada
        if (activacion.plataformaMovilNueva != null)
        {
            // Configurar el punto de destino al desactivar en la plataforma
            if (activacion.plataformaMovilNueva.irAPuntoAlDesactivar)
            {
                activacion.plataformaMovilNueva.puntoDestinoAlDesactivar = activacion.puntoDestinoAlDesactivar;
            }
            
            // Activar la plataforma
            activacion.plataformaMovilNueva.Activar();
            
            // Si tiene punto de destino específico al activar
            if (activacion.puntoDestinoAlActivar >= 0)
            {
                activacion.plataformaMovilNueva.IrAPunto(activacion.puntoDestinoAlActivar);
            }
            
            // Desactivar después si corresponde
            if (activacion.desactivarDespuesDeUso)
            {
                StartCoroutine(DesactivarPlataformaNuevaDespues(
                    activacion.plataformaMovilNueva, 
                    activacion.retrasoDesactivacion
                ));
            }
        }
        
        // Reproducir sonido
        if (activacion.sonidoActivacion != null && Camera.main != null)
        {
            AudioSource.PlayClipAtPoint(activacion.sonidoActivacion, Camera.main.transform.position);
        }
        
        // Mostrar efecto visual
        if (activacion.efectoVisual != null)
        {
            GameObject efecto = Instantiate(
                activacion.efectoVisual, 
                transform.position, 
                Quaternion.identity
            );
            
            Destroy(efecto, activacion.duracionEfectoVisual);
        }
        
        // Invocar evento personalizado
        activacion.alActivar?.Invoke();
    }
    
    private IEnumerator DesactivarDespues(GameObject objetivo, float retraso)
    {
        yield return new WaitForSeconds(retraso);
        if (objetivo != null)
        {
            objetivo.SetActive(false);
        }
    }
    
    
    private IEnumerator DesactivarPlataformaNuevaDespues(PlataformaMovil_02 plataforma, float retraso)
    {
        yield return new WaitForSeconds(retraso);
        if (plataforma != null)
        {
            plataforma.Desactivar();
        }
    }
    
    // Activar manualmente una configuración específica (útil para eventos o testing)
    public void ActivarPorIndice(int indice)
    {
        if (indice >= 0 && indice < activaciones.Count)
        {
            ActivarObjetivo(indice);
        }
    }
}