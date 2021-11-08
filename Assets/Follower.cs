using UnityEngine;
using UnityEngine.AI;

public class Follower : MonoBehaviour
{
    private LeaderAgent leaderAgent;
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.enabled = true;
    }

    private float updateTime;

    private void Update()
    {
        updateTime -= Time.deltaTime;
        if (updateTime < 0)
        {
            agent.destination = leaderAgent.transform.position;
            updateTime = 0.2f;
        }
    }

    public void Init(LeaderAgent leaderAgent)
    {
        this.leaderAgent = leaderAgent;
        transform.position = leaderAgent.transform.position;
    }
}
