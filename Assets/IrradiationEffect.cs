using System;
using UnityEngine;
using UnityEngine.WSA;

public class IrradiationEffect : MonoBehaviour
{
    public Light light;
    public ParticleSystem particleSystem;
    public float maxLight;

    public bool active;

    private void Start()
    {
        light.intensity = 0;
    }

    private void Update()
    {
        if (active)
        {
            light.intensity = Math.Min(light.intensity + Time.deltaTime, maxLight);
        }
        else
        {
            light.intensity = Math.Max(0, light.intensity - Time.deltaTime);
        }

    }

    public void Activate()
    {
        particleSystem.Play();
        active = true;
    }
    
    public void Deactivate()
    {
        particleSystem.Stop();
        active = false;
    }
}
