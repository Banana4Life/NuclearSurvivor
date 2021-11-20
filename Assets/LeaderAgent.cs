using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class LeaderAgent : MonoBehaviour
{
    public IrradiationEffect radiationLight;

    public GameObject followerPrefab;

    public TileGenerator generator;
    
    public float irradiated;
    public bool auto;

    private NavMeshAgent agent;
    private Plane plane = new(Vector3.up, Vector3.zero);
    public Dictionary<Interactable.Type, int> points = new();

    private HashSet<Room> visited = new();
    public Room currentRoom;
    
    public AudioSource pickupAudio;

    private bool holding;

    public float baseSpeed = 10f;
    public float boostedSpeed = 20f;

    public float boosted;

    private Animator animator;
    public float moving;
    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
        foreach (Interactable.Type pickupType in Enum.GetValues(typeof(Interactable.Type)))
        {
            points[pickupType] = 0;
        }
        // agent.destination = Vector3.zero;
    }

    private void Update()
    {
        moving = agent.velocity.sqrMagnitude == 0 ? moving - Time.deltaTime : 0.2f;
        if (animator)
        {
            animator.SetFloat("movespeed", moving <= 0 ? 0.1f : 1);
        }
        
        irradiated -= Time.deltaTime;
        boosted -= Time.deltaTime;
        if (irradiated < 0)
        {
            radiationLight.Deactivate();
        }

        agent.speed = Mathf.Lerp(agent.speed,  boosted > 0 ? boostedSpeed : baseSpeed, Time.deltaTime * 10);
        
        if (agent.isOnNavMesh)
        {
            if (auto)
            {
                AutoDestination();
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    holding = true;
                }
                if (Input.GetMouseButtonUp(0))
                {
                    holding = false;
                }
                if (holding)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (plane.Raycast(ray, out var dist)) // We have no height at the moment
                    {
                        var targetPos = ray.GetPoint(dist);
                        if (NavMesh.SamplePosition(targetPos, out var hit, 200f, NavMesh.AllAreas))
                        {
                            agent.SetDestination(hit.position);
                        }
                    }    
                }
            }
        }
    }

    private void AutoDestination()
    {
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            // Debug.Log("Autopilot Retargeting");
            Vector3 newDest = Vector3.zero;
            // Find nearby pickups
            if (currentRoom != null)
            {
                var interactable = currentRoom.TileArea.gameObject.GetComponentsInChildren<Interactable>()
                    .Where(i => (transform.position - i.transform.position).sqrMagnitude < 100 * 100)
                    .ToList().Shuffled().FirstOrDefault();
                if (interactable != null)
                {
                    newDest = interactable.transform.position;
                    agent.destination = newDest;
                    // Debug.Log($"{gameObject.name} pickup thing at {newDest}");
                    return;
                }
                // Debug.Log($"{gameObject.name} cannot find anything in Room {currentRoom.RoomCoord}");
            }

            // If none found move to another room
            Room room = generator.FindNearbyRoom(transform.position, visited, 150);
            if (room == null)
            {
                Debug.Log("No more rooms?");
                return;
            }

            var path = new NavMeshPath();
            if (agent.CalculatePath(room.WorldCenter, path) && path.status == NavMeshPathStatus.PathComplete)
            {
                agent.SetPath(path);
                visited.Add(room);
                currentRoom = room;
                // Debug.Log($"{gameObject.name} pathing to {room.RoomCoord}{room.TileArea.transform.position}");
            }
            else
            {   
                // Debug.Log($"{gameObject.name} cannot path to {room.WorldCenter}");
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (agent)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, agent.destination);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(agent.destination, 0.5f);    
        }
    }

    public void Pickup(Interactable.Type type)
    {
        if (pickupAudio)
        {
            pickupAudio.PlayOneShot(pickupAudio.clip);
        }
        
        switch (type)
        {
            case Interactable.Type.BARREL:
                boosted = 15f;
                break;
            case Interactable.Type.CUBE:
                irradiated = 15f;
                radiationLight.Activate();
                Instantiate(followerPrefab, transform.parent).GetComponent<Follower>().Init(this);
                break;
        }

        if (!points.TryGetValue(type, out var value))
        {
            value = 0;
        }

        points[type] = value + 1;
    }
}
