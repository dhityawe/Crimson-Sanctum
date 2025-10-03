using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using GabrielBigardi.SpriteAnimator;
using CrimsonSanctum.UI;

public class MainMenuIntro : MonoBehaviour
{
    [SerializeField] private float mainMenuDuration;
    [SerializeField] private Image titleLogo;
    [SerializeField] private Image titleBg;
    [SerializeField] private GameObject UIButtons;
    [SerializeField] private ButtonSelector buttonSelector;
    [SerializeField] private float startDelay = 0.5f; // Delay in seconds before animation starts

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Make UI elements invisible initially
        MakeTextInvisible();
        MakeUIButtonsInvisible();
        
        // Start animation after delay
        Invoke(nameof(StartTitleAnimation), startDelay);
    }
    
    private void MakeTextInvisible()
    {
        if (titleLogo != null)
        {
            // Set alpha to 0 to make image completely invisible
            Color imageColor = titleLogo.color;
            imageColor.a = 0f;
            titleLogo.color = imageColor;
        }

        if (titleBg != null)
        {
            // Set alpha to 0 to make background image completely invisible
            Color bgColor = titleBg.color;
            bgColor.a = 0f;
            titleBg.color = bgColor;
        }
    }
    
    private void MakeUIButtonsInvisible()
    {
        if (UIButtons == null) return;
        
        // Disable all Button components first
        Button[] buttons = UIButtons.GetComponentsInChildren<Button>();
        foreach (Button btn in buttons)
        {
            btn.enabled = false;
        }
        
        // Get all Image components in UIButtons and its children
        Image[] buttonImages = UIButtons.GetComponentsInChildren<Image>();
        foreach (Image img in buttonImages)
        {
            Color buttonColor = img.color;
            buttonColor.a = 0f;
            img.color = buttonColor;
        }
        
        // Get all Text components in UIButtons and its children (if any)
        Text[] buttonTexts = UIButtons.GetComponentsInChildren<Text>();
        foreach (Text txt in buttonTexts)
        {
            Color textColor = txt.color;
            textColor.a = 0f;
            txt.color = textColor;
        }
        
        // Get all TextMeshPro components in UIButtons and its children (if any)
        TMPro.TextMeshProUGUI[] buttonTMPs = UIButtons.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI tmp in buttonTMPs)
        {
            Color tmpColor = tmp.color;
            tmpColor.a = 0f;
            tmp.color = tmpColor;
        }
    }
    
    private void StartTitleAnimation()
    {
        TitleTextAnim();
        UIButtonsAnim();
    }

    private void TitleTextAnim()
    {
        if (titleLogo == null)
        {
            Debug.LogError("titleLogo is null! Please assign the Image component in the inspector.");
            return;
        }

        // Create a sequence for the image animation
        Sequence titleSequence = DOTween.Sequence();

        // Calculate timing for each phase
        float fadeTime = mainMenuDuration * 0.4f; // 40% for fade in
        float scaleTime = mainMenuDuration * 0.6f; // 60% for scale effect

        // Phase 1: Fade in alpha from 0 to 1
        titleSequence.Append(titleLogo.DOFade(1f, fadeTime)
                            .SetEase(Ease.OutQuad));

        // Phase 2: Scale animation for dramatic effect
        // Start the image at normal scale but add a punch scale effect
        titleSequence.Join(titleLogo.transform.DOPunchScale(Vector3.one * 0.1f, scaleTime, 1, 0.5f)
                          .SetEase(Ease.OutBounce));

        // Animate titleBg if it exists
        if (titleBg != null)
        {
            // Fade in the background image alongside the logo
            titleSequence.Join(titleBg.DOFade(1f, fadeTime)
                              .SetEase(Ease.OutQuad));
        }

        // Play the sequence
        titleSequence.Play();
    }
    
    private void UIButtonsAnim()
    {
        if (UIButtons == null)
        {
            Debug.LogError("UIButtons is null! Please assign the GameObject in the inspector.");
            return;
        }
        
        // Create a sequence for the UI buttons animation
        Sequence buttonsSequence = DOTween.Sequence();
        
        // Add a delay so buttons appear after title animation starts
        float buttonDelay = mainMenuDuration * 0.1f; // Start buttons animation at 10% of title duration
        float buttonFadeTime = mainMenuDuration * 0.3f; // Fade duration for buttons
        
        // Get all Image components and animate them
        Image[] buttonImages = UIButtons.GetComponentsInChildren<Image>();
        foreach (Image img in buttonImages)
        {
            buttonsSequence.Join(img.DOFade(1f, buttonFadeTime)
                               .SetDelay(buttonDelay)
                               .SetEase(Ease.OutQuad));
        }
        
        // Get all Text components and animate them
        Text[] buttonTexts = UIButtons.GetComponentsInChildren<Text>();
        foreach (Text txt in buttonTexts)
        {
            buttonsSequence.Join(txt.DOFade(1f, buttonFadeTime)
                               .SetDelay(buttonDelay)
                               .SetEase(Ease.OutQuad));
        }
        
        // Get all TextMeshPro components and animate them
        TMPro.TextMeshProUGUI[] buttonTMPs = UIButtons.GetComponentsInChildren<TMPro.TextMeshProUGUI>();
        foreach (TMPro.TextMeshProUGUI tmp in buttonTMPs)
        {
            buttonsSequence.Join(tmp.DOFade(1f, buttonFadeTime)
                               .SetDelay(buttonDelay)
                               .SetEase(Ease.OutQuad));
        }
        
        // Enable Button components when animation is complete
        buttonsSequence.OnComplete(() => {
            buttonSelector.enabled = true;
            Button[] buttons = UIButtons.GetComponentsInChildren<Button>();
            foreach (Button btn in buttons)
            {
                btn.enabled = true;
            }
        });
        
        // Play the sequence
        buttonsSequence.Play();
    }
}
