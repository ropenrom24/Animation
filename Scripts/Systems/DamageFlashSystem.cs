using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using RPG.Components;

namespace RPG.Systems
{
    /// <summary>
    /// Flashes character red when taking damage by modulating AnimTint.
    /// Uses RecentlyDamaged.Timer to control the flash intensity.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    public partial struct DamageFlashSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RecentlyDamaged>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new FlashJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,

                // Tunables
                FlashDuration = 0.5f,      // How long the flash lasts (should match or be <= RecentlyDamaged duration)
                FlashColor = new float4(1f, 0.2f, 0.2f, 1f),  // Red tint (RGB + alpha)
                NormalColor = new float4(1f, 1f, 1f, 1f),     // White (no tint)
                FlashIntensity = 1f,     // How much red to mix in (0-1)
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Alive), typeof(VisibleTag))]
        private partial struct FlashJob : IJobEntity
        {
            public float DeltaTime;
            public float FlashDuration;
            public float4 FlashColor;
            public float4 NormalColor;
            public float FlashIntensity;

            private void Execute(
                in RecentlyDamaged recentlyDamaged,
                ref AnimTint tint)
            {
                // Calculate flash intensity based on remaining timer
                // Timer counts down from FlashDuration to 0
                float t = math.saturate(recentlyDamaged.TintTimer / FlashDuration);

                // Use a curve for more impact at the start
                // You can experiment with different curves:
                // - Linear: t
                // - Exponential decay: t * t
                // - Smooth: smoothstep(0, 1, t)
                float flashAmount = t * t * FlashIntensity;

                // Lerp between normal and flash color
                float4 targetColor = math.lerp(NormalColor, FlashColor, flashAmount);

                // Smooth transition
                float k = 1f - math.exp(-12f * DeltaTime);
                tint.Value = math.lerp(tint.Value, targetColor, k);
            }
        }
    }
}