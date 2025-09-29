using System;

namespace Assets.Scripts.Player 
{
    public class PlayerEvents
    {
        public static Action OnPlayerDeath;
        public static Action OnCoinPickup;
        public static Action<PlayerState> OnStateChange;
        public static Action OnClimbStart;
        public static Action OnClimbEnd;
        public static Action OnDashStart;
        public static Action OnDashEnd;

        #region V1 - Original Code (Commented for Rollback)
        /*
        // Original events were scattered across individual components
        // This centralized event system allows for better decoupling
        // and easier debugging of player state changes
        */
        #endregion
    }
}
