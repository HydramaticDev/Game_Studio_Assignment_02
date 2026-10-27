using UnityEngine;

[System.Serializable]
public class Mage : CharacterClass
{
    public Mage()
    {
        displayName = "Mage";
        moveSpeed = 5f;
        jumpForce = 11f;
        maxJumps = 1;
        canWallJump = false;
        canDash = false;
        abilities = new string[0];
    }
}