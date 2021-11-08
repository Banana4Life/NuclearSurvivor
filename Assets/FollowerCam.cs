using System;
using UnityEngine;

public class FollowerCam : MonoBehaviour
{
    
    public LeaderAgent player;
    
    public float maxPanSpeed = 2f;
    public float panAcceleration = 5f;
    public float panBreaking = 2f;
    public float panLimit = 10;
    public float panStopLimit = 9;
    private bool panning;
    private float panSpeed;
    
    private void Update()
    {
   
        var camDelta = (transform.position - player.transform.position).sqrMagnitude;
        if (camDelta > panLimit * panLimit)
        {
            panning = true;
        }
        else if (camDelta < panStopLimit * panStopLimit)
        {
            panning = false;
        }
        
        panSpeed = Math.Max(0, Math.Min(panSpeed + (panning ? panAcceleration : -panBreaking) * Time.deltaTime, maxPanSpeed));
        transform.position = Vector3.Lerp(transform.position, player.transform.position, panSpeed * Time.deltaTime);
    }
}
