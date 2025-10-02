using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace CrimsonSanctum.UI
{
    /// <summary>
    /// Handles keyboard/gamepad navigation for buttons in a layout group.
    /// Automatically detects Vertical or Horizontal layout and adjusts navigation accordingly.
    /// Features color transitions and fade animation for selected button.
    /// </summary>
    public class ButtonSelector : MonoBehaviour
    {
        [Header("Navigation Settings")]
        [SerializeField] private Transform buttonParent;
        [SerializeField] private int startingIndex = 0;
        
        [Header("Visual Feedback")]
        [SerializeField] private Color selectedColor = Color.yellow;
        [SerializeField] private Color pressedColor = Color.red;
        [SerializeField] private Color normalColor = Color.white;
        
        [Header("Fade Animation")]
        [SerializeField] private bool enableFadeAnimation = true;
        [SerializeField] private float fadeMinAlpha = 0.5f;
        [SerializeField] private float fadeMaxAlpha = 1f;
        [SerializeField] private float fadeDuration = 1.2f;
        [SerializeField] private Ease fadeEase = Ease.InOutSine;
        
        [Header("Arrow Indicator")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private float arrowOffsetX = -20f;
        [SerializeField] private bool animateArrow = true;
        [SerializeField] private float arrowBounceDistance = 5f;
        [SerializeField] private float arrowBounceDuration = 0.5f;
        
        [Header("Audio (Optional)")]
        [SerializeField] private AudioClip navigateSound;
        [SerializeField] private AudioClip selectSound;
        
        private List<Button> buttons = new List<Button>();
        private List<TextMeshProUGUI> buttonTexts = new List<TextMeshProUGUI>();
        private int currentSelectedIndex = 0;
        private bool isVerticalLayout = true;
        private Tween currentFadeTween;
        private Tween arrowBounceTween;
        private AudioSource audioSource;
        private GameObject currentArrowInstance;
        
        void Awake()
        {
            InitializeButtons();
            DetectLayoutType();
            
            // Setup audio source if sounds are assigned
            if (navigateSound != null || selectSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }
        
        void OnEnable()
        {
            currentSelectedIndex = Mathf.Clamp(startingIndex, 0, buttons.Count - 1);
            UpdateButtonVisuals();
            StartFadeAnimation();
            UpdateArrowPosition();
        }
        
        void OnDisable()
        {
            StopFadeAnimation();
            DestroyArrow();
        }
        
        void Update()
        {
            HandleInput();
        }
        
        void InitializeButtons()
        {
            buttons.Clear();
            buttonTexts.Clear();
            
            if (buttonParent == null)
            {
                Debug.LogError("ButtonSelector: buttonParent is not assigned!");
                return;
            }
            
            // Get all buttons from children
            foreach (Transform child in buttonParent)
            {
                Button button = child.GetComponent<Button>();
                if (button != null && button.interactable)
                {
                    buttons.Add(button);
                    
                    // Find TextMeshProUGUI component (can be in child or same GameObject)
                    TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
                    if (buttonText == null)
                    {
                        buttonText = button.GetComponent<TextMeshProUGUI>();
                    }
                    
                    buttonTexts.Add(buttonText);
                    
                    // Add onClick listener to handle button clicks
                    int index = buttons.Count - 1;
                    button.onClick.AddListener(() => OnButtonClicked(index));
                }
            }
            
            if (buttons.Count == 0)
            {
                Debug.LogWarning("ButtonSelector: No interactable buttons found in buttonParent!");
            }
        }
        
        void DetectLayoutType()
        {
            if (buttonParent == null) return;
            
            // Check for layout group components
            VerticalLayoutGroup verticalLayout = buttonParent.GetComponent<VerticalLayoutGroup>();
            HorizontalLayoutGroup horizontalLayout = buttonParent.GetComponent<HorizontalLayoutGroup>();
            
            if (verticalLayout != null)
            {
                isVerticalLayout = true;
            }
            else if (horizontalLayout != null)
            {
                isVerticalLayout = false;
            }
            else
            {
                // Default to vertical if no layout component found
                isVerticalLayout = true;
            }
        }
        
        void HandleInput()
        {
            if (buttons.Count == 0) return;
            
            bool navigationChanged = false;
            int previousIndex = currentSelectedIndex;
            
            if (isVerticalLayout)
            {
                // Vertical Layout: Up/Down arrows
                if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
                {
                    currentSelectedIndex--;
                    navigationChanged = true;
                }
                else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
                {
                    currentSelectedIndex++;
                    navigationChanged = true;
                }
            }
            else
            {
                // Horizontal Layout: Left/Right arrows
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
                {
                    currentSelectedIndex--;
                    navigationChanged = true;
                }
                else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
                {
                    currentSelectedIndex++;
                    navigationChanged = true;
                }
            }
            
            // Wrap around
            if (currentSelectedIndex < 0)
                currentSelectedIndex = buttons.Count - 1;
            else if (currentSelectedIndex >= buttons.Count)
                currentSelectedIndex = 0;
            
            // Update visuals if selection changed
            if (navigationChanged && previousIndex != currentSelectedIndex)
            {
                OnNavigate();
            }
            
            // Enter/Space to click selected button
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                OnButtonPressed();
            }
        }
        
        void OnNavigate()
        {
            PlaySound(navigateSound);
            UpdateButtonVisuals();
            RestartFadeAnimation();
            UpdateArrowPosition();
        }
        
        void OnButtonPressed()
        {
            if (buttons.Count == 0 || currentSelectedIndex < 0 || currentSelectedIndex >= buttons.Count)
                return;
            
            Button selectedButton = buttons[currentSelectedIndex];
            if (selectedButton != null && selectedButton.interactable)
            {
                // Show pressed color feedback
                ShowPressedFeedback();
                
                PlaySound(selectSound);
                
                // Invoke the button's onClick event
                selectedButton.onClick.Invoke();
            }
        }
        
        void OnButtonClicked(int index)
        {
            // Update selection when button is clicked with mouse
            if (index >= 0 && index < buttons.Count)
            {
                currentSelectedIndex = index;
                UpdateButtonVisuals();
                RestartFadeAnimation();
                UpdateArrowPosition();
            }
        }
        
        void UpdateButtonVisuals()
        {
            for (int i = 0; i < buttons.Count; i++)
            {
                if (buttonTexts[i] != null)
                {
                    if (i == currentSelectedIndex)
                    {
                        // Selected button
                        buttonTexts[i].color = selectedColor;
                    }
                    else
                    {
                        // Normal button
                        buttonTexts[i].color = normalColor;
                    }
                }
            }
        }
        
        #region Arrow Indicator
        
        void UpdateArrowPosition()
        {
            if (arrowPrefab == null) return;
            if (currentSelectedIndex < 0 || currentSelectedIndex >= buttons.Count) return;
            
            Button selectedButton = buttons[currentSelectedIndex];
            TextMeshProUGUI selectedText = buttonTexts[currentSelectedIndex];
            
            if (selectedButton == null) return;
            
            // Create arrow instance if it doesn't exist
            if (currentArrowInstance == null)
            {
                currentArrowInstance = Instantiate(arrowPrefab);
                currentArrowInstance.name = "ArrowIndicator";
            }
            
            // Parent arrow to the selected button
            currentArrowInstance.transform.SetParent(selectedButton.transform, false);
            
            // Calculate position based on text width
            float textWidth = 0f;
            if (selectedText != null)
            {
                // Get the preferred width of the text
                textWidth = selectedText.preferredWidth;
            }
            
            // Position arrow to the left of the text
            RectTransform arrowRect = currentArrowInstance.GetComponent<RectTransform>();
            if (arrowRect != null)
            {
                arrowRect.anchorMin = new Vector2(0.5f, 0.5f);
                arrowRect.anchorMax = new Vector2(0.5f, 0.5f);
                arrowRect.pivot = new Vector2(0.5f, 0.5f);
                
                float xPosition = -(textWidth / 2f) + arrowOffsetX;
                arrowRect.anchoredPosition = new Vector2(xPosition, 0f);
            }
            
            // Animate arrow if enabled
            if (animateArrow)
            {
                AnimateArrow();
            }
        }
        
        void AnimateArrow()
        {
            if (currentArrowInstance == null) return;
            
            // Kill existing animation
            if (arrowBounceTween != null)
            {
                arrowBounceTween.Kill();
            }
            
            RectTransform arrowRect = currentArrowInstance.GetComponent<RectTransform>();
            if (arrowRect != null)
            {
                Vector2 originalPos = arrowRect.anchoredPosition;
                Vector2 bouncePos = originalPos + new Vector2(-arrowBounceDistance, 0f);
                
                arrowBounceTween = arrowRect.DOAnchorPos(bouncePos, arrowBounceDuration)
                                           .SetLoops(-1, LoopType.Yoyo)
                                           .SetEase(Ease.InOutSine)
                                           .SetUpdate(true);
            }
        }
        
        void DestroyArrow()
        {
            if (arrowBounceTween != null)
            {
                arrowBounceTween.Kill();
                arrowBounceTween = null;
            }
            
            if (currentArrowInstance != null)
            {
                Destroy(currentArrowInstance);
                currentArrowInstance = null;
            }
        }
        
        #endregion
        
        void ShowPressedFeedback()
        {
            if (currentSelectedIndex < 0 || currentSelectedIndex >= buttonTexts.Count) return;
            
            TextMeshProUGUI currentText = buttonTexts[currentSelectedIndex];
            if (currentText == null) return;
            
            // Stop fade animation temporarily
            StopFadeAnimation();
            
            // Flash pressed color
            Sequence pressSequence = DOTween.Sequence();
            pressSequence.Append(currentText.DOColor(pressedColor, 0.1f));
            pressSequence.Append(currentText.DOColor(selectedColor, 0.1f));
            pressSequence.OnComplete(() => {
                // Restart fade animation after press feedback
                StartFadeAnimation();
            });
        }
        
        void StartFadeAnimation()
        {
            if (!enableFadeAnimation) return;
            if (currentSelectedIndex < 0 || currentSelectedIndex >= buttonTexts.Count) return;
            
            TextMeshProUGUI currentText = buttonTexts[currentSelectedIndex];
            if (currentText == null) return;
            
            StopFadeAnimation();
            
            // Ensure starting with selected color
            currentText.color = selectedColor;
            
            // Create breathing fade effect
            currentFadeTween = currentText.DOFade(fadeMinAlpha, fadeDuration)
                                          .SetLoops(-1, LoopType.Yoyo)
                                          .SetEase(fadeEase)
                                          .SetUpdate(true);
        }
        
        void StopFadeAnimation()
        {
            if (currentFadeTween != null)
            {
                currentFadeTween.Kill();
                currentFadeTween = null;
            }
            
            // Reset alpha to full
            if (currentSelectedIndex >= 0 && currentSelectedIndex < buttonTexts.Count)
            {
                TextMeshProUGUI currentText = buttonTexts[currentSelectedIndex];
                if (currentText != null)
                {
                    Color col = currentText.color;
                    col.a = fadeMaxAlpha;
                    currentText.color = col;
                }
            }
        }
        
        void RestartFadeAnimation()
        {
            StopFadeAnimation();
            StartFadeAnimation();
        }
        
        void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
        
        /// <summary>
        /// Manually set the selected button index
        /// </summary>
        public void SetSelectedIndex(int index)
        {
            if (index >= 0 && index < buttons.Count)
            {
                currentSelectedIndex = index;
                UpdateButtonVisuals();
                RestartFadeAnimation();
                UpdateArrowPosition();
            }
        }
        
        /// <summary>
        /// Get the currently selected button
        /// </summary>
        public Button GetSelectedButton()
        {
            if (currentSelectedIndex >= 0 && currentSelectedIndex < buttons.Count)
                return buttons[currentSelectedIndex];
            return null;
        }
        
        /// <summary>
        /// Get the currently selected index
        /// </summary>
        public int GetSelectedIndex()
        {
            return currentSelectedIndex;
        }
        
        void OnDestroy()
        {
            StopFadeAnimation();
            DestroyArrow();
            
            // Clean up button listeners
            foreach (Button button in buttons)
            {
                if (button != null)
                {
                    button.onClick.RemoveAllListeners();
                }
            }
        }
    }
}
