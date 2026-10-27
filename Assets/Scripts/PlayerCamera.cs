using UnityEngine;

// This script follows the player with the camera.
// It adds a look-ahead effect (the camera leans in the direction the player is moving)
// and keeps the camera inside the current room's boundaries.
//
// Attach this to your Main Camera. Drag the player's Transform into the "Target" slot.
public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    // The thing the camera follows. Usually the player.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Transform.html
    public Transform target;

    // How far the camera sits from the target. Z is usually negative for 2D.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Vector3.html
    public Vector3 offset = new Vector3(0f, 1.5f, -10f);

    [Header("Follow")]
    // How long the camera takes to catch up. Lower = snappier, higher = smoother/laggier.
    public float followSmoothTime = 0.15f;

    [Header("Look-Ahead")]
    // Turn the look-ahead effect on or off.
    public bool enableLookAhead = true;

    // How far ahead the camera leans when the player is moving.
    public float lookAheadDistance = 2f;

    // How long the look-ahead takes to slide into place. Higher = slower/softer.
    public float lookAheadSmoothTime = 0.4f;

    [Header("Room")]
    // The room the camera is currently locked inside.
    // Leave empty until the player enters a room trigger.
    public RoomBounds currentRoom;

    // If true, the camera automatically fixes itself if the player ends up
    // in a different room without a trigger firing (safety net for bugs).
    public bool enableRoomFailsafe = true;

    // How long the player has to be in the "wrong" room before the failsafe kicks in.
    public float failsafeDelay = 1f;

    [Header("Camera")]
    // The Camera component on this GameObject.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Camera.html
    public Camera cam;

    // --- Internal values (set automatically, no need to touch) ---

    // Used by SmoothDamp to remember the current follow speed.
    private Vector3 followVelocity;

    // The current look-ahead offset (slides toward the target value).
    private float currentLookAhead;

    // Used by SmoothDamp to remember the current look-ahead speed.
    private float lookAheadVelocity;

    // Where the target was last frame. Used to detect movement direction.
    private Vector2 lastTargetPos;

    // --- Failsafe tracking ---
    // A list of every RoomBounds in the scene, found at startup.
    private RoomBounds[] allRooms;

    // How long the player has been in the "wrong" room.
    private float roomMismatchTimer = 0f;

    // Runs once when the game starts.
    // Finds the Camera and all rooms, then snaps to the player.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.Start.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Object.FindObjectsByType.html
    void Start()
    {
        // If the artist forgot to drag in the Camera, grab it automatically.
        if (cam == null) cam = GetComponent<Camera>();

        // Find every RoomBounds in the scene so the failsafe can check them.
        allRooms = Object.FindObjectsByType<RoomBounds>(FindObjectsSortMode.None);

        if (target == null) return;

        lastTargetPos = target.position;
        SnapToTarget();
    }

    // Runs after Update every frame. Camera movement goes here so it stays smooth
    // and doesn't fight with the player's movement in the same frame.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.LateUpdate.html
    void LateUpdate()
    {
        if (target == null || cam == null) return;

        // 1. Run the failsafe check (auto-fix if the player is in the wrong room).
        if (enableRoomFailsafe)
        {
            CheckRoomFailsafe();
        }

        // 2. Move the camera toward where it wants to be.
        // Unity docs: https://docs.unity3d.com/ScriptReference/Vector3.SmoothDamp.html
        Vector3 desired = ComputeDesiredPosition();
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, followSmoothTime);
        transform.position = smoothed;
    }

    // Safety net: if the player is inside a room but the camera is still locked to a different room,
    // wait a moment and then force the camera to switch. This protects against missed triggers.
    void CheckRoomFailsafe()
    {
        RoomBounds playerRoom = null;

        // Find which room the player is currently in.
        foreach (var room in allRooms)
        {
            if (room.Contains(target.position))
            {
                playerRoom = room;
                break;
            }
        }

        // If the player is in a valid room, but it's not the current one...
        if (playerRoom != null && playerRoom != currentRoom)
        {
            roomMismatchTimer += Time.deltaTime;

            if (roomMismatchTimer >= failsafeDelay)
            {
                Debug.Log($"[CAMERA FAILSAFE] Player spent {failsafeDelay}s in {playerRoom.name} without trigger. Forcing camera room update.");
                SetRoom(playerRoom);
                roomMismatchTimer = 0f; // Reset the timer.
            }
        }
        else
        {
            // If the player is in the correct room (or in no room), reset the timer.
            roomMismatchTimer = 0f;
        }
    }

    // Works out where the camera *wants* to be this frame.
    // Combines the target position, the offset, the look-ahead, and the room clamp.
    Vector3 ComputeDesiredPosition()
    {
        Vector3 followPos = target.position + offset;

        // Look-ahead: lean in the direction the player is moving.
        if (enableLookAhead)
        {
            float targetLook = 0f;
            float deltaX = target.position.x - lastTargetPos.x;

            if (Mathf.Abs(deltaX) > 0.01f)
                targetLook = Mathf.Sign(deltaX) * lookAheadDistance;

            // Smoothly slide the look-ahead value toward its target.
            // Unity docs: https://docs.unity3d.com/ScriptReference/Mathf.SmoothDamp.html
            currentLookAhead = Mathf.SmoothDamp(currentLookAhead, targetLook, ref lookAheadVelocity, lookAheadSmoothTime);
            followPos.x += currentLookAhead;
        }

        lastTargetPos = target.position;

        // Keep the camera inside the current room's boundaries.
        if (currentRoom != null)
            followPos = ClampToRoom(followPos, currentRoom);

        return followPos;
    }

    // Keeps the camera inside the room's bounds.
    // If the room is smaller than the camera view, the camera locks to the room's centre.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Mathf.Clamp.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Camera-orthographic.html
    Vector3 ClampToRoom(Vector3 desiredPos, RoomBounds room)
    {
        // Figure out how big the camera view is in world units.
        float halfHeight = cam.orthographic
            ? cam.orthographicSize
            : Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * Mathf.Abs(offset.z);

        float halfWidth = halfHeight * cam.aspect;

        // Pull the room's bounds and size.
        Vector2 roomMin = room.WorldMin;
        Vector2 roomMax = room.WorldMax;
        Vector2 roomSize = room.WorldSize;
        Vector2 roomCenter = room.WorldCenter;

        float clampedX, clampedY;

        // If the room is narrower than the camera view, centre the camera horizontally.
        if (roomSize.x < halfWidth * 2f)
            clampedX = roomCenter.x;
        else
            clampedX = Mathf.Clamp(desiredPos.x, roomMin.x + halfWidth, roomMax.x - halfWidth);

        // Same idea for vertical.
        if (roomSize.y < halfHeight * 2f)
            clampedY = roomCenter.y;
        else
            clampedY = Mathf.Clamp(desiredPos.y, roomMin.y + halfHeight, roomMax.y - halfHeight);

        return new Vector3(clampedX, clampedY, desiredPos.z);
    }

    // Call this from a room trigger to lock the camera to a new room.
    // Set "snap" to true if you want the camera to jump instantly instead of sliding.
    public void SetRoom(RoomBounds room, bool snap = false)
    {
        if (currentRoom == room) return;

        currentRoom = room;

        if (snap && target != null)
            SnapToTarget();
    }

    // Instantly places the camera where it wants to be, with no smoothing.
    // Used at startup and when SetRoom is called with snap = true.
    void SnapToTarget()
    {
        Vector3 desired = ComputeDesiredPosition();
        transform.position = desired;
        followVelocity = Vector3.zero;
    }
}