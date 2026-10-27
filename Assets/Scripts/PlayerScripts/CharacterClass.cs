using UnityEngine;

// This is the data template for a single character class (Mage, Ranger, Barbarian, Thief).
// Each class stores its own stats, sprite, animator, and abilities.
// The player's MultiCharacterController reads these values when switching forms.
//
// create these as ScriptableObjects OR as children of this class
// (Mage, Ranger, etc.) — each class file fills in its own default values.
// Unity docs: https://docs.unity3d.com/ScriptReference/System.Serializable.html
[System.Serializable]
public abstract class CharacterClass
{
    [Header("Identity")]
    // The name shown for this class (e.g. "Mage"). Handy for debug logs and UI.
    public string displayName = "Class";

    // The sprite used when this class is active.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Sprite.html
    public Sprite formSprite;

    // The animator controller used when this class is active.
    // Unity docs: https://docs.unity3d.com/ScriptReference/RuntimeAnimatorController.html
    public RuntimeAnimatorController animator;

    // If false, the player must unlock this class before switching to it.
    public bool unlocked = true;

    [Header("Movement")]
    // How fast the character moves left and right (units per second).
    public float moveSpeed = 6f;

    // How quickly the character speeds up on the ground. Higher = snappier.
    public float groundAcceleration = 60f;

    // How quickly the character speeds up in the air. Usually lower than ground for floatier control.
    public float airAcceleration = 30f;

    // How strong the jump is (upward force).
    public float jumpForce = 12f;

    // Multiplies gravity while falling, so falls feel heavier than jumps.
    public float fallGravityMultiplier = 1.6f;

    [Header("Jumps")]
    // How many jumps the class gets before touching the ground again.
    // 1 = normal jump. 2 = double jump. 3 = triple jump.
    public int maxJumps = 1;

    [Header("Wall Jump")]
    // Whether this class can grab/slide on walls and wall jump.
    public bool canWallJump = false;

    // Horizontal push when jumping off a wall.
    public float wallJumpForceX = 8f;

    // Upward push when jumping off a wall.
    public float wallJumpForceY = 12f;

    // Multiplies gravity while wall sliding. Lower = slower slide.
    public float wallSlideGravityMultiplier = 0.3f;

    [Header("Dash")]
    // Whether this class can dash.
    public bool canDash = false;

    // How fast the dash moves.
    public float dashSpeed = 18f;

    // How long the dash lasts, in seconds.
    public float dashDuration = 0.15f;

    [Header("Collider (optional override)")]
    // If true, this class uses its own collider size instead of the default one.
    // Useful when classes have very different shapes (e.g. a tall Barbarian vs a small Thief).
    public bool overrideCollider = false;

    // The collider size to use when overrideCollider is true.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Vector2.html
    public Vector2 colliderSize = Vector2.zero;

    [Header("Abilities")]
    // A list of ability names this class unlocks (e.g. "DoubleJump", "WallJump").
    // Other scripts can check these with MultiCharacterController.HasAbility(...).
    public string[] abilities = new string[0];
}