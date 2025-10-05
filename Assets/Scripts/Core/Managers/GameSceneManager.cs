using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;


public class GameSceneManager : MonoBehaviour
{
    private static readonly WaitForSeconds _waitForSeconds0_5 = new(0.5f);
    public static GameSceneManager Instance;

    // references, loading ui, mintime
    [Header("References components")]
    [SerializeField] private LoadingSceneUI _loadingSceneUI;
    [Space]
    [SerializeField, Range(1f, 5f)] private float _minTimeLoading = 4.5f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            return;
        }
        Destroy(gameObject);
    }

    public void LoadingScene(string sceneName)
    {
        StartCoroutine(LoadingSceneAsync(sceneName));
    }

    public void Reload()
    {
        StartCoroutine(LoadingSceneAsync(SceneManager.GetActiveScene().name));
    }

    private IEnumerator LoadingSceneAsync(string sceneName)
    {
        _loadingSceneUI.ShowLoadingScene();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            if (operation.progress >= 0.9f && timer >= _minTimeLoading)
            {
                yield return _waitForSeconds0_5;
                operation.allowSceneActivation = true;
            }
            yield return null;
        }

        _loadingSceneUI.HideLoadingScene();
    }
}