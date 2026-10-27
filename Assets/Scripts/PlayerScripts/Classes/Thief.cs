using UnityEngine;

[System.Serializable]
public class Thief : CharacterClass
{
    public Thief()
    {
        displayName = "Thief";
        moveSpeed = 7.5f;
        jumpForce = 12f;
        maxJumps = 2;
        canWallJump = true;
        wallJumpForceX = 9f;
        wallJumpForceY = 12f;
        wallSlideGravityMultiplier = 0.3f;
        canDash = true;
        abilities = new string[] { "DoubleJump", "WallJump" };
    }
}