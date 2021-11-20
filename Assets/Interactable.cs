using UnityEngine;

public class Interactable : MonoBehaviour
{
    public Type type;
    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<LeaderAgent>();
        if (agent)
        {
            agent.Pickup(type);
            Destroy(gameObject);
        }
    }

    public enum Type
    {
        BARREL,
        CUBE,
    }
}
