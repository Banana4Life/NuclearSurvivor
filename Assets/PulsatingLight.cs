using UnityEngine;

public class PulsatingLight : MonoBehaviour
{
    private Light light;
    private float dt;
    void Start()
    {
        light = GetComponent<Light>();
        var colors = new[] { Color.green, Color.yellow, Color.red, Color.blue };
        light.color = colors[Random.Range(0, colors.Length)];
    }
    
    void Update()
    {
        dt += Time.deltaTime * 2f;
        light.intensity = (Mathf.Sin(dt) + 2f) * 3f;
    }
}
