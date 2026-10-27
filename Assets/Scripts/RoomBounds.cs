using UnityEngine;

[ExecuteAlways]
public class RoomBounds : MonoBehaviour
{
    [Header("Bounds (relative to this transform)")]
    public Vector2 min;
    public Vector2 max;

    [Header("Documentation")]
    [Tooltip("Developer notes for this room. Has no effect on gameplay.")]
    [TextArea(3, 10)]
    public string notes = "";

    public Vector2 WorldMin => (Vector2)transform.position + min;
    public Vector2 WorldMax => (Vector2)transform.position + max;
    public Vector2 WorldCenter => (WorldMin + WorldMax) * 0.5f;
    public Vector2 WorldSize => WorldMax - WorldMin;

    public bool Contains(Vector2 point)
    {
        Vector2 wMin = WorldMin;
        Vector2 wMax = WorldMax;
        return point.x >= wMin.x && point.x <= wMax.x && point.y >= wMin.y && point.y <= wMax.y;
    }

    public void SetBounds(Vector2 newMin, Vector2 newMax)
    {
        min = newMin;
        max = newMax;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);

        Vector2 wMin = WorldMin;
        Vector2 wMax = WorldMax;

        Vector3 bl = new Vector3(wMin.x, wMin.y, 0f);
        Vector3 br = new Vector3(wMax.x, wMin.y, 0f);
        Vector3 tr = new Vector3(wMax.x, wMax.y, 0f);
        Vector3 tl = new Vector3(wMin.x, wMax.y, 0f);

        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
    }
}