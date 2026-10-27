using UnityEngine;

[System.Serializable]
public class Ranger : CharacterClass
{
    public Ranger()
    {
        displayName = "Ranger";
        moveSpeed = 6.5f;
        jumpForce = 12f;
        maxJumps = 1;
        canWallJump = false;
        canDash = true;
        abilities = new string[0];
    }
}