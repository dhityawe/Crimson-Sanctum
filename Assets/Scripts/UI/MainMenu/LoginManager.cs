using Assets.Scripts.Player.API;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoginManager : MonoBehaviour
{
    [SerializeField] Button login, register;
    [SerializeField] TMP_InputField loginInput, registerInput;
    [SerializeField] string mainMenuSceneName = "MainMenu";

    public void Login()
    {
        PlayerDataManager.Instance.LoadPlayerFromServer(
            nickname: loginInput.text,
            onSuccess: () => 
            {
                Debug.Log("Sukses load player");
                LoadMainMenu();
            },
            onError: (error) => Debug.Log($"gagal load pemain: {error}")
        );
    }

    public void Register()
    {
        PlayerDataManager.Instance.CreateNewPlayer(
            nickname: registerInput.text,
            onSuccess: () => 
            {
                Debug.Log("Sukses nambah player");
                LoadMainMenu();
            },
            onError: (error) => Debug.LogError($"Gagal nambah pemain: {error}")
        );
    }
    
    private void LoadMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
