using UnityEngine;

[ExecuteAlways]
public class CamaraGizmo : MonoBehaviour
{
    private BoxCollider2D boxCollider;

    public Color gizmoColor = Color.green;

    void OnDrawGizmos()
    {
        if (boxCollider == null)
            boxCollider = GetComponent<BoxCollider2D>();
        if (boxCollider == null) return;

        Gizmos.color = gizmoColor;

        Vector3 offset = (Vector3)boxCollider.offset;
        Vector3 size = (Vector3)boxCollider.size;

        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(offset, size);
    }
}
