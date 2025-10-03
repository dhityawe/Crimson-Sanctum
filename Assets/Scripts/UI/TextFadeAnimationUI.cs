using DG.Tweening;
using TMPro;
using UnityEngine;

public class TextFadeAnimationUI : MonoBehaviour
{
    [SerializeField] private float fadeDuration, fadeMinAlpha, fadeMaxAlpha;
    private TMP_Text _textAnimation;

    private void Awake()
    {
        _textAnimation = GetComponent<TMP_Text>();
    }

    private void OnEnable()
    {
        _textAnimation.DOFade(fadeMinAlpha, fadeDuration)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine)
            .SetUpdate(true);
    }

    private void StopAnimation()
    {
        _textAnimation.DOKill();
    }

    private void OnDisable()
    {
        StopAnimation();
    }
}
