using UnityEngine;
using TMPro;
using DG.Tweening;

public class MainMenuIntro : MonoBehaviour
{
    [SerializeField] private float mainMenuDuration;
    [SerializeField] private TextMeshProUGUI TitleText;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TitleTextAnim();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void TitleTextAnim()
    {
        if (TitleText == null) 
        {
            Debug.LogError("TitleText is null! Please assign the TextMeshPro component in the inspector.");
            return;
        }
        
        // Get the material from TextMeshPro (use materialForRendering for runtime)
        Material titleMaterial = TitleText.materialForRendering;
        
        if (titleMaterial == null)
        {
            Debug.LogError("TitleText material is null!");
            return;
        }
        
        // Check if the material has the Face Dilate property
        if (!titleMaterial.HasProperty("_FaceDilate"))
        {
            Debug.LogError("Material doesn't have _FaceDilate property! Make sure you're using a TextMeshPro material.");
            return;
        }
        
        Debug.Log($"Starting TitleTextAnim with duration: {mainMenuDuration}");
        
        // Set initial value
        titleMaterial.SetFloat("_FaceDilate", -1f);
        
        // Create a sequence for the Face Dilate animation
        Sequence dilateSequence = DOTween.Sequence();
        
        // Calculate timing for each phase (0 -> -1 -> 1 -> 0)
        float phaseTime = mainMenuDuration / 3f;
        
        Debug.Log($"Phase time: {phaseTime}");
        
        // Phase 1: 0 to -1
        dilateSequence.Append(DOTween.To(() => titleMaterial.GetFloat("_FaceDilate"), 
                                        x => titleMaterial.SetFloat("_FaceDilate", x), 
                                        -1f, phaseTime)
                             .SetEase(Ease.InOutSine)
                             .OnComplete(() => Debug.Log("Phase 1 complete: -1")));
        
        // Phase 2: -1 to 1
        dilateSequence.Append(DOTween.To(() => titleMaterial.GetFloat("_FaceDilate"), 
                                        x => titleMaterial.SetFloat("_FaceDilate", x), 
                                        1f, phaseTime)
                             .SetEase(Ease.InOutSine)
                             .OnComplete(() => Debug.Log("Phase 2 complete: 1")));
        
        // Phase 3: 1 to 0
        dilateSequence.Append(DOTween.To(() => titleMaterial.GetFloat("_FaceDilate"), 
                                        x => titleMaterial.SetFloat("_FaceDilate", x), 
                                        0.5f, phaseTime)
                             .SetEase(Ease.InOutSine)
                             .OnComplete(() => Debug.Log("Phase 3 complete: 0")));
        
        // Play the sequence
        dilateSequence.Play();
        
        Debug.Log("TitleTextAnim sequence started!");
    }
}
