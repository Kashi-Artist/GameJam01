using System.Collections;
using UnityEngine;
using TMPro;

public class FadeTextoBotones : MonoBehaviour
{
    public TextMeshProUGUI[] textos; // Asigna aquí los textos de los botones
    public float desfase = 0.5f;
    public float duracionFade = 1f;
    public float tiempoTransparente = 2f;
    public float tiempoEntreCiclos = 5f;

    void Start()
    {
        StartCoroutine(CicloFadeTexto());
    }

    IEnumerator CicloFadeTexto()
    {
        while (true)
        {
            // 🔻 Fase 1: Desaparecer textos en orden
            for (int i = 0; i < textos.Length; i++)
            {
                StartCoroutine(FadeTexto(textos[i], 1f, 0f));
                yield return new WaitForSeconds(desfase);
            }

            // ⏳ Espera mientras todos están invisibles
            yield return new WaitForSeconds(tiempoTransparente);

            // 🔺 Fase 2: Aparecer textos en orden inverso
            for (int i = textos.Length - 1; i >= 0; i--)
            {
                StartCoroutine(FadeTexto(textos[i], 0f, 1f));
                yield return new WaitForSeconds(desfase);
            }

            // ⏲️ Esperar antes de repetir ciclo
            yield return new WaitForSeconds(tiempoEntreCiclos);
        }
    }

    IEnumerator FadeTexto(TextMeshProUGUI texto, float alphaInicio, float alphaFinal)
    {
        float tiempo = 0f;
        Color original = texto.color;

        while (tiempo < duracionFade)
        {
            float t = tiempo / duracionFade;
            float alpha = Mathf.Lerp(alphaInicio, alphaFinal, t);
            texto.color = new Color(original.r, original.g, original.b, alpha);
            tiempo += Time.deltaTime;
            yield return null;
        }

        texto.color = new Color(original.r, original.g, original.b, alphaFinal);
    }
}
