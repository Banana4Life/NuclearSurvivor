using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Unit : MonoBehaviour
{
    public Team team;

    public float speed;
    public float maxSpeed = 3f;

    public float spaceBetween = 3f;

    public GameObject target;

    public float attackRange = 5f;
    private bool attacking;
    
    public float attacksPerSecond = 1f;
    private float attackTime;
    
    public float bobbingSpeed = 1f;
    private bool bobbingUp;

    private float ttl = 5f;
    private List<GameObject> liveUnits;

    public void Init(string name, Team team, List<GameObject> liveUnits)
    {
        this.name = name;
        this.liveUnits = liveUnits;
        this.team = team;
        this.ttl = 5f;
        this.attackTime = Random.Range(0, 1 / attacksPerSecond);
        gameObject.SetActive(true);
    }
    
    private void Update()
    {
        var oldPos = transform.position;
        
        // Find Target
        var enemyUnits = liveUnits
            .Where(unit => unit != gameObject)
            .Select(unit => unit.GetComponent<Unit>())
            .Where(unitScript => unitScript.team != team)
            .ToList();

        Unit enemy = null;
        float sqrEnemyDistance = float.MaxValue;
        foreach (var enemyUnit in enemyUnits)
        {
            var d = (oldPos - enemyUnit.transform.position).sqrMagnitude;
            if (sqrEnemyDistance > d)
            {
                sqrEnemyDistance = d;
                enemy = enemyUnit;
            }
        }
        if (enemy != null)
        {
            target = enemy.gameObject;
            // Rotate To Target
            var targetPos = target.transform.position;
            targetPos = new Vector3(targetPos.x, 0, targetPos.z);
            transform.LookAt(targetPos);
        }

        // TTL
        ttl -= Time.deltaTime;
        if (ttl < 0)
        {
            gameObject.SetActive(false);
            return;
        }
        
        // Check for Attack Range
        if (sqrEnemyDistance > attackRange * attackRange)
        {
            // Walk to Enemy
            speed = maxSpeed; // TODO lerp
            attacking = false;
        }
        else
        {
            // Stop to Attack
            speed = 0;
            attacking = true;
        }
        
        // Movement
        var forward = transform.forward * speed;
        float bob = (bobbingUp ? 1 : -1) * bobbingSpeed;
        bobbingUp = !(oldPos.y > 1) && (oldPos.y < 0 || bobbingUp);
        var finalPos = transform.position;
        finalPos += new Vector3(forward.x, bob, forward.z) * Time.deltaTime;    
        
        if (attacking)
        {
            attackTime -= Time.deltaTime;
            if (attackTime < 0)
            {
                attackTime = 1f / attacksPerSecond;
                Game.projPool().LaunchProjectile(gameObject, target.transform.position);
                Game.audioPool().PlaySound(ClipGroup.PEW1, transform.position);
            }
        }

        foreach (var liveUnit in liveUnits)
        {
            if (liveUnit != gameObject)
            {
                var distance = (finalPos - liveUnit.transform.position);
                distance = new Vector3(distance.x, 0, distance.z);
                if (distance.sqrMagnitude <= spaceBetween * spaceBetween)
                {
                    finalPos += distance.normalized * 1/distance.sqrMagnitude * 5f * Time.deltaTime;
                }
            }
        }

        transform.position = finalPos;
    }
    private void OnTriggerEnter(Collider other)
    {
        var projectile = other.GetComponent<Projectile>();
        if (projectile)
        {
            Debug.Log("HIT");
            projectile.Collide(gameObject);
        }
    }

}

public enum Team
{
    RED,
    BLUE
}
