using System;
using UnityEngine;

public class IrradiationEffect : MonoBehaviour
{
    public Light l;
    public ParticleSystem ps;
    public float maxLight;

    public bool active;

    private void Start()
    {
        l.intensity = 0;
    }

    private void Update()
    {
        if (active)
        {
            l.intensity = Math.Min(l.intensity + Time.deltaTime, maxLight);
        }
        else
        {
            l.intensity = Math.Max(0, l.intensity - Time.deltaTime);
        }

    }

    public void Activate()
    {
        ps.Play();
        active = true;
    }
    
    public void Deactivate()
    {
        ps.Stop();
        active = false;
    }
}
