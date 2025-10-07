using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScaleAnimation : MonoBehaviour
{
    [SerializeField] private Image _image;
    [SerializeField] private CanvasGroup _canvasGroup;

    private void OnEnable()
    {
        // start animation looping until load menu scene additively
        StartAnimation();
    }

    private void StartAnimation()
    {
        _image.DOFade(1f, 5f).From(0f).SetEase(Ease.Linear).SetUpdate(true).OnComplete(() =>
        {
            _canvasGroup.DOFade(0, 0.25f).SetEase(Ease.Linear).SetUpdate(true);
        });
    }
}
