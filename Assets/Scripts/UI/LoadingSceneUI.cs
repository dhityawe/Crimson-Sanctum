using System.Collections;
using System.Collections.Generic;
using GabrielBigardi.SpriteAnimator;
using TMPro;
using UnityEngine;

public class LoadingSceneUI : MonoBehaviour
{
    [Header("Text Tutorial Component")]
    [SerializeField] private TMP_Text _tutorialText;
    [SerializeField, TextArea(2, 10)] private List<string> _tutorialContents = new();

    [Header("Animation Data Component")]
    [SerializeField] private UISpriteAnimator _uISprite;
    [SerializeField] private List<SpriteAnimationObject> _spriteAnimationObjects = new();

    [Header("Loading Text Component")]
    [SerializeField] private TMP_Text _loadingText;
    [SerializeField, Range(0f, 3f)] private float _interval, _loopTime;

    [ContextMenu("Show Loading Scene")]
    public void ShowLoadingScene()
    {
        // activate scene
        gameObject.SetActive(true);
        // set up sprite nya
        _uISprite.ChangeAnimationObject(_spriteAnimationObjects[Random.Range(0, _spriteAnimationObjects.Count)]);
        _uISprite.Play("Idle");

        // setup tutorial text nya
        _tutorialText.SetText(_tutorialContents[Random.Range(0, _tutorialContents.Count)]);

        // loop loading text nya
        StartCoroutine(LoopLoadingText(_interval, _loopTime));
    }

    private IEnumerator LoopLoadingText(float interval, float loopTime)
    {
        while (true)
        {
            string loadingText = "Loading";
            _loadingText.SetText(loadingText);
            for (int i = 0; i < 3; i++)
            {
                loadingText += ".";
                _loadingText.SetText(loadingText);
                yield return new WaitForSeconds(interval);
            }

            yield return new WaitForSeconds(loopTime);
        }
    }

    public void HideLoadingScene()
    {
        gameObject.SetActive(false);
        StopCoroutine(nameof(LoopLoadingText));
    }
}
