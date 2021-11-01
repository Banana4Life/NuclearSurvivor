using System;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 velocity;
    public float speed = 20f;

    private ParticleSystem ps;
    private GameObject source;

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

    public void Init(Vector3 target, GameObject source)
    {
        velocity = (target - transform.position).normalized * speed;
        this.source = source;
        gameObject.SetActive(true);
        GetComponent<Rigidbody>().detectCollisions = true;
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

    public void Collide(GameObject other)
    {
        if (other != source && other.GetComponent<Unit>().team != source.GetComponent<Unit>().team)
        {
            KillProjectile();
            GetComponent<Rigidbody>().detectCollisions = false;
            other.SetActive(false);
            Game.SpawnFloaty("HIT", transform.position);
            Game.audioPool().PlaySound(ClipGroup.PROJ_HIT, transform.position);
        }
        
    }
}
