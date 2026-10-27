using UnityEngine;
using System.Collections.Generic;

// The four playable character classes.
public enum CharacterForm
{
    Mage = 1,
    Ranger = 2,
    Barbarian = 3,
    Thief = 4
}

// This script controls the player character.
// It handles moving left and right, jumping, double jumping, wall jumping,
// dashing, and switching between the four character classes.
//
// The character needs a Rigidbody2D (for physics) and a Collider2D (for collision shape).
// Unity docs: https://docs.unity3d.com/ScriptReference/Rigidbody2D.html
// Unity docs: https://docs.unity3d.com/ScriptReference/Collider2D.html
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class MultiCharacterController : MonoBehaviour
{
    [Header("Class Definitions")]
    // The four class data assets. Each one stores stats like speed and jump height.
    [SerializeField] private Mage mage = new Mage();
    [SerializeField] private Ranger ranger = new Ranger();
    [SerializeField] private Barbarian barbarian = new Barbarian();
    [SerializeField] private Thief thief = new Thief();

    [Header("Ground Check")]
    // An empty GameObject placed at the character's feet.
    // The script uses this point to decide if the character is standing on the ground.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Transform.html
    [SerializeField] private Transform groundCheck;

    // How wide the ground detection circle is. Bigger = more forgiving.
    [SerializeField] private float groundCheckRadius = 0.15f;

    // Which layers count as "ground". Set this to your ground/platform layers.
    // Unity docs: https://docs.unity3d.com/ScriptReference/LayerMask.html
    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Check")]
    // How far sideways the script looks for a wall when wall sliding.
    [SerializeField] private float wallCheckDistance = 0.15f;

    // Which layers count as "wall". Set this to your wall layers.
    [SerializeField] private LayerMask wallLayer;

    [Header("Visuals")]
    // The Sprite Renderer that shows the character art.
    // Unity docs: https://docs.unity3d.com/ScriptReference/SpriteRenderer.html
    [SerializeField] private SpriteRenderer spriteRenderer;

    // The Animator that plays the character animations.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Animator.html
    [SerializeField] private Animator animator;

    [Header("Input Buffers")]
    // "Coyote time" = a short grace period after walking off a ledge
    // where the player can still jump. Feels more forgiving.
    [SerializeField] private float coyoteTime = 0.12f;

    // "Jump buffer" = if the player presses jump slightly before landing,
    // the jump still happens when they touch the ground.
    [SerializeField] private float jumpBufferTime = 0.12f;

    // How long the player must wait between dashes.
    [SerializeField] private float dashCooldown = 0.6f;

    // After a wall jump, wall sliding is disabled for this long
    // so the player doesn't instantly stick to the same wall.
    [SerializeField] private float wallJumpLockTime = 0.15f;

    // --- Internal references (set automatically, no need to touch) ---
    private Rigidbody2D rb;
    private Collider2D col;
    private CharacterClass[] classes;
    private int currentIndex = 0;
    private CharacterClass current => classes[currentIndex];

    // --- Input values ---
    private float moveInput;
    private bool jumpHeld;
    private bool dashPressed;

    // --- Timers ---
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float dashTimer;
    private float wallJumpLockTimer;
    private bool isDashing;
    private float dashTimeLeft;
    private int facing = 1;

    // --- Jump and wall state ---
    private int jumpsLeft;
    private bool isWallSliding;
    private int wallDir;

    // A list of ability names the player has unlocked.
    private HashSet<string> unlockedAbilities = new HashSet<string>();

    // Which class is active right now, as an enum (Mage, Ranger, etc.).
    public CharacterForm CurrentForm => (CharacterForm)(currentIndex + 1);

    // Which class is active right now, as a number (0 = Mage, 1 = Ranger, etc.).
    public int CurrentIndex => currentIndex;

    // The active class data (stats, sprite, animator, abilities).
    public CharacterClass CurrentClass => current;

    // Runs once when the game starts.
    // Grabs the Rigidbody2D and Collider2D, then sets up the default class.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true; // Prevents the character from tipping over.

        // If the artist forgot to drag in the sprite or animator, find them automatically.
        if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        classes = new CharacterClass[] { mage, ranger, barbarian, thief };

        // Warn in the Console if any class slot is empty.
        for (int i = 0; i < classes.Length; i++)
        {
            if (classes[i] == null)
                Debug.LogError($"Class slot {i} is empty! Please assign it in the Inspector.");
        }

        ApplyClass(currentIndex);
    }

    // Runs every frame. Reads player input and handles jump/dash timers.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.Update.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Input.html
    void Update()
    {
        if (current == null) return;

        // Read left/right movement and jump input.
        moveInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) jumpBufferTimer = jumpBufferTime;
        jumpHeld = Input.GetButton("Jump");

        // Dash with Left Shift or J.
        if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.J))
            dashPressed = true;

        // Switch classes with the number keys 1-4.
        if (Input.GetKeyDown(KeyCode.Alpha1)) TrySwap(CharacterForm.Mage);
        if (Input.GetKeyDown(KeyCode.Alpha2)) TrySwap(CharacterForm.Ranger);
        if (Input.GetKeyDown(KeyCode.Alpha3)) TrySwap(CharacterForm.Barbarian);
        if (Input.GetKeyDown(KeyCode.Alpha4)) TrySwap(CharacterForm.Thief);

        // Count down all the timers.
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;
        if (dashTimer > 0f) dashTimer -= Time.deltaTime;
        if (wallJumpLockTimer > 0f) wallJumpLockTimer -= Time.deltaTime;

        // Ground check and coyote time.
        bool grounded = IsGrounded();
        if (grounded) coyoteTimer = coyoteTime;
        else if (coyoteTimer > 0f) coyoteTimer -= Time.deltaTime;

        // Refill jump count when standing on the ground.
        if (grounded && rb.linearVelocity.y <= 0f)
            jumpsLeft = current.maxJumps;

        // Decide whether the player is wall sliding right now.
        UpdateWallSlide(grounded);

        // Try to jump if the jump button was pressed recently.
        if (jumpBufferTimer > 0f && !isDashing)
        {
            if (isWallSliding && current.canWallJump)
                DoWallJump();
            else if (grounded || coyoteTimer > 0f)
                DoGroundJump();
            else if (jumpsLeft > 0 && current.maxJumps > 1)
                DoAirJump();
        }

        // Variable jump height: if the player releases jump early, cut upward speed in half.
        if (!jumpHeld && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        // Start a dash if allowed.
        if (dashPressed && current.canDash && dashTimer <= 0f && !isDashing)
            StartDash();
        if (dashPressed) dashPressed = false;

        // Remember which way the character is facing.
        if (moveInput > 0.01f) facing = 1;
        else if (moveInput < -0.01f) facing = -1;

        // Update the sprite and animation.
        UpdateVisuals(grounded);
    }

    // Runs on a fixed timer (default 50 times per second). All physics movement goes here.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.FixedUpdate.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Time-fixedDeltaTime.html
    void FixedUpdate()
    {
        if (current == null) return;

        // --- Dashing ---
        if (isDashing)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            if (dashTimeLeft <= 0f) EndDash();
            return;
        }

        // --- Wall sliding ---
        if (isWallSliding)
        {
            float targetX = moveInput * current.moveSpeed;
            float slideSpeed = Mathf.Max(rb.linearVelocity.y, -2f); // Slow fall speed.
            rb.linearVelocity = new Vector2(
                Mathf.MoveTowards(rb.linearVelocity.x, targetX, current.airAcceleration * Time.fixedDeltaTime),
                slideSpeed);
            return;
        }

        // --- Normal ground/air movement ---
        float targetSpeed = moveInput * current.moveSpeed;
        float accel = IsGrounded() ? current.groundAcceleration : current.airAcceleration;
        float newX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    // A normal jump from the ground.
    private void DoGroundJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, current.jumpForce);
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
        jumpsLeft = current.maxJumps - 1;
    }

    // A second (or third) jump while in the air.
    private void DoAirJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, current.jumpForce);
        jumpBufferTimer = 0f;
        jumpsLeft--;
    }

    // A jump away from a wall while wall sliding.
    private void DoWallJump()
    {
        rb.linearVelocity = new Vector2(-wallDir * current.wallJumpForceX, current.wallJumpForceY);
        jumpBufferTimer = 0f;
        jumpsLeft = current.maxJumps - 1;
        isWallSliding = false;
        wallJumpLockTimer = wallJumpLockTime;
    }

    // Decides if the character should be wall sliding right now.
    // Wall sliding happens when: the class can wall jump, the player is pushing into a wall,
    // the player is falling, and the character is actually touching a wall.
    private void UpdateWallSlide(bool grounded)
    {
        isWallSliding = false;

        if (!current.canWallJump || grounded || wallJumpLockTimer > 0f) return;

        int dir = 0;
        if (moveInput > 0.1f) dir = 1;
        else if (moveInput < -0.1f) dir = -1;
        else return;

        if (rb.linearVelocity.y > 0f) return;

        if (IsTouchingWall(dir))
        {
            isWallSliding = true;
            wallDir = dir;
        }
    }

    // Shoots an invisible line sideways to check if there is a wall there.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Physics2D.Raycast.html
    private bool IsTouchingWall(int dir)
    {
        float xOffset = col.bounds.extents.x;
        Vector2 origin = (Vector2)col.bounds.center + new Vector2(xOffset * dir, 0);

        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * dir, wallCheckDistance, wallLayer);

        return hit.collider != null && hit.collider != col;
    }

    // Starts a dash in the direction the character is facing.
    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashCooldown;
        dashTimeLeft = current.dashDuration;
        rb.linearVelocity = new Vector2(facing * current.dashSpeed, 0f);
    }

    // Ends the dash and slows the character down a bit.
    private void EndDash()
    {
        isDashing = false;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.4f, rb.linearVelocity.y);
    }

    // Returns true if the character is standing on the ground.
    // Uses a small circle at the groundCheck point. If no groundCheck is assigned,
    // it falls back to a short raycast below the collider.
    // Unity docs: https://docs.unity3d.com/ScriptReference/Physics2D.OverlapCircle.html
    private bool IsGrounded()
    {
        if (groundCheck == null)
            return Physics2D.Raycast(col.bounds.center, Vector2.down, col.bounds.extents.y + 0.1f, groundLayer);
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    // Returns true if the given class has been unlocked.
    public bool IsFormUnlocked(CharacterForm form)
    {
        int i = (int)form - 1;
        if (i < 0 || i >= classes.Length) return false;
        return classes[i] != null && classes[i].unlocked;
    }

    // Unlocks a class so the player can switch to it.
    public void UnlockForm(CharacterForm form)
    {
        int i = (int)form - 1;
        if (i < 0 || i >= classes.Length) return;
        if (classes[i] == null) return;
        classes[i].unlocked = true;
    }

    // Tries to switch to a class. Fails if it is locked, already active, or if the player is dashing.
    public void TrySwap(CharacterForm form)
    {
        int i = (int)form - 1;
        if (i < 0 || i >= classes.Length) return;
        if (i == currentIndex) return;
        if (classes[i] == null) return;
        if (!classes[i].unlocked) { Debug.Log($"Class {form} not unlocked"); return; }
        if (isDashing) return;

        SwapTo(form);
    }

    // Forcefully switches to a class. Skips the unlock check.
    // Use this from cutscenes or scripted events.
    public void SwapTo(CharacterForm form)
    {
        currentIndex = (int)form - 1;
        ApplyClass(currentIndex);
        OnFormChanged();
    }

    // Applies the class data to the character: sprite, animator, abilities, collider, and jump count.
    private void ApplyClass(int index)
    {
        CharacterClass c = classes[index];
        if (c == null) return;

        if (spriteRenderer != null && c.formSprite != null)
            spriteRenderer.sprite = c.formSprite;
        if (animator != null && c.animator != null)
            animator.runtimeAnimatorController = c.animator;

        foreach (var a in c.abilities)
            unlockedAbilities.Add(a);

        if (c.overrideCollider && c.colliderSize != Vector2.zero)
        {
            if (col is BoxCollider2D box)
                box.size = c.colliderSize;
        }

        jumpsLeft = c.maxJumps;
        isWallSliding = false;
    }

    // Called right after the class changes. Good place to play a swap effect or sound.
    private void OnFormChanged()
    {
        Debug.Log($"Swapped to class: {CurrentForm}");
    }

    // Updates the character art: flips the sprite and updates animator parameters.
    // Unity docs: https://docs.unity3d.com/ScriptReference/SpriteRenderer-flipX.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Animator.SetFloat.html
    private void UpdateVisuals(bool grounded)
    {
        if (spriteRenderer != null)
            spriteRenderer.flipX = facing < 0;

        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
            animator.SetBool("Grounded", grounded);
            animator.SetBool("Dashing", isDashing);
            animator.SetBool("WallSliding", isWallSliding);
        }
    }

    // Returns true if the player has unlocked the named ability.
    public bool HasAbility(string ability) => unlockedAbilities.Contains(ability);

    // Unlocks an ability by name (for example, "DoubleJump").
    public void UnlockAbility(string ability) => unlockedAbilities.Add(ability);

    // Draws helpful shapes in the Scene view so you can see the ground and wall checks.
    // Only visible when this object is selected.
    // Unity docs: https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDrawGizmosSelected.html
    // Unity docs: https://docs.unity3d.com/ScriptReference/Gizmos.html
    private void OnDrawGizmosSelected()
    {
        // Green circle shows the ground check area.
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // Cyan lines show where the wall checks look.
        if (TryGetComponent<Collider2D>(out var c))
        {
            Gizmos.color = Color.cyan;
            float xOffset = c.bounds.extents.x;
            Vector3 center = c.bounds.center;

            Vector3 leftEdge = center + Vector3.left * xOffset;
            Vector3 rightEdge = center + Vector3.right * xOffset;

            Gizmos.DrawLine(leftEdge, leftEdge + Vector3.left * wallCheckDistance);
            Gizmos.DrawLine(rightEdge, rightEdge + Vector3.right * wallCheckDistance);
        }
    }
}