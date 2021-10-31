using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;
        Entities.ForEach((ref Translation pos, in Rotation rot, in Speed speed) =>
        {
            var forward = math.forward(rot.Value) * speed.value * dt;
            pos.Value = pos.Value + new float3(forward.x, 0, forward.z);
        }).ScheduleParallel();
    }
}

public class BobbingSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float dt = Time.DeltaTime;
        Entities.ForEach((ref Translation pos, ref Bobbing bobbing) =>
        {
            float bob = (bobbing.up ? 1 : -1) * bobbing.speed * dt;
            pos.Value = pos.Value + new float3(0, bob, 0);
            bobbing.up = !(pos.Value.y > 1) && (pos.Value.y < 0 || bobbing.up);
        }).ScheduleParallel();
    }
}

public class DespawnSystem : SystemBase
{
    private EntityQuery _query;
    private EndSimulationEntityCommandBufferSystem _endSimulationEcbSystem;
    
    protected override void OnCreate()
    {
        _query = GetEntityQuery(typeof(Ttl));
        _endSimulationEcbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    /// <summary>
    /// https://forum.unity.com/threads/creating-and-destroying-entities-from-a-job.1119883/#post-7203187
    /// </summary>
    [BurstCompile]
    struct DespawnJob : IJobEntityBatch
    {
        public float DeltaTime;
        public ComponentTypeHandle<Ttl> TtlHandle;
        [ReadOnly]
        public EntityTypeHandle EntityTypeHandle;
        public EntityCommandBuffer.ParallelWriter CommandBufferWriter;

        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            var entities = batchInChunk.GetNativeArray(EntityTypeHandle);
            var chunk = batchInChunk.GetNativeArray(TtlHandle);
            for (var i = 0; i < chunk.Length; i++)
            {
                var ttl = chunk[i];
                ttl.value -= DeltaTime;
                if (ttl.value < 0)
                {
                    CommandBufferWriter.DestroyEntity(i, entities[i]);
                }
                chunk[i] = ttl;
            }
        }
    }

    protected override void OnUpdate()
    {
        var destroyBufferWriter = _endSimulationEcbSystem.CreateCommandBuffer().AsParallelWriter();
        var entityTypeHandle = GetEntityTypeHandle();
        var job = new DespawnJob()
        {
            TtlHandle = GetComponentTypeHandle<Ttl>(),
            EntityTypeHandle = entityTypeHandle,
            DeltaTime = Time.DeltaTime,
            CommandBufferWriter = destroyBufferWriter,
        };
        Dependency = job.ScheduleParallel(_query, 1, Dependency);
        
        _endSimulationEcbSystem.AddJobHandleForProducer(Dependency);
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

