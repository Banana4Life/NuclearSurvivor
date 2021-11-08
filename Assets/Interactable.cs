using UnityEngine;

public class Interactable : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponent<LeaderAgent>();
        if (agent)
        {
            agent.SetIrradiated();
            Destroy(gameObject);
        }
    }
}
