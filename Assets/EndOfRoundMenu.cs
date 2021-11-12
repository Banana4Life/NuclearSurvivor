using UnityEngine;

public class EndOfRoundMenu : MonoBehaviour
{

    private Game game;
    public void NextRound()
    {
        gameObject.SetActive(false);
        game.NextRound();
    }
    
    public void ExitButton()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            return;
        #endif
        Application.Quit();
    }


    public void EndRound(Game game, LeaderAgent player)
    {
        this.game = game;
        gameObject.SetActive(true);
    }
}
