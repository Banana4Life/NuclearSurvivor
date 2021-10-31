using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class SphereScript : MonoBehaviour
{
    private Entity entity;
    public Floaty floatyPrefab;
    public GameObject canvas;
    
    private void Start()
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;
        entity = em.CreateEntity();
        em.AddComponentData(entity, new Translation()
        {
            Value = new float3(Random.Range(-15f, 15f), 0, Random.Range(-5f, 5f))
        });
        em.AddComponent<PotentialTargetTag>(entity);
    }

    private void OnParticleCollision(GameObject other)
    {
        List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
        other.GetComponent<ParticleSystem>().GetCollisionEvents(gameObject, collisionEvents);
        foreach (var collisionEvent in collisionEvents)
        {
            if (collisionEvent.intersection.sqrMagnitude == 0) continue;
            var floaty = Instantiate(floatyPrefab, canvas.transform, false);
            floaty.Init("HIT", collisionEvent.intersection);
        }
    }

    private void Update()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(entity, new Translation()
        {
            Value = new float3(transform.position)
        });
    }
}
