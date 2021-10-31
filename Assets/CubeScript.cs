using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class CubeScript : MonoBehaviour, IConvertGameObjectToEntity
{

    public void Convert(Entity entity, EntityManager em, GameObjectConversionSystem conversionSystem)
    {
        em.AddComponentData(entity, Speed.Of(3));
        em.AddComponentData(entity, new NonUniformScale()
        {
            Value = new float3(0.5f)
        });
        em.AddComponentData(entity, new Ttl()
        {
            value = 5f
        });
        em.AddComponent<Target>(entity);
        em.AddComponentData(entity, new Bobbing()
        {
            speed = 1f
        });
        em.AddComponentData(entity, new AttackRange()
        {
            value = 5f,
            timeUntilMax = 1
        });
    }
}
