using UnityEngine;
using UnityEngine.AI;

public class Follower : MonoBehaviour
{
    private LeaderAgent _leaderAgent;
    private NavMeshAgent _agent;
    private float _updateTime;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.enabled = true;
    }

    private void Update()
    {
        if (!_leaderAgent || !_agent.enabled)
        {
            return;
        }
        _updateTime -= Time.deltaTime;
        if (_updateTime < 0)
        {
            _agent.destination = _leaderAgent.transform.position;
            _updateTime = 0.2f;
        }
    }

    public void Init(LeaderAgent leaderAgent)
    {
        this._leaderAgent = leaderAgent;
        transform.position = leaderAgent.transform.position;
    }

    private void OnTriggerEnter(Collider other)
    {
        var cablesContact = other.gameObject.GetComponent<CablesContact>();
        if (cablesContact && cablesContact.Fry(this))
        {
            _agent.enabled = false;
        }
    }
}
