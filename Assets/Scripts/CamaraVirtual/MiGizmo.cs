using UnityEngine;

public class MiGizmo : MonoBehaviour
{
    [Header("Radio del área visible")]
    [Range(0f, 50f)]
    public float radio = 5f;

    public Color gizmoColor = Color.yellow;

    void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
