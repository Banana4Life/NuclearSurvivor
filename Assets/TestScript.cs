using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class TestScript : SystemBase
{
    private EntityQuery query;
    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(Rotation), typeof(Speed));
    }

    struct MoveJob : IJobEntityBatch
    {
        public float deltaTime;
        public ComponentTypeHandle<Rotation> rotatationHandle;
        public ComponentTypeHandle<Speed> speedHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var tChunk = batchInChunk.GetNativeArray(rotatationHandle);
            var sChunk = batchInChunk.GetNativeArray(speedHandle);
            for (var i = 0; i < tChunk.Length; i++)
            {
                var rotation = tChunk[i];
                var speed = sChunk[i];
                tChunk[i] = new Rotation()
                {
                    Value = math.mul(math.normalize(rotation.Value),
                        quaternion.AxisAngle(math.up(), speed.speed * deltaTime))
                };
            }
        }
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        var rot = GetComponentTypeHandle<Rotation>();
        var speed = GetComponentTypeHandle<Speed>();
        var moveJob = new MoveJob()
        {
            rotatationHandle = rot,
            speedHandle = speed,
            deltaTime = Time.DeltaTime
        };

        Dependency = moveJob.ScheduleParallel(query, 1, Dependency);
    }
}
