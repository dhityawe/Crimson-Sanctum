using UnityEngine;

namespace CrimsonSanctum.Audio
{
    /// <summary>
    /// Example of how to organize SFX in your game.
    /// Use SFXManager for core/reusable sounds, direct calls for specialized sounds.
    /// </summary>
    public static class SFXLibrary
    {
        // Core UI sounds (put these in SFXManager)
        public const string BUTTON_CLICK = "buttonClick";
        public const string BUTTON_HOVER = "buttonHover";
        public const string MENU_OPEN = "menuOpen";
        public const string MENU_CLOSE = "menuClose";
        public const string ERROR_SOUND = "errorSound";
        
        // Player action sounds (put these in SFXManager)
        public const string JUMP = "jumpSound";
        public const string LAND = "landSound";
        public const string FOOTSTEP = "footstep";
        public const string TAKE_DAMAGE = "takeDamage";
        public const string PICKUP_ITEM = "pickupItem";
        public const string LEVEL_UP = "levelUp";
        
        // Combat sounds (put these in SFXManager)
        public const string SWORD_SWING = "swordSwing";
        public const string HIT_IMPACT = "hitImpact";
        public const string BLOCK = "block";
        public const string CRITICAL_HIT = "criticalHit";
        public const string SPELL_CAST = "spellCast";
        
        // Environment sounds (put these in SFXManager)
        public const string DOOR_OPEN = "doorOpen";
        public const string DOOR_CLOSE = "doorClose";
        public const string CHEST_OPEN = "chestOpen";
        public const string SWITCH_TOGGLE = "switchToggle";
        
        // Convenience methods for common sounds
        public static void PlayUISound(string soundName)
        {
            AudioManager.Instance?.PlaySFX(soundName, 0.7f);
        }
        
        public static void PlayPlayerAction(string soundName, Vector3 position)
        {
            AudioManager.Instance?.PlaySFX3D(soundName, position, 0.8f);
        }
        
        public static void PlayCombatSound(string soundName, Vector3 position)
        {
            AudioManager.Instance?.PlaySFX3D(soundName, position, 1.0f);
        }
    }
}

// Example usage in your game scripts:
/*
public class PlayerController : MonoBehaviour
{
    void Jump()
    {
        SFXLibrary.PlayPlayerAction(SFXLibrary.JUMP, transform.position);
    }
    
    void TakeDamage()
    {
        SFXLibrary.PlayPlayerAction(SFXLibrary.TAKE_DAMAGE, transform.position);
    }
}

public class UIButton : MonoBehaviour
{
    void OnClick()
    {
        SFXLibrary.PlayUISound(SFXLibrary.BUTTON_CLICK);
    }
    
    void OnHover()
    {
        SFXLibrary.PlayUISound(SFXLibrary.BUTTON_HOVER);
    }
}

public class WeaponController : MonoBehaviour
{
    [SerializeField] private AudioClip specialAttackSound; // Weapon-specific sound
    
    void NormalAttack()
    {
        // Use registered sound from SFXManager
        SFXLibrary.PlayCombatSound(SFXLibrary.SWORD_SWING, transform.position);
    }
    
    void SpecialAttack()
    {
        // Use weapon-specific sound directly
        AudioManager.Instance.PlaySFX3D(specialAttackSound, transform.position, 1.2f);
    }
}
*/