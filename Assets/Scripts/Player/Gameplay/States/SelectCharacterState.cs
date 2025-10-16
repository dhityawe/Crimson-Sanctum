using UnityEngine;
using Assets.Scripts.Systems;
using Assets.Scripts.Player;

public class CharacterSelectState : BaseState<GameManager>
{
    private int currentCharacterIndex = 0;
    private GameObject currentPreviewCharacter = null;
    
    public void EnterState(GameManager owner)
    {
        // Display first character info and create preview
        if (owner.GetCharacterCount() > 0)
        {
            CharacterData firstCharacter = owner.GetCharacterData(0);
            if (firstCharacter != null)
            {
                // Create preview of first character
                CreateCharacterPreview(owner, firstCharacter);
            }
            owner.SetActiveUI(true);
        }
        else
        {
            Debug.LogWarning("No characters available in character data list!");
        }
    }
    
    public void UpdateState(GameManager owner)
    {
        // Handle character selection input
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SelectPreviousCharacter(owner);
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SelectNextCharacter(owner);
        }
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            // Confirm selection and move to playing state
            owner.ChangeToPlayingState(currentCharacterIndex);
        }
    }

    public void ExitState(GameManager owner)
    {
        // Don't destroy preview character - it will be used in PlayState
        // Just clear the reference
        currentPreviewCharacter = null;
        owner.SetActiveUI(false);
    }
    
    private void SelectPreviousCharacter(GameManager owner)
    {
        int characterCount = owner.GetCharacterCount();
        if (characterCount == 0) return;
        
        currentCharacterIndex = (currentCharacterIndex - 1 + characterCount) % characterCount;
        DisplayCurrentCharacter(owner);
    }
    
    private void SelectNextCharacter(GameManager owner)
    {
        int characterCount = owner.GetCharacterCount();
        if (characterCount == 0) return;
        
        currentCharacterIndex = (currentCharacterIndex + 1) % characterCount;
        DisplayCurrentCharacter(owner);
    }
    
    private void DisplayCurrentCharacter(GameManager owner)
    {
        CharacterData character = owner.GetCharacterData(currentCharacterIndex);
        if (character != null)
        {
            Debug.Log($"Character: {character.Name}");
            Debug.Log($"Description: {character.Description}");
            
            // Update preview character
            CreateCharacterPreview(owner, character);
        }
    }
    
    /// <summary>
    /// Create a preview of the selected character
    /// </summary>
    private void CreateCharacterPreview(GameManager owner, CharacterData characterData)
    {
        // Destroy current preview if exists
        if (currentPreviewCharacter != null)
        {
            Object.Destroy(currentPreviewCharacter);
        }
        
        // Create new preview character
        if (characterData != null ? characterData.CharacterPrefab : null != null && owner.SpawnPoint != null)
        {
            // Create preview at spawn point
            currentPreviewCharacter = Object.Instantiate(characterData.CharacterPrefab, owner.SpawnPoint.position, owner.SpawnPoint.rotation);
            
            // Disable player movement scripts for preview
            DisablePlayerScripts(currentPreviewCharacter);
            
            // Set as current player in GameManager
            owner.SetCurrentPlayer(currentPreviewCharacter);
            
        }
    }
    
    /// <summary>
    /// Disable player movement and interaction scripts for preview
    /// </summary>
    private void DisablePlayerScripts(GameObject character)
    {
        // New Architecture: Set player state to Preview
        if (character.TryGetComponent<PlayerStateManager>(out var stateManager))
        {
            stateManager.ChangeState(PlayerState.Preview);
        }
        if (character.TryGetComponent<PlayerController>(out var playerController))
        {
            playerController.enabled = false;
        }
            
        if (character.TryGetComponent<PlayerMove>(out var playerMove))
        {
            playerMove.enabled = false;
        }
        
        if (character.TryGetComponent<PlayerDash>(out var playerDash))
        {
            playerDash.enabled = false;
        }
        
        if (character.TryGetComponent<PlayerClimb>(out var playerClimb))
        {
            playerClimb.enabled = false;
        } 
    }
}
