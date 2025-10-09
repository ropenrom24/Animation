//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Transforms;
//using RPG.Components;
//using Unity.Rendering;

//// Runs in the simulation group, potentially after movement systems
//// but before rendering transform calculations.
//[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateAfter(typeof(TransformSystemGroup))] // Ensure it runs after potential movement updates
//[BurstCompile]
//public partial struct CharacterYtoZSortSystem : ISystem
//{
//    // Small factor to prevent large Z values and potential precision issues
//    // Make this larger if characters are very close in Y but need distinct sorting.
//    // Make it smaller if Z values become too large.
//    private const float Y_TO_Z_FACTOR = 0.001f;

//    [BurstCompile]
//    public void OnCreate(ref SystemState state)
//    {
//        // Require LocalTransform (already implicit in query) and EITHER HeroTag OR MonsterTag
//        // This RequireForUpdate ensures the system only runs if matching entities exist.
//        state.RequireForUpdate(SystemAPI.QueryBuilder()
//            .WithAny<NPCTag, MonsterTag>()
//            .WithAll<LocalTransform, Alive, VisibleTag>()
//            .Build());
//    }

//    [BurstCompile]
//    public void OnDestroy(ref SystemState state)
//    {
//        // No cleanup needed
//    }

//    [BurstCompile]
//    public void OnUpdate(ref SystemState state)
//    {
//        // Create the job instance
//        var job = new CharacterYtoZSortJob
//        {
//            YToZFactor = Y_TO_Z_FACTOR
//        };

//        // Schedule the job to run in parallel on entities matching the job's query.
//        // The query for the job is implicitly defined by its Execute method signature
//        // and filtered by the WithAny clause on the job struct.
//        // Pass the current dependency chain handle.
//        state.Dependency = job.ScheduleParallel(state.Dependency);
//    }
//}

//// The job itself
//[BurstCompile]
//// Process entities that have LocalTransform AND (HeroTag OR MonsterTag)
//[WithAny(typeof(NPCTag), typeof(MonsterTag))]
//[WithAll(typeof(Alive), typeof(VisibleTag))]
//// FIX: Exclude entities that also have PooledObjectTag OR IsDeadTag
//[WithNone(typeof(DisableRendering))]
//public partial struct CharacterYtoZSortJob : IJobEntity
//{
//    // Make Y_TO_Z_FACTOR accessible within the job
//    [ReadOnly] public float YToZFactor;

//    // The Execute method contains the core logic for each matching entity.
//    // Using RefRW<LocalTransform> for write access to the transform component.
//    // Using [WithAll] implicitly requires LocalTransform due to the parameter.
//    public void Execute(ref LocalTransform transform)
//    {
//        float currentY = transform.Position.y;
//        // Invert the sign: Lower Y should have smaller Z (closer to camera)
//        float adjustedZ = currentY * YToZFactor;

//        // Update only the Z component of the position
//        transform.Position.z = adjustedZ;
//    }
//}

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using RPG.Components; // keep your tags/namespaces
using Unity.Rendering;

// Runs in the simulation group (after transforms/movement)
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct CharacterYtoZSortSystem : ISystem
{
    // Tunables: tweak these per your scene
    // yToZFactor: how strongly Y maps to Z (smaller = smaller Z differences)
    // baseZOffset: global offset so all characters sit at that Z baseline
    // jitterScale: tiny stable per-entity offset to avoid exact ties
    private const float DEFAULT_Y_TO_Z_FACTOR = 0.001f;
    private const float DEFAULT_BASE_Z_OFFSET = 0f;
    private const float DEFAULT_JITTER_SCALE = 1e-6f; // very small stable offset
    private const float MIN_Z = -100f;
    private const float MAX_Z = 100f;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Ensure this system only runs if there are matching entities and required components
        state.RequireForUpdate(SystemAPI.QueryBuilder()
            .WithAny<NPCTag, MonsterTag>()
            .WithAll<LocalTransform, LocalToWorld, Alive, VisibleTag>()
            .Build());
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new CharacterYtoZSortJob
        {
            YToZFactor = DEFAULT_Y_TO_Z_FACTOR,
            BaseZOffset = DEFAULT_BASE_Z_OFFSET,
            JitterScale = DEFAULT_JITTER_SCALE,
            MinZ = MIN_Z,
            MaxZ = MAX_Z
        };

        // Wire job handle into system dependency chain
        state.Dependency = job.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAny(typeof(NPCTag), typeof(MonsterTag))]
[WithAll(typeof(Alive), typeof(VisibleTag), typeof(LocalTransform), typeof(LocalToWorld))]
[WithNone(typeof(DisableRendering))]
public partial struct CharacterYtoZSortJob : IJobEntity
{
    [ReadOnly] public float YToZFactor;
    [ReadOnly] public float BaseZOffset;
    [ReadOnly] public float JitterScale;
    [ReadOnly] public float MinZ;
    [ReadOnly] public float MaxZ;

    // We need the entity so we can create a tiny stable jitter per entity
    public void Execute(Entity entity, ref LocalTransform transform, in LocalToWorld ltw)
    {
        // Use world Y for sorting (correct when entity is parented)
        float worldY = ltw.Position.y;

        // Decide sign so smaller worldY yields smaller Z (closer to camera).
        // If your camera faces +Z instead of -Z, flip the sign (remove the negative).
        float zFromY = -worldY * YToZFactor;

        // Stable jitter: use entity.Index (an int) to produce a tiny unique offset
        // This avoids sprite Z ties. Keep it tiny to not visually move items.
        float jitter = (entity.Index & 0xFFFF) * JitterScale;

        float adjustedZ = BaseZOffset + zFromY + jitter;

        // Clamp to safe range to avoid precision problems
        adjustedZ = math.clamp(adjustedZ, MinZ, MaxZ);

        // Write back only the Z component of the LocalTransform position
        float3 pos = transform.Position;
        pos.z = adjustedZ;
        transform.Position = pos;
    }
}
