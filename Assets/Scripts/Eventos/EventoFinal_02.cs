using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EventoFinal_02 : MonoBehaviour
{
    [Header("Zona 1 - Fade y efectos")]
    public Vector2 tamanoDeteccion = new Vector2(5f, 3f); // Área de detección
    public float duracionFade = 2.0f;                     // Duración del fade
    public string escenaDestino = "MenuPrincipal";        // Escena de destino

    [Header("Zona 2 - Cambio directo")]
    public Transform puntoCambioEscena;                   // Centro del área
    public Vector2 tamanoCambioEscena = new Vector2(5f, 3f);
    public float retardoCambioEscena = 3f;

    [Header("Referencias")]
    public SpriteRenderer spriteFade1;                    // Primer sprite negro
    public SpriteRenderer spriteFade2;                    // Segundo sprite negro
    public AudioClip sonidoFinal;                         // Sonido
    public ParticleSystem efectoFinal;                    // Partículas

    private bool eventoActivado = false;
    private bool cambioEscenaActivado = false;
    private Coroutine secuenciaActual;

    private void Start()
    {
        InicializarFade(spriteFade1);
        InicializarFade(spriteFade2);
    }

    private void InicializarFade(SpriteRenderer sprite)
    {
        if (sprite != null)
        {
            Color c = sprite.color;
            sprite.color = new Color(c.r, c.g, c.b, 0f);
            sprite.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!eventoActivado)
        {
            Collider2D jugador = Physics2D.OverlapBox(
                transform.position,
                tamanoDeteccion,
                0f
            );

            if (jugador != null && jugador.CompareTag("Player"))
            {
                ActivarEventoFinal(jugador.gameObject);
            }
        }

        if (!cambioEscenaActivado && puntoCambioEscena != null)
        {
            Collider2D jugador = Physics2D.OverlapBox(
                puntoCambioEscena.position,
                tamanoCambioEscena,
                0f
            );

            if (jugador != null && jugador.CompareTag("Player"))
            {
                cambioEscenaActivado = true;
                StartCoroutine(CambiarEscenaTrasEspera());
            }
        }
    }

    public void ActivarEventoFinal(GameObject jugador)
    {
        if (eventoActivado) return;

        eventoActivado = true;
        GuardarDatosJugador();

        // Cambiar capa visual del jugador
        SpriteRenderer sr = jugador.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingLayerName = "Foreground";
            sr.sortingOrder = 31;
        }

        secuenciaActual = StartCoroutine(SecuenciaEventoFinal());
    }

    private IEnumerator SecuenciaEventoFinal()
    {
        if (sonidoFinal != null)
        {
            AudioSource.PlayClipAtPoint(sonidoFinal, Camera.main.transform.position);
        }

        if (efectoFinal != null)
        {
            efectoFinal.Play();
        }

        if (spriteFade1 != null) spriteFade1.gameObject.SetActive(true);
        if (spriteFade2 != null) spriteFade2.gameObject.SetActive(true);

        float tiempoTranscurrido = 0f;
        Color c1 = spriteFade1 != null ? spriteFade1.color : Color.black;
        Color c2 = spriteFade2 != null ? spriteFade2.color : Color.black;

        while (tiempoTranscurrido < duracionFade)
        {
            float alpha = Mathf.Lerp(0f, 1f, tiempoTranscurrido / duracionFade);

            if (spriteFade1 != null)
                spriteFade1.color = new Color(c1.r, c1.g, c1.b, alpha);
            if (spriteFade2 != null)
                spriteFade2.color = new Color(c2.r, c2.g, c2.b, alpha);

            tiempoTranscurrido += Time.deltaTime;
            yield return null;
        }

        if (spriteFade1 != null)
            spriteFade1.color = new Color(c1.r, c1.g, c1.b, 1f);
        if (spriteFade2 != null)
            spriteFade2.color = new Color(c2.r, c2.g, c2.b, 1f);
    }

    private IEnumerator CambiarEscenaTrasEspera()
    {
        yield return new WaitForSeconds(retardoCambioEscena);
        SceneManager.LoadScene(escenaDestino);
    }

    private void GuardarDatosJugador()
    {
        GestorJuego gestor = GestorJuego.Instancia;
        if (gestor != null)
        {
            gestor.ActualizarInterfazPuntuacion();
            if (gestor.guardarEnAlmacenamientoLocal)
            {
                gestor.GuardarEnAlmacenamientoLocal();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawCube(transform.position, tamanoDeteccion);

        if (puntoCambioEscena != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // naranja
            Gizmos.DrawCube(puntoCambioEscena.position, tamanoCambioEscena);
        }
    }
}
