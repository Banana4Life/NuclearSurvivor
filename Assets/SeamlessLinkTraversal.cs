using UnityEngine;
using UnityEngine.AI;

public class SeamlessLinkTraversal : MonoBehaviour
{
    private NavMeshAgent agent;
    // Start is called before the first frame update
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (!agent.enabled)
        {
            return;
        }

        if (agent.isOnOffMeshLink)
        {
            agent.CompleteOffMeshLink();
            agent.isStopped = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (agent && agent.isOnOffMeshLink)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 1);
        }
        
    }
}
