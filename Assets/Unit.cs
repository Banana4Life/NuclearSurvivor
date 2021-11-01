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

    public float rotSpeed = 5f;

    public float spaceBetween = 3f;

    public GameObject target;

    public float attackRange = 5f;
    private bool attacking;
    
    public float attacksPerSecond = 1f;
    private float attackTime;
    
    public float bobbingSpeed = 1f;
    private bool bobbingUp;

    private float ttl = 5f;

    public bool invincible;

    public GameObject projectilePrefab;

    // Sub-Scripts
    private Sensors sensors;
    private TeamMaterial materials;
    
    public void Init(Team team, Vector3 pos)
    {
        transform.position = pos;
        name = "Unit " + team;
        this.team = team;
        ttl = 5f;
        attackTime = Random.Range(0, 1 / attacksPerSecond);
        gameObject.SetActive(true);
        materials = GetComponent<TeamMaterial>();
        sensors = GetComponentInChildren<Sensors>();
        GetComponent<MeshRenderer>().materials = materials.teamColors.First(t => t.team == team).materials;
    }
    
    private void Update()
    {
        var oldPos = transform.position;

        var enemy = sensors.LocateNearestEnemy(this);
        if (enemy != null)
        {
            target = enemy.gameObject;
            // Rotate To Target
            var targetDir = target.transform.position - oldPos;
            targetDir = new Vector3(targetDir.x, 0, targetDir.z);
            var toRotation = Quaternion.LookRotation(targetDir, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, rotSpeed * Time.deltaTime);
            
            // Check for Attack Range
            if ((enemy.transform.position - oldPos).sqrMagnitude > attackRange * attackRange)
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
        }
        else
        {
            speed = 0;
            // TODO find enemy base instead
        }

        // TTL
        ttl -= Time.deltaTime;
        if (ttl < 0)
        {
            gameObject.SetActive(false);
            return;
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
                Projectile projectile = Game.Pooled<Projectile>(projectilePrefab);
                projectile.Init(target.transform.position, gameObject, projectilePrefab);
                Game.audioPool().PlaySound(ClipGroup.PEW1, transform.position);
            }
        }

        // TODO avoidance/separation
        // foreach (var liveUnit in liveUnits)
        // {
        //     if (liveUnit != gameObject)
        //     {
        //         var distance = (finalPos - liveUnit.transform.position);
        //         distance = new Vector3(distance.x, 0, distance.z);
        //         if (distance.sqrMagnitude <= spaceBetween * spaceBetween)
        //         {
        //             finalPos += distance.normalized * 1/Math.Max(1f, distance.sqrMagnitude) * 5f * Time.deltaTime;
        //         }
        //     }
        // }

        transform.position = finalPos;
    }
    
    void OnCollisionEnter(Collision collision)
    {
        var projectile = collision.collider.GetComponent<Projectile>();
        if (projectile)
        {
            projectile.Collide(gameObject, invincible);
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