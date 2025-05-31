using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Evento personalizado para pociones
[System.Serializable]
public class EventoPocion : UnityEvent<TipoPocion, int> { }

// Componente para el jugador que recolecta pociones
public class ColectorPociones : MonoBehaviour
{
    [Header("Configuración de Pociones Blancas")]
    public int totalPocionesBlancasNivel = 10;  // Valor a configurar en el inspector
    public int totalPocionesBlancasOcultas = 3; // Valor predeterminado para pociones ocultas

    [Header("Contadores de Pociones")]
    public int pocionesBlancasRecolectadas = 0;
    public int pocionesBlancasOcultasRecolectadas = 0;
    public int pocionesOroRecolectadas = 0;
    public int pocionesAzulesRecolectadas = 0;
    public int pocionesRojasRecolectadas = 0;
    
    [Header("Puntuación")]
    public int puntuacionActual = 0;
    public float porcentajeExploracion = 0f;  // Nuevo: Porcentaje de exploración
    
    [Header("Eventos")]
    public EventoPocion alRecolectarPocion;
    public UnityEvent<int> alCambiarPuntuacion;
    
    [Header("Control de Día/Noche")]
    public bool activarCambioDiaNoche = true;
    public Pocion pocionParaCambio;  // Referencia a la poción específica que activará el cambio día/noche
    
    // Diccionario para rastrear eventos específicos de cantidad
    private Dictionary<TipoPocion, List<KeyValuePair<int, UnityEvent>>> eventosUmbral = new Dictionary<TipoPocion, List<KeyValuePair<int, UnityEvent>>>();
    
    private void Awake()
    {
        // Inicializar el diccionario para cada tipo de poción
        eventosUmbral[TipoPocion.Blanca] = new List<KeyValuePair<int, UnityEvent>>();
        eventosUmbral[TipoPocion.BlancaOculta] = new List<KeyValuePair<int, UnityEvent>>();
        eventosUmbral[TipoPocion.Oro] = new List<KeyValuePair<int, UnityEvent>>();
        eventosUmbral[TipoPocion.Azul] = new List<KeyValuePair<int, UnityEvent>>();
        eventosUmbral[TipoPocion.Roja] = new List<KeyValuePair<int, UnityEvent>>();
        
        // Cargar la puntuación guardada
        CargarPuntuacion();
    }
    public string ObtenerPorcentajeExploracionFormateado()
    {
        return $"{porcentajeExploracion:F1}%"; // Muestra con un decimal, ej: 87.5%
    }
    
    public void RecolectarPocion(Pocion pocion)
    {
        // Verificar si es la poción específica que activa el cambio día/noche
        if (activarCambioDiaNoche && pocionParaCambio != null && pocion == pocionParaCambio)
        {
            if (MovimientoFondos_02.Instance != null)
            {
                MovimientoFondos_02.Instance.CambiarDiaNoche();
            }
        }
        
        // Incrementar contador según tipo
        switch (pocion.tipo)
        {
            case TipoPocion.Blanca:
                pocionesBlancasRecolectadas++;
                break;
            case TipoPocion.BlancaOculta:
                pocionesBlancasOcultasRecolectadas++;
                break;
            case TipoPocion.Oro:
                pocionesOroRecolectadas++;
                break;
            case TipoPocion.Azul:
                pocionesAzulesRecolectadas++;
                break;
            case TipoPocion.Roja:
                pocionesRojasRecolectadas++;
                break;
        }
        
        // Calcular puntuación basada en el porcentaje de exploración
        CalcularPorcentajeExploracion();
        
        // Solo sumar puntos si son pociones blancas (normales u ocultas)
        if (pocion.tipo == TipoPocion.Blanca || pocion.tipo == TipoPocion.BlancaOculta)
        {
            // No usamos el valorPuntos de la poción, sino el porcentaje calculado
            ActualizarPuntuacion();
        }
        
        // Lanzar evento general de recolección
        alRecolectarPocion.Invoke(pocion.tipo, ObtenerCantidadPociones(pocion.tipo));
        
        // Verificar eventos de umbral para este tipo de poción
        VerificarEventosUmbral(pocion.tipo);
    }
    
