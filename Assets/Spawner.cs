using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    public GameObject prefab;
    public ParticleSystem shotParticles;
    private float timeUntil;
    public float delay = 0.1f;
    public GameObject target;

    private Entity entityPrefab;
    private BlobAssetStore blobAssetStore;
    
    private void Start()
    {
        blobAssetStore = new BlobAssetStore();

        var settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);
        entityPrefab = GameObjectConversionUtility.ConvertGameObjectHierarchy(prefab, settings);

        var ps = Instantiate(shotParticles);
        var emissionModule = ps.emission;
        emissionModule.enabled = false;

        World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<VfxSystem>().Init(ps);
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
                Value = new float3(Random.Range(-15f, 15f), 0, Random.Range(-15f, 15f))
            });
        }
    }

}