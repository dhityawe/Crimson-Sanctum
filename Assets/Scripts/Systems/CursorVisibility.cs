using UnityEngine;

public class CursorVisibility : MonoBehaviour
{
    [SerializeField] private bool isVisible;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        Cursor.visible = isVisible;
        Cursor.lockState = CursorLockMode.Locked;
    }
}
