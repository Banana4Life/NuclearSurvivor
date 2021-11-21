using UnityEngine;

public class Interactable : MonoBehaviour
{
    public bool destroyOnInteract = true;
    public Type type;
    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<LeaderAgent>();
        if (agent)
        {
            agent.InteractWith(type);
            if (destroyOnInteract)
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
        BARREL,
        CUBE,
        HIDEOUT
    }
}
