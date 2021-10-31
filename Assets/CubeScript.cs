using System;
using Unity.Entities;
using UnityEngine;

public class CubeScript : MonoBehaviour, IConvertGameObjectToEntity
{

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var speed = new Speed()
        {
            speed = 1f
        };
        dstManager.AddComponentData(entity, speed);
    }
}
