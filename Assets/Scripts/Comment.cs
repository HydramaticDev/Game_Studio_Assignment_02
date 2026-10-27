using UnityEngine;

public class Comment : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [Header("Documentation")]
    [Tooltip("Developer notes for this room. Has no effect on gameplay.")]
    [TextArea(3, 10)]
    public string notes = "";
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
