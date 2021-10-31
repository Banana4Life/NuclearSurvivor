using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class SphereScript : MonoBehaviour
{
    private Entity entity;
    
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

    private void Update()
    {
        World.DefaultGameObjectInjectionWorld.EntityManager.SetComponentData(entity, new Translation()
        {
            Value = new float3(transform.position)
        });
    }
}
