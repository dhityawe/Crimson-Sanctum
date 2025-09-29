using UnityEngine;

namespace Assets.Scripts.Player
{
    public interface IPlayerAbility
    {
        bool CanActivate();
        void Activate();
        void Deactivate();
        bool IsActive { get; }
        void SetEnabled(bool enabled);
    }

    #region V1 - Original Code (Commented for Rollback)
    /*
    // Original system had no interface
    // Components directly accessed each other's methods
    // This interface provides a consistent way to manage player abilities
    // and enables better state management and testing
    */
    #endregion
}
