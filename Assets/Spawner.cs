using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : ComponentSystem
{
    protected override void OnCreate()
    {
     
    }

    protected override void OnUpdate()
    {
        Entities.ForEach((ref MyEntity spawner) =>
        {
            spawner.timeUntil -= Time.DeltaTime;
            if (spawner.timeUntil < 0)
            {
                spawner.timeUntil = spawner.deltaTime;
                var entity = EntityManager.Instantiate(spawner.prefab);
                EntityManager.SetComponentData(entity, new Translation()
                {
                    Value = new float3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f))
                });
                EntityManager.AddComponentData(entity, new NonUniformScale()
                {
                    Value = new float3(Random.Range(0.5f, 2f), Random.Range(0.5f, 2f) , Random.Range(0.5f, 2f))
                });
            }
        });
    }
}
