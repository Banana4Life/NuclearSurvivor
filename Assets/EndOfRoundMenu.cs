using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EndOfRoundMenu : MonoBehaviour
{
    private static Score playerScore;
    private static Score enemyScore;

    public Text playerScoreTotal;
    public Text playerScoreCube;
    public Text playerScoreBat;
    public Text playerScoreFood;
    
    public Text enemyScoreTotal;
    public Text enemyScoreCube;
    public Text enemyScoreBat;
    public Text enemyScoreFood;

    public int cubeMulti = 250;
    public int batMulti = 100;
    public int foodMulti = 300;
    
    private void Start()
    {
        playerScoreTotal.text = (playerScore.cubes * cubeMulti + playerScore.batteries * batMulti + playerScore.foodz * foodMulti).ToString();
        playerScoreCube.text = playerScore.cubes.ToString();
        playerScoreBat.text = playerScore.batteries.ToString();
        playerScoreFood.text = playerScore.foodz.ToString();
        
        enemyScoreTotal.text = (enemyScore.cubes * cubeMulti + enemyScore.batteries * batMulti + enemyScore.foodz * foodMulti).ToString();
        enemyScoreCube.text = enemyScore.cubes.ToString();
        enemyScoreBat.text = enemyScore.batteries.ToString();
        enemyScoreFood.text = enemyScore.foodz.ToString();
        
    }

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

    public static void InitScore(Game game, LeaderAgent player, LeaderAgent enemy)
    {
        // TODO do stuff with it
        playerScore = new Score()
        {
            batteries = player.points[Interactable.Type.BATTERY],
            cubes = player.points[Interactable.Type.CUBE],
            foodz = player.points[Interactable.Type.FOOD],
        };
        enemyScore = new Score()
        {
            batteries = enemy.points[Interactable.Type.BATTERY],
            cubes = enemy.points[Interactable.Type.CUBE],
            foodz = enemy.points[Interactable.Type.FOOD],
        };

    }
    
    public struct Score
    {
        public int batteries;
        public int cubes;
        public int foodz;
    }
}
