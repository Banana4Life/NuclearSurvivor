using UnityEngine;
using UnityEngine.SceneManagement;

public class EndOfRoundMenu : MonoBehaviour
{

    public void NextRound()
    {
        SceneManager.LoadScene("Main");
    }
    
    public void ExitButton()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public static void Score(Game game, LeaderAgent player)
    {
        // TODO do stuff with it
    }
}
