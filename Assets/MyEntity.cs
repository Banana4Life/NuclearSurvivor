using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class MyEntity : MonoBehaviour
{
    public GameObject prefab;
    private float timeUntil;
    public float delay = 0.1f;

    private Entity entityPrefab;
    private BlobAssetStore blobAssetStore;
    
    private void Start()
    {
        blobAssetStore = new BlobAssetStore();
        entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab,
            GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore));
    }

    private void OnDestroy()
    {
        blobAssetStore.Dispose();
    }

    private void Update()
    {
        timeUntil -= Time.deltaTime;
        if (timeUntil < 0)
        {
            timeUntil = delay;
            var em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var entity = em.Instantiate(entityPrefab);
            em.SetComponentData(entity, new Translation()
            {
                Value = new float3(Random.Range(-15f, 15f), 0, Random.Range(-5f, 5f))
            });
            em.AddComponentData(entity, new NonUniformScale()
            {
                Value = new float3(0.5f)
            });
            em.AddComponentData(entity, new Ttl()
            {
                value = 5f
            });
        }
    }

}