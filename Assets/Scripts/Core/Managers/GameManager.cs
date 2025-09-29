using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Systems;

public class GameManager : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private List<CharacterData> characterDataList = new List<CharacterData>();
    
    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("State Machine")]
    private StateMachine<GameManager> stateMachine;
    
    // Current selected character index
    public int SelectedCharacterIndex { get; private set; } = 0;
    
    // Current spawned player instance
    public GameObject CurrentPlayer { get; private set; }
    
    // Public access to spawn point for character preview
    public Transform SpawnPoint => spawnPoint;
    
    void Start()
    {
        // Initialize state machine
        stateMachine = new StateMachine<GameManager>();
        
        // Start with character select state
        stateMachine.ChangeState(new CharacterSelectState(), this);
    }

    void Update()
    {
        // Update current state
        stateMachine.Update(this);
    }
    
    /// <summary>
    /// Change to playing state with selected character index
    /// </summary>
    public void ChangeToPlayingState(int characterIndex)
    {
        SelectedCharacterIndex = characterIndex;
        stateMachine.ChangeState(new PlayingState(), this);
    }
    
    /// <summary>
    /// Change to game over state
    /// </summary>
    public void ChangeToGameOverState()
    {
        stateMachine.ChangeState(new GameOverState(), this);
    }
    
    /// <summary>
    /// Get character data at specific index
    /// </summary>
    public CharacterData GetCharacterData(int index)
    {
        if (index >= 0 && index < characterDataList.Count)
        {
            return characterDataList[index];
        }
        return null;
    }
    
    /// <summary>
    /// Get total number of characters
    /// </summary>
    public int GetCharacterCount()
    {
        return characterDataList.Count;
    }
    
    /// <summary>
    /// Spawn character at spawn point
    /// </summary>
    public GameObject SpawnCharacter(CharacterData characterData)
    {
        if (characterData?.CharacterPrefab != null && spawnPoint != null)
        {
            // Destroy current player if exists
            if (CurrentPlayer != null)
            {
                Destroy(CurrentPlayer);
            }
            
            // Spawn new player
            CurrentPlayer = Instantiate(characterData.CharacterPrefab, spawnPoint.position, spawnPoint.rotation);
            return CurrentPlayer;
        }
        return null;
    }
    
    /// <summary>
    /// Set the current player instance (used for character preview)
    /// </summary>
    public void SetCurrentPlayer(GameObject player)
    {
        CurrentPlayer = player;
    }
}
