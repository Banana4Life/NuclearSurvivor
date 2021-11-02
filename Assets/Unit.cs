using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    public Team team;


    public float attackRange = 5f;
    private bool attacking;
    
    public float attacksPerSecond = 1f;
    private float attackTime;
    
    public float bobbingSpeed = 1f;
    private bool bobbingUp;

    private float ttl = 5f;

    public GameObject projectilePrefab;
    public GameObject renderObject;
    
    public GameObject mainTarget;
    public GameObject target;
    private float updateTarget;

    // Sub-Scripts
    private Sensors sensors;
    private TeamMaterial[] materials;
    private NavMeshAgent agent;
    
    public void Init(Team team, Vector3 pos, GameObject mainTarget)
    {
        transform.position = pos;
        name = "Unit " + team;
        this.team = team;
        ttl = 5f;
        attackTime = Random.Range(0, 1 / attacksPerSecond);
        gameObject.SetActive(true);
        materials = GetComponentsInChildren<TeamMaterial>();
        sensors = GetComponentInChildren<Sensors>();
        agent = GetComponent<NavMeshAgent>();
        foreach (var material in materials)
        {
            material.GetComponent<MeshRenderer>().materials = material.teamColors.First(t => t.team == team).materials;
        }

        this.mainTarget = mainTarget;
        foreach (var projTarget in GetComponentsInChildren<ProjectileTarget>())
        {
            projTarget.Init(team, gameObject, false);
        }
    }
    
    private void Update()
    {
        sensors.CleanupInactiveUnits();
        updateTarget -= Time.deltaTime;
        if (!target.activeSelf || updateTarget <= 0)
        {
            updateTarget = 0.5f;
            target = sensors.LocateNearestEnemy(this);
            if (target == null)
            {
                target = mainTarget;
            }
            agent.destination = target.transform.position;
            if (target == mainTarget)
            {
                agent.isStopped = false;
                agent.stoppingDistance = 0.1f;
            }
            else
            {
                agent.isStopped = agent.remainingDistance < attackRange;
            }
        }
        
        attacking = agent.remainingDistance < attackRange;

        // TTL
        // ttl -= Time.deltaTime;
        // if (ttl < 0)
        // {
        //     gameObject.SetActive(false);
        // }
        
        // Bobbing
        float bob = (bobbingUp ? 1 : -1) * bobbingSpeed * Time.deltaTime;
        var bobY = renderObject.transform.position.y;
        bobbingUp = !(bobY > 1) && (bobY < 0 || bobbingUp);
        renderObject.transform.Translate(0, bob, 0);
        attackTime -= Time.deltaTime;
        if (attacking && attackTime < 0)
        {
            attackTime = 1f / attacksPerSecond;
            Projectile projectile = Game.Pooled<Projectile>(projectilePrefab);
            projectile.Init(target.transform.position, gameObject, projectilePrefab);
            Game.audioPool().PlaySound(ClipGroup.PEW1, transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (agent)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.transform.position);
            var lastCorner = transform.position;
        
            foreach (var corner in agent.path.corners)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(lastCorner, corner);
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(corner, 0.5f);
                lastCorner = corner;
            }
        }
        
    }
}

public enum Team
{
    RED,
    BLUE,
    GREEN
}

[Serializable]
public struct TeamColors
{
    public Team team;
    public Material[] materials;
}