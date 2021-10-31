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

    [BurstCompile]
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
                var forward = math.forward(rotation.Value);

                var multiplier = speed.value * deltaTime;
                pChunk[i] = new Translation()
                {
                    Value = pos.Value + new float3(forward.x * multiplier,  (speed.up ? 1 : -1) * multiplier, forward.z * multiplier)
                };
                sChunk[i] = new Speed()
                {
                    value = speed.value,
                    up = !(pos.Value.y > 1) && (pos.Value.y < 0 || speed.up)
                };
            }
        }
    }

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

    [BurstCompile]
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

public class UpdateTargetSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.ForEach((ref Target target, in Translation translation) =>
        {
            Translation tt = translation;
            Entities.ForEach((in PotentialTargetTag potentialTarget, in Translation pos) =>
            {
                tt = pos;
            }).WithoutBurst().Run();

            target.value = tt.Value;
        }).WithoutBurst().Run();
    }
}

[UpdateBefore(typeof(UpdateTargetSystem))]
public class RotateToTargetSystem : SystemBase
{

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

