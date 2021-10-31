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
        public ComponentTypeHandle<Translation> translationHandle;
        public ComponentTypeHandle<Speed> speedHandle;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var tChunk = batchInChunk.GetNativeArray(rotatationHandle);
            var pChunk = batchInChunk.GetNativeArray(translationHandle);
            var sChunk = batchInChunk.GetNativeArray(speedHandle);
            for (var i = 0; i < tChunk.Length; i++)
            {
                var rotation = tChunk[i];
                var speed = sChunk[i];
                var pos = pChunk[i];
                pChunk[i] = new Translation()
                {
                    Value = pos.Value + new float3(0,  (speed.up ? speed.speed : -speed.speed) * deltaTime, 0)
                };
                sChunk[i] = new Speed()
                {
                    speed = speed.speed,
                    up = !(pos.Value.y > 1) && (pos.Value.y < 0 || speed.up)
                };
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
        var pos = GetComponentTypeHandle<Translation>();
        var moveJob = new MoveJob()
        {
            rotatationHandle = rot,
            translationHandle = pos,
            speedHandle = speed,
            deltaTime = Time.DeltaTime
        };

        Dependency = moveJob.ScheduleParallel(query, 1, Dependency);
    }
}
