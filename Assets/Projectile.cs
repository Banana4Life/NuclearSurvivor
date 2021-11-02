using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 velocity;
    public float speed = 20f;

    private ParticleSystem ps;
    private GameObject source;

    private Team team;

    private void Start()
    {
        ps = GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (!ps.IsAlive())
        {
            gameObject.SetActive(false);
        }

        transform.position += velocity * Time.deltaTime;
    }

    public void Init(Vector3 target, GameObject source, GameObject prefab)
    {
        transform.position = source.transform.position;
        velocity = (target - transform.position).normalized * speed;
        this.source = source;
        name = prefab.name;
        gameObject.SetActive(true);
        GetComponent<Rigidbody>().detectCollisions = true;
        team = source.GetComponent<Unit>().team;
    }

    public void KillProjectile()
    {
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];
        var particlesAlive = ps.GetParticles(particles);
        for (int i = 0; i < particlesAlive; i++)
        {
            particles[i].remainingLifetime = 0;
        }
        ps.SetParticles(particles);
        velocity = Vector3.zero;
    }

    public bool Collide(GameObject other, Team otherTeam)
    {
        if (other != source && otherTeam != team)
        {
            KillProjectile();
            GetComponent<Rigidbody>().detectCollisions = false;
            
            Game.SpawnFloaty("HIT", transform.position);
            Game.audioPool().PlaySound(ClipGroup.PROJ_HIT, transform.position);
            return true;
        }

        return false;

    }
}
