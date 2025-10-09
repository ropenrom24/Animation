using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using RPG.Components;

namespace RPG.Systems
{
    /// <summary>
    /// Always-on sync:
    ///  • Smooths Hp01 toward Health/Max each frame (exp smoothing).
    ///  • Decrements RecentlyDamaged.Timer each frame (clamped at 0).
    ///  • Visibility is TIMER-ONLY: visible while Timer>0, otherwise fade out.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DamageSystem))]
    [UpdateAfter(typeof(global::FrustumCullingSystem))]
    public partial struct HealthBarSyncSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<Health>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            new SyncJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,

                // Minimal tunables
                DrainRate = 6f,   // slower drain on damage (smoother)
                HealRate  = 12f,  // faster catch-up on heal
                FadeSpeed = 8f,    // alpha fade speed
				BackAlphaFactor = 0.40f
            }.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Alive), typeof(VisibleTag))]
        private partial struct SyncJob : IJobEntity
        {
            public float DeltaTime;
            public float DrainRate;
            public float HealRate;
            public float FadeSpeed;
			public float BackAlphaFactor;
			
            private void Execute(
                in Health health,
                ref Hp01 hp,
                ref HpBarTint barTint,
                ref HpBarBackTint backTint,
                ref RecentlyDamaged recentlyDamaged)
            {
                // 1) Smooth Hp01 toward true fraction (always)
                float target01 = (health.Max > 0f)
                    ? math.saturate(health.Value / math.max(0.0001f, health.Max))
                    : 0f;

                float current01 = math.saturate(hp.Value);  // negatives treated as 0
                float rate      = (target01 < current01) ? DrainRate : HealRate;
                float k         = 1f - math.exp(-FadeSpeed * DeltaTime);
                hp.Value        = math.lerp(current01, target01, k);

                // 2) Decrement "recently damaged" timer (always)
                recentlyDamaged.Timer = math.max(0f, recentlyDamaged.Timer - DeltaTime);
                recentlyDamaged.TintTimer = math.max(0f, recentlyDamaged.TintTimer - DeltaTime);

                // 3) TIMER-ONLY visibility
                float targetA = (recentlyDamaged.Timer > 0f) ? 1f : 0f;
				float targetBackA = targetA * BackAlphaFactor;

                // 4) Fade alphas every frame (no extra conditions)
                float kFade = 1f - math.exp(-FadeSpeed * DeltaTime);

                var bt = barTint.Value;
                var bk = backTint.Value;
                bt.w = math.lerp(bt.w, targetA, kFade);
                bk.w = math.lerp(bk.w, targetBackA, kFade);
                barTint.Value  = bt;
                backTint.Value = bk;
            }
        }
    }
}
