using UnityEngine;
using UnityEngine.AI;

public class Follower : MonoBehaviour
{
    private Player player;
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
            agent.destination = player.transform.position;
            updateTime = 0.2f;
        }
    }

    public void Init(Player player)
    {
        this.player = player;
        transform.position = player.transform.position;
    }
}
