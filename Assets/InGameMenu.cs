using UnityEngine;

public class InGameMenu : MonoBehaviour
{
    public RectTransform leftTime;
    public RectTransform rightTime;

    public Game game;
    
    private RectTransform thisRect;
    void Start()
    {
        thisRect = GetComponent<RectTransform>();
    }

    void Update()
    {
        leftTime.sizeDelta = new Vector2(leftTime.sizeDelta.x, thisRect.rect.height * (game.timeLeft / 60f));
        rightTime.sizeDelta = new Vector2(rightTime.sizeDelta.x, thisRect.rect.height * (game.timeLeft / 60f));
    }
}
