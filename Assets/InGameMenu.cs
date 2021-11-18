using UnityEngine;
using UnityEngine.UI;

public class InGameMenu : MonoBehaviour
{
    public RectTransform leftTime;
    public RectTransform rightTime;

    public Text cubePoints; 
    public Text barrelPoints; 

    public Game game;
    
    private RectTransform parentTimeRect;
    void Start()
    {
        parentTimeRect = leftTime.parent.GetComponent<RectTransform>();
    }

    void Update()
    {
        leftTime.sizeDelta = new Vector2(leftTime.sizeDelta.x, parentTimeRect.rect.height * (game.timeLeft / 60f));
        rightTime.sizeDelta = new Vector2(rightTime.sizeDelta.x, parentTimeRect.rect.height * (game.timeLeft / 60f));

        barrelPoints.text = game.player.points.ToString();
        cubePoints.text = 0.ToString();
    }
}
