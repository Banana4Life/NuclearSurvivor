using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void ExitButton()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        #endif
        Application.Quit();
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Scenes/Main");
    }

    public void ContinueGame()
    {
        SceneManager.LoadScene("Scenes/Main");
    }

}
