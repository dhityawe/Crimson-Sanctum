using Assets.Settings;
using UnityEngine;

namespace Assets.Scripts.Core.Managers
{
    public class GameInput : MonoBehaviour
    {
        public static GameInput Instance { get; set; }
        public InputSystem_Actions InputActions { get; private set; }

        void Awake()
        {
            InputActions = new();
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                return;
            }
            Destroy(gameObject);
        }

        void OnEnable()
        {
            InputActions.Player.Enable();
        }

        void OnDisable()
        {
            InputActions.Player.Disable();
        }

        public bool IsJumpPressed() => InputActions.Player.Jump.WasPressedThisFrame();
    }
}
