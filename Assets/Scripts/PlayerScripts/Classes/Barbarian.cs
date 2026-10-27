using UnityEngine;

[System.Serializable]
public class Barbarian : CharacterClass
{
    public Barbarian()
    {
        displayName = "Barbarian";
        moveSpeed = 5.5f;
        jumpForce = 13f;
        maxJumps = 1;
        canWallJump = false;
        canDash = false;
        abilities = new string[0];
    }
}