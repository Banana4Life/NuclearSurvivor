using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MovementSystem : SystemBase
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
                // tChunk[i] = new Rotation()
                // {
                //     Value = math.mul(math.normalize(rotation.Value),
                //         quaternion.AxisAngle(math.up(), speed.speed * deltaTime))
                // };
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

public class DespawnSystem : SystemBase
{
    private EntityQuery query;
    EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;
    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(Ttl));
        m_EndSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    struct DespawnJob : IJobEntityBatch
    {
        public float deltaTime;
        public ComponentTypeHandle<Ttl> ttl;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var chunk = batchInChunk.GetNativeArray(ttl);
            for (var i = 0; i < chunk.Length; i++)
            {
                var ttl = chunk[i];
                ttl.value -= deltaTime;
                chunk[i] = new Ttl()
                {
                    value = ttl.value -= deltaTime
                };
            }
        }
    }

    [BurstCompile]
    protected override void OnUpdate()
    {
        var cmdBuf = m_EndSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
        var despawnJob = new DespawnJob()
        {
            ttl = GetComponentTypeHandle<Ttl>(),
            deltaTime = Time.DeltaTime,
        };
        Dependency = despawnJob.ScheduleParallel(query, 1, Dependency);
        
        Entities.ForEach((Entity entity, int entityInQueryIndex, ref Ttl ttl) =>
        {
            if (ttl.value < 0)
            {
                cmdBuf.DestroyEntity(entityInQueryIndex, entity);
            }
        }).ScheduleParallel();
        
        m_EndSimulationEcbSystem.AddJobHandleForProducer(Dependency);
    }
}



public class RotateToTargetSystem : SystemBase
{

    [BurstCompile]
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Rotation rotation, in Translation translation, in Target target) =>
        {
            var pos = translation.Value;
            var targetPos = target.value;
            var direction = new float3(targetPos.x - pos.x, 0, targetPos.z - pos.z);
            rotation.Value = quaternion.LookRotation(direction, math.up());
        }).ScheduleParallel();
    }
}

