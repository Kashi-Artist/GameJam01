using UnityEngine;
using System.Collections;

public class FirebaseInitializer : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Intentar inicializar Firebase automáticamente al inicio")]
    public bool iniciarAutomaticamente = true;

    [Tooltip("Tiempo de espera entre intentos de inicialización (segundos)")]
    public float tiempoEspera = 2f;

    [Tooltip("Número máximo de intentos de inicialización")]
    public int maximoIntentos = 5;

    private int intentosRealizados = 0;
    private bool firebaseInicializado = false;

    private void Start()
    {
        if (iniciarAutomaticamente)
        {
            StartCoroutine(InicializarFirebaseConReintentos());
        }
    }

    // Método público para iniciar la inicialización de Firebase manualmente
    public void InicializarFirebase()
    {
        if (!firebaseInicializado)
        {
            StartCoroutine(InicializarFirebaseConReintentos());
        }
    }

    private IEnumerator InicializarFirebaseConReintentos()
    {
        // Esperar un momento para asegurarse de que Unity esté listo
        yield return new WaitForSeconds(0.5f);

        // Buscar el GestorFirebase
        GestorFirebase gestorFirebase = FindObjectOfType<GestorFirebase>();

        if (gestorFirebase == null)
        {
            Debug.LogError("[FirebaseInitializer] No se encontró un GestorFirebase en la escena.");
            yield break;
        }

        // Suscribirse al evento de inicialización
        gestorFirebase.OnFirebaseInicializado += () => {
            firebaseInicializado = true;
            Debug.Log("[FirebaseInitializer] Firebase inicializado correctamente.");
        };

        // También buscar el helper
        GestorFirebaseHelper helper = FindObjectOfType<GestorFirebaseHelper>();
        if (helper == null)
        {
            // Crear una instancia
            helper = GestorFirebaseHelper.Instancia;
            Debug.Log("[FirebaseInitializer] GestorFirebaseHelper instanciado.");
        }

        // Comenzar intentos de inicialización
        intentosRealizados = 0;
        while (!firebaseInicializado && intentosRealizados < maximoIntentos)
        {
            intentosRealizados++;
            Debug.Log($"[FirebaseInitializer] Intento {intentosRealizados}/{maximoIntentos} de inicializar Firebase");

            // Llamar tanto al helper como al gestor principal
            helper.InicializarFirebaseSiNecesario();
            gestorFirebase.VerificarEstadoFirebase();

            // Esperar un tiempo antes del siguiente intento
            yield return new WaitForSeconds(tiempoEspera);

            // Si mientras tanto se inicializó, salir del bucle
            if (helper.EstaFirebaseInicializado() || gestorFirebase.EstaFirebaseDisponible())
            {
                firebaseInicializado = true;
                Debug.Log("[FirebaseInitializer] Firebase inicializado durante los reintentos.");
                break;
            }
        }

        if (!firebaseInicializado)
        {
            Debug.LogWarning("[FirebaseInitializer] No se pudo inicializar Firebase después de varios intentos.");
        }
    }
}