    public int ObtenerCantidadPociones(TipoPocion tipo)
    {
        switch (tipo)
        {
            case TipoPocion.Blanca: return pocionesBlancasRecolectadas;
            case TipoPocion.BlancaOculta: return pocionesBlancasOcultasRecolectadas;
            case TipoPocion.Oro: return pocionesOroRecolectadas;
            case TipoPocion.Azul: return pocionesAzulesRecolectadas;
            case TipoPocion.Roja: return pocionesRojasRecolectadas;
            default: return 0;
        }
    }
    
    public void AgregarEventoUmbral(TipoPocion tipo, int umbral, UnityEvent accion)
    {
        eventosUmbral[tipo].Add(new KeyValuePair<int, UnityEvent>(umbral, accion));
    }
    
    private void VerificarEventosUmbral(TipoPocion tipo)
    {
        int cantidad = ObtenerCantidadPociones(tipo);
        
        foreach (var parUmbral in eventosUmbral[tipo])
        {
            if (cantidad == parUmbral.Key)
            {
                parUmbral.Value.Invoke();
            }
        }
    }
    
    // Nuevo método para calcular el porcentaje de exploración
    private void CalcularPorcentajeExploracion()
    {
        if (totalPocionesBlancasNivel <= 0)
            return;

        // Calcular porcentaje básico
        float porcentajeBase = (float)pocionesBlancasRecolectadas / totalPocionesBlancasNivel * 100f;
        
        // Calcular bonificación por pociones ocultas (5% por cada una, hasta 15% total)
        float bonificacionOcultas = 0;
        if (totalPocionesBlancasOcultas > 0)
        {
            bonificacionOcultas = (float)pocionesBlancasOcultasRecolectadas / totalPocionesBlancasOcultas * 15f;
        }
        
        // Sumar porcentaje base y bonificación
        porcentajeExploracion = porcentajeBase + bonificacionOcultas;
        
        // Limitar a un máximo de 115%
        porcentajeExploracion = Mathf.Min(porcentajeExploracion, 115f);
    }
    
    // Actualizar puntuación basada en el porcentaje
    private void ActualizarPuntuacion()
    {
        // Usar directamente el porcentaje como puntuación (70.5% = 70.5 puntos)
        int nuevaPuntuacion = Mathf.RoundToInt(porcentajeExploracion);
        
        // Actualizar puntuación solo si es diferente
        if (nuevaPuntuacion != puntuacionActual)
        {
            puntuacionActual = nuevaPuntuacion;
            alCambiarPuntuacion.Invoke(puntuacionActual);
            
            // Guardar la puntuación
            GuardarPuntuacion();
        }
    }
    
    // Método original para agregar puntos (mantenerlo por compatibilidad)
    public void AgregarPuntos(int puntos)
    {
        // Este método se mantiene por compatibilidad pero no se usará
        // para las pociones blancas, sino para otros posibles elementos
        puntuacionActual += puntos;
        alCambiarPuntuacion.Invoke(puntuacionActual);
        
        // Guardar la puntuación
        GuardarPuntuacion();
    }
    
    // Guardar puntuación
    public void GuardarPuntuacion()
    {
        // Guardar puntuación actual
        PlayerPrefs.SetInt("PuntuacionActual", puntuacionActual);
        
        // Comprobar y actualizar puntuación máxima
        int puntuacionMaxima = PlayerPrefs.GetInt("PuntuacionMaxima", 0);
        if (puntuacionActual > puntuacionMaxima)
        {
            PlayerPrefs.SetInt("PuntuacionMaxima", puntuacionActual);
        }
        
        // Guardar inmediatamente
        PlayerPrefs.Save();
    }
    
    // Cargar puntuación
    public void CargarPuntuacion()
    {
        puntuacionActual = PlayerPrefs.GetInt("PuntuacionActual", 0);
        alCambiarPuntuacion.Invoke(puntuacionActual);
    }
    
    // Obtener puntuación máxima
    public int ObtenerPuntuacionMaxima()
    {
        return PlayerPrefs.GetInt("PuntuacionMaxima", 0);
    }
    
    // Reiniciar puntuación actual
    public void ReiniciarPuntuacionActual()
    {
        puntuacionActual = 0;
        alCambiarPuntuacion.Invoke(puntuacionActual);
        PlayerPrefs.SetInt("PuntuacionActual", 0);
        PlayerPrefs.Save();
    }
}