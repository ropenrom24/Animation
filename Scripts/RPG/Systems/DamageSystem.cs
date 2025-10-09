using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;
using EcsDamageBubbles;

namespace RPG.Systems
{
    // Applies accumulated damage into Health, clears the buffer,
    // and refreshes RecentlyDamaged.Timer so the bar stays visible briefly.
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(AttackSystem))]
    [UpdateBefore(typeof(DeathSystem))]
    public partial struct DamageSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

            new ApplyDamageJob
            {
                ecb = ecb,
                ShowSecs = 1.25f, // how long to keep the bar visible after a hit
                tintDamageLasts = 0.5f
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)]
        private partial struct ApplyDamageJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public float ShowSecs;
            public float tintDamageLasts;

            private void Execute(
                [ChunkIndexInQuery] int chunkIndex,
                ref Health health,
                ref State state,
                ref Velocity velocity,
                ref ActiveAnim active,
                EnabledRefRW<Alive> alive,
                EnabledRefRO<VisibleTag> visible,
                ref RecentlyDamaged recentlyDamaged,
                in LocalTransform transform,
                Entity entity,
                DynamicBuffer<Damage> damageBuffer)
            {
                if (!alive.ValueRO) return;
                if (damageBuffer.Length == 0) return;

                float total = 0f;
                for (int i = 0; i < damageBuffer.Length; i++) total += damageBuffer[i].Value;

                if (total > 0f && visible.ValueRO)
                {
                    var requestEntity = ecb.CreateEntity(chunkIndex);
                    float3 spawnPos = transform.Position + new float3(0, 0.17f, 0);
                    ecb.AddComponent(chunkIndex, requestEntity, new DamageBubbleRequest
                    {
                        Value = (int)math.round(total),
                        ColorId = 2
                    });
                    ecb.AddComponent(chunkIndex, requestEntity, new LocalTransform
                    {
                        Position = spawnPos,
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    ecb.AddComponent(chunkIndex, requestEntity, new LocalToWorld
                    {
                        Value = float4x4.Translate(spawnPos)
                    });
                }
                damageBuffer.Clear();

                float before = health.Value;
                float after  = math.max(0f, before - total);
                health.Value = after;

                // Timer-only visibility: refresh on every hit
                recentlyDamaged.Timer = math.max(recentlyDamaged.Timer, ShowSecs); // or: = ShowSecs;
                recentlyDamaged.TintTimer = math.max(recentlyDamaged.TintTimer, tintDamageLasts);

                if (before > 0f && after <= 0f)
                {
                    ecb.AddComponent(chunkIndex, entity, new DeathStartedTag { Timer = 1.5f });
                }
            }
        }
    }
}
