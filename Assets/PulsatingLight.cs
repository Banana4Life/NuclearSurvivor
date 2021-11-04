using UnityEngine;

public class PulsatingLight : MonoBehaviour
{
    private Light l;
    private float dt;
    void Start()
    {
        l = GetComponent<Light>();
        var colors = new[] { Color.green, Color.yellow, Color.red, Color.blue };
        l.color = colors[Random.Range(0, colors.Length)];
    }
    
    void Update()
    {
        dt += Time.deltaTime * 2f;
        l.intensity = (Mathf.Sin(dt) + 2f) * 3f;
    }
}
