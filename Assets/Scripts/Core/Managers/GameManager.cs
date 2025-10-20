using UnityEngine;
using System.Collections.Generic;
using Assets.Scripts.Systems;
using TMPro;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private List<CharacterData> characterDataList = new List<CharacterData>();

    [Header("Spawn Settings")]
    [SerializeField] private Transform spawnPoint;

    [Header("State Machine")]
    private StateMachine<GameManager> stateMachine;

    [Header("UI character select")]
    [SerializeField] private TMP_Text _enterGameText;
    [SerializeField] private Image[] _arrowImages = new Image[2];

    [Header("UI Tutorial")]
    [SerializeField] private TMP_Text _tutorialJumpText;

    // Current selected character index
    public int SelectedCharacterIndex { get; private set; } = 0;

    // Current spawned player instance
    public GameObject CurrentPlayer { get; private set; }
    public TMP_Text TutorialJumpText { get => _tutorialJumpText; }

    // Public access to spawn point for character preview
    public Transform SpawnPoint => spawnPoint;

    void Start()
    {
        // Initialize state machine
        InitializeStateMachine();
    }

    /// <summary>
    /// Initialize the state machine - can be called manually if needed
    /// </summary>
    public void Initialize()
    {
        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        if (stateMachine == null)
        {
            stateMachine = new StateMachine<GameManager>();
            Debug.Log("GameManager: State machine initialized");
        }

        // Start with character select state
        stateMachine.ChangeState(new CharacterSelectState(), this);
    }

    void Update()
    {
        // Update current state only if state machine exists
        stateMachine?.Update(this);
    }

    /// <summary>
    /// Change to playing state with selected character index
    /// </summary>
    public void ChangeToPlayingState(int characterIndex)
    {
        // Ensure state machine is initialized
        if (stateMachine == null)
        {
            Debug.LogWarning("GameManager: StateMachine is null, initializing...");
            InitializeStateMachine();
        }

        SelectedCharacterIndex = characterIndex;
        stateMachine.ChangeState(new PlayingState(), this);
    }

    /// <summary>
    /// Change to game over state
    /// </summary>
    public void ChangeToGameOverState()
    {
        Debug.Log("GameManager: ChangeToGameOverState called");

        // Ensure state machine is initialized
        if (stateMachine == null)
        {
            Debug.LogWarning("GameManager: StateMachine is null, initializing on-demand...");
            InitializeStateMachine();

            // If still null after initialization, there's a bigger problem
            if (stateMachine == null)
            {
                Debug.LogError("GameManager: Failed to initialize StateMachine! Cannot change to GameOver state.");
                return;
            }
        }

        Debug.Log("GameManager: Changing to GameOver state");
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

    public void SetActiveUI(bool isActive)
    {
        if (isActive)
        {
            _enterGameText.gameObject.SetActive(isActive);

            if (characterDataList.Count > 1)
            {
                foreach (var img in _arrowImages)
                {
                    img.gameObject.SetActive(isActive);
                }
            }
        }
        else
        {
            _enterGameText.gameObject.SetActive(false);
            foreach (var img in _arrowImages)
            {
                img.gameObject.SetActive(false);
            }
        }
    }
}
