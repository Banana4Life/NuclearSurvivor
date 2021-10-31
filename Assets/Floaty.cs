using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Floaty : MonoBehaviour
{
    public Animator animator;
    public Text dmgText;
    private Vector3 worldPos;

    void Start()
    {
        var clipInfo = animator.GetCurrentAnimatorClipInfo(0);
        Destroy(gameObject, clipInfo[0].clip.length);
    }

    void Update()
    {
        transform.position = Camera.main.WorldToScreenPoint(worldPos);
    }
    
    public void Init(string text, Vector3 worldPos)
    {
        dmgText.text = text;
        this.worldPos = worldPos;
        transform.position = Camera.main.WorldToScreenPoint(worldPos);
    }
}
