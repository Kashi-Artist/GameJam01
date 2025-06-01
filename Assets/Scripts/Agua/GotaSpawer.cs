using UnityEngine;

public class GotaSpawner : MonoBehaviour
{
    public GameObject gotaPrefab;      // Prefab de la gota animada
    public int cantidad = 5;           // Número total de gotas
    public float intervalo = 0.5f;     // Tiempo entre cada aparición
    public Vector2 desplazamiento;     // Desplazamiento relativo entre cada gota

    void Start()
    {
        StartCoroutine(SpawnGotas());
    }

    System.Collections.IEnumerator SpawnGotas()
    {
        Vector2 posicionActual = transform.position;

        for (int i = 0; i < cantidad; i++)
        {
            // Instancia como hija del objeto que tiene este script
            Instantiate(gotaPrefab, posicionActual, transform.rotation, transform);

            posicionActual += desplazamiento;

            yield return new WaitForSeconds(intervalo);
        }
    }
}
