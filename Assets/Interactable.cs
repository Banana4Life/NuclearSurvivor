using UnityEngine;

public class Interactable : MonoBehaviour
{
    public AudioSource enterAudio;
    public GameObject visual;
    
    public bool destroyOnInteract = true;
    public Type type;
    private bool destroy;
    
    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<LeaderAgent>();
        if (agent)
        {
            if (enterAudio)
            {
                enterAudio.Play();
            }
            agent.InteractWith(type);
            destroy = destroyOnInteract;
        }
    }

    private void Update()
    {
        if (destroy)
        {
            if (visual)
            {
                Destroy(visual);
            }
            if (!enterAudio || !enterAudio.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var agent = other.GetComponent<LeaderAgent>();
        if (agent)
        {
            agent.InteractWith(type, false);
        }
    }

    public enum Type
    {
        BATTERY,
        CUBE,
        HIDEOUT,
        FOOD
    }
}
