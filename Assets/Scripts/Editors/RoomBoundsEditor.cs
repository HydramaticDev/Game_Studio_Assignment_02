using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoomBounds))]
public class RoomBoundsEditor : Editor
{
    private void OnSceneGUI()
    {
        RoomBounds room = (RoomBounds)target;

        // Safety check to prevent crashes if the object gets deleted
        if (room == null) return;

        float handleSize = HandleUtility.GetHandleSize(room.WorldCenter) * 0.1f;

        Handles.color = Color.green;

        Vector3 wMin = room.WorldMin;
        Vector3 wMax = room.WorldMax;
        Vector3 wCenter = room.WorldCenter;

        Vector3 midLeft = new Vector3(wMin.x, wCenter.y, 0f);
        Vector3 midRight = new Vector3(wMax.x, wCenter.y, 0f);
        Vector3 midBottom = new Vector3(wCenter.x, wMin.y, 0f);
        Vector3 midTop = new Vector3(wCenter.x, wMax.y, 0f);

        // 1. Force snapping to exact 1-unit increments
        Vector3 snap = Vector3.one;

        EditorGUI.BeginChangeCheck();

        // 2. Draw the handles with the snap parameter
        Vector3 newLeft = Handles.FreeMoveHandle(midLeft, handleSize, snap, Handles.RectangleHandleCap);
        Vector3 newRight = Handles.FreeMoveHandle(midRight, handleSize, snap, Handles.RectangleHandleCap);
        Vector3 newBottom = Handles.FreeMoveHandle(midBottom, handleSize, snap, Handles.RectangleHandleCap);
        Vector3 newTop = Handles.FreeMoveHandle(midTop, handleSize, snap, Handles.RectangleHandleCap);
        Vector3 newCorner = Handles.FreeMoveHandle(wMax, handleSize, snap, Handles.RectangleHandleCap);
        Vector3 newCenter = Handles.FreeMoveHandle(wCenter, handleSize, snap, Handles.CircleHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(room, "Modify Room Bounds");

            // 3. Update the bounds based on which handle moved
            if (newCenter != wCenter)
            {
                Vector2 delta = (Vector2)newCenter - room.WorldCenter;
                room.min += delta;
                room.max += delta;
            }
            else if (newLeft != midLeft)
            {
                room.min.x = newLeft.x - room.transform.position.x;
            }
            else if (newRight != midRight)
            {
                room.max.x = newRight.x - room.transform.position.x;
            }
            else if (newBottom != midBottom)
            {
                room.min.y = newBottom.y - room.transform.position.y;
            }
            else if (newTop != midTop)
            {
                room.max.y = newTop.y - room.transform.position.y;
            }
            else if (newCorner != wMax)
            {
                room.max = (Vector2)newCorner - (Vector2)room.transform.position;
            }

            // 4. Ensure min and max don't cross over each other
            room.min = Vector2.Min(room.min, room.max);
            room.max = Vector2.Max(room.min, room.max);

            EditorUtility.SetDirty(room);
        }
    }
}