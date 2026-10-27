using UnityEngine;

// Place this on a trigger collider placed at the entrance to a room.
// When the player walks into the trigger, the camera switches to follow that room's bounds.
//
// Setup:
//   1. Create a GameObject with a BoxCollider2D (or any 2D Collider) and tick "Is Trigger".
//   2. Add this script to that GameObject.
//   3. Drag your Main Camera (with the PlayerCamera script) into "Camera Script".
//   4. Drag the RoomBounds object for this room into "Room".
public class PlayerCameraRoomTrigger : MonoBehaviour
{
    // The camera script that should switch rooms. Usually the one on your Main Camera.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Camera.html
    public PlayerCamera cameraScript;

    // The room this trigger belongs to. When the player enters, the camera locks to this room.
    public RoomBounds room;

    // Unity calls this automatically when something enters the trigger collider.
    // We only respond if the thing that entered is the player.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnTriggerEnter2D.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Collider2D.html
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignore anything that isn't the player.
        // Note: the player's collider must be tagged "Player" for this to work.
        // Unity docs: https://docs.unity3d.com/ScriptReference/Component.CompareTag.html
        if (!other.CompareTag("Player")) return;

        // Safety check: make sure the artist assigned both slots in the Inspector.
        if (cameraScript == null || room == null) return;

        // Simply switch to the new room bounds.
        // The camera will smoothly pan there on its own (see PlayerCamera.LateUpdate).
        cameraScript.SetRoom(room);

        // Log so you can confirm the trigger fired in the Console.
        Debug.Log($"[TRIGGER] Player entered {room.name}. Smoothing camera transition.");
    }
}