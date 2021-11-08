using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class SeamlessLinkTraversal : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 linkTarget;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.autoTraverseOffMeshLink = false;
        StartCoroutine(StartCo());
    }

    IEnumerator StartCo()
    {
        while (true)
        {
            if (agent.isOnOffMeshLink)
            {
                linkTarget = agent.currentOffMeshLinkData.endPos;
                agent.transform.LookAt(linkTarget);
                agent.Warp(transform.position);
                yield return StartCoroutine(NormalSpeed(agent));
                agent.Warp(linkTarget);
            }

            yield return null;
        }
    }

    IEnumerator NormalSpeed(NavMeshAgent agent)
    {
        while (agent.transform.position != linkTarget)
        {
            agent.transform.position =
                Vector3.MoveTowards(agent.transform.position, linkTarget, agent.speed * Time.deltaTime);
            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        
        var agent = GetComponent<NavMeshAgent>();
        if (agent.isOnOffMeshLink)
        {
            var mld = agent.currentOffMeshLinkData;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(mld.startPos, mld.endPos);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(mld.startPos, Vector3.one * 0.2f);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(mld.endPos, 0.2f);
        }
            
      
    }
}

