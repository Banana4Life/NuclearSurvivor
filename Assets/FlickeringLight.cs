using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

// Based on https://gist.github.com/sinbad/4a9ded6b00cf6063c36a4837b15df969
// Written by Steve Streeting 2017
// License: CC0 Public Domain http://creativecommons.org/publicdomain/zero/1.0/
public class FlickeringLight : MonoBehaviour
{
    public float minIntensity = 0f;
    public float maxIntensity = 1f;

    private Light light;
    private float lastSum;
    private Queue<float> smoothQueue = new();
    public int smoothness = 5;
    private void Start()
    {
        light = GetComponent<Light>();
    }

    void Update()
    {
        if (light == null)
        {
            return;
        }
        while (smoothQueue.Count >= smoothness)
        {
            lastSum -= smoothQueue.Dequeue();
        }
        
        var newVal = Random.Range(minIntensity, maxIntensity);
        smoothQueue.Enqueue(newVal);
        lastSum += newVal;
        light.intensity = Mathf.Lerp(light.intensity, lastSum / smoothQueue.Count, Time.deltaTime * 20);
    }

}
