using UnityEngine;

public class ProjectileTarget : MonoBehaviour
{
    public bool invincible;
    public GameObject owner;
    public Team team;
    void OnCollisionEnter(Collision collision)
    {
        var projectile = collision.collider.GetComponent<Projectile>();
        if (projectile)
        {
            if (projectile.Collide(gameObject, team))
            {
                if (!invincible)
                {
                    owner.SetActive(false);
                }
            }
        }
    }

    public void Init(Team team, GameObject owner, bool invincible)
    {
        this.team = team;
        this.owner = owner;
        this.invincible = invincible;
    }
}
