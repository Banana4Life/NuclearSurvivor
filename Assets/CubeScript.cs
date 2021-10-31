using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = UnityEngine.Random;

public class CubeScript : MonoBehaviour, IConvertGameObjectToEntity
{

    public void Convert(Entity entity, EntityManager em, GameObjectConversionSystem conversionSystem)
    {
        em.AddComponentData(entity, Speed.Of(1));
        em.AddComponentData(entity, new NonUniformScale()
        {
            Value = new float3(0.5f)
        });
        em.AddComponentData(entity, new Ttl()
        {
            value = 5f
        });
        em.AddComponent<Target>(entity);
    }
}
