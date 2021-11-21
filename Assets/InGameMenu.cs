using UnityEngine;
using UnityEngine.UI;

public class InGameMenu : MonoBehaviour
{
    public RectTransform leftTime;
    public RectTransform rightTime;

    public Text cubePoints; 
    public Text batteryPoints; 
    public Text applePoints; 

    public Game game;
    
    private RectTransform parentTimeRect;
    void Start()
    {
        parentTimeRect = leftTime.parent.GetComponent<RectTransform>();
    }

    void Update()
    {
        // Timer
        leftTime.sizeDelta = new Vector2(leftTime.sizeDelta.x, parentTimeRect.rect.height * (game.timeLeft / 60f));
        rightTime.sizeDelta = new Vector2(rightTime.sizeDelta.x, parentTimeRect.rect.height * (game.timeLeft / 60f));

        // Points
        batteryPoints.text = game.player.points[Interactable.Type.BATTERY].ToString();
        cubePoints.text = game.player.points[Interactable.Type.CUBE].ToString();
        applePoints.text = game.player.points[Interactable.Type.FOOD].ToString();
    }
}
