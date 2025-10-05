// Assets/Scripts/Systems/FrustumCullingSystem.cs
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

[BurstCompile]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(UpdateCameraFrustumSystem))]
public partial struct FrustumCullingSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CameraFrustumPlanes>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var planes = SystemAPI.GetSingleton<CameraFrustumPlanes>();

        new CullingJob
        {
            L = planes.L, R = planes.R, B = planes.B,
            T = planes.T, N = planes.N, F = planes.F
        }.ScheduleParallel();
    }

    [BurstCompile]
    [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)] // <— KEY LINE
    public partial struct CullingJob : IJobEntity
    {
        public float4 L, R, B, T, N, F;

        void Execute(in WorldRenderBounds wrb, EnabledRefRW<VisibleTag> visible)
        {
            var aabb = wrb.Value;

            bool inside =
                InsideAabb(aabb.Center, aabb.Extents, L) &&
                InsideAabb(aabb.Center, aabb.Extents, R) &&
                InsideAabb(aabb.Center, aabb.Extents, B) &&
                InsideAabb(aabb.Center, aabb.Extents, T) &&
                InsideAabb(aabb.Center, aabb.Extents, N) &&
                InsideAabb(aabb.Center, aabb.Extents, F);

            // Only write when the state actually changes (avoids extra work)
            if (visible.ValueRO != inside)
                visible.ValueRW = inside;
        }

        static bool InsideAabb(float3 c, float3 e, float4 p)
        {
            float3 n = p.xyz; float d = p.w;
            float r = math.dot(n, c) + d;
            float s = math.dot(math.abs(n), e);
            return (r + s) >= 0f;
        }
    }
}
