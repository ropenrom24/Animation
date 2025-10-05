using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

[
    BurstCompile,
    UpdateInGroup(typeof(InitializationSystemGroup)),
    UpdateAfter(typeof(RPG.Systems.BootstrapSystem))
]
public partial struct FlipbookSpawnerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<FlipbookSpawner>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var spawner = SystemAPI.GetSingleton<FlipbookSpawner>();

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Strongly centered HP bar defaults (tile-local UV). Offset is bar CENTER.
        // Make the change very noticeable: much shorter width and placed very high.
        float2 hpSize = new float2(0.5f, 0.010f);  // slightly wider bar
        float2 hpOffset = new float2(0.5f, 0.85f);
        float4 npcHpTint = new float4(0.2f, 0.8f, 1f, 1f);
        float4 monHpTint = new float4(1f, 0.3f, 0.2f, 1f);
        float4 backTint = new float4(0f, 0f, 0f, 0.65f);

        // NPCs
        if (spawner.NpcPrefab != Entity.Null && spawner.NpcCount > 0)
        {
            var npcProto = ecb.Instantiate(spawner.NpcPrefab);
            ecb.AddComponent(npcProto, new Velocity { Value = float3.zero });
            ecb.AddComponent(npcProto, new Heading { Value = new float3(0, 1, 0) });
            ecb.AddComponent(npcProto, new Health { Value = 100f, Max = 100f });
            ecb.AddComponent(npcProto, new MoveSpeed { Value = 1f });
            ecb.AddComponent(npcProto, new State { Value = AgentState.Wander, StateTimer = 1f });
            ecb.AddComponent(npcProto, new Target { Entity = Entity.Null, LastKnownPosition = float3.zero, DistanceSq = float.MaxValue });
            ecb.AddComponent(npcProto, new Perception { SenseRadius = 5f, SenseRadiusSq = 25f, AttackRadius = 1f, AttackRadiusSq = 1f });
            ecb.AddComponent(npcProto, new Attack { DamagePerHit = 10f, AttackSpeed = 1f, CooldownTimer = 0f });
            ecb.AddComponent(npcProto, new WanderParams { MinIdleSeconds = 0.5f, MaxIdleSeconds = 2.5f, DirectionChangeInterval = 1.25f, Timer = 1f });
            ecb.AddComponent(npcProto, new Team { Value = 0 });
            ecb.AddComponent(npcProto, new RandomState { Rng = new Unity.Mathematics.Random(1u) });
            ecb.AddComponent(npcProto, new PerceptionControl { CooldownSeconds = 0.1f, JitterFraction = 0.25f, MaxPerCellChecks = 12, MaxAgentChecks = 96, Timer = 0.1f });
            ecb.AddComponent<HpBarVisible>(npcProto);
            ecb.AddComponent(npcProto, new RecentlyDamaged { Timer = 0f });
            ecb.AddComponent<Alive>(npcProto);
            ecb.AddBuffer<Damage>(npcProto);
            ecb.AddComponent<VisibleTag>(npcProto);
            // HP bar defaults
            ecb.AddComponent(npcProto, new Hp01 { Value = 1f });
            ecb.AddComponent(npcProto, new HpBarSize { Value = hpSize });
            ecb.AddComponent(npcProto, new HpBarOffset { Value = hpOffset });
            ecb.AddComponent(npcProto, new HpBarTint { Value = npcHpTint });
            ecb.AddComponent(npcProto, new HpBarBackTint { Value = backTint });

            var rng = new Unity.Mathematics.Random(0xABCDEFu);
            for (int i = 0; i < spawner.NpcCount; i++)
            {
                var e = ecb.Instantiate(npcProto);
                float2 dir = rng.NextFloat2Direction();
                float r = rng.NextFloat(0f, math.max(0f, spawner.SpawnRadius));
                float2 p = dir * r;
                ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(new float3(p.x, p.y, 0f), quaternion.identity, 1f));
                ecb.SetComponent(e, new Team { Value = 0 });
                ecb.AddComponent<NPCTag>(e);
            }
            ecb.DestroyEntity(npcProto);
        }

        // Monsters
        if (spawner.MonsterPrefab != Entity.Null && spawner.MonsterCount > 0)
        {
            var monProto = ecb.Instantiate(spawner.MonsterPrefab);
            ecb.AddComponent(monProto, new Velocity { Value = float3.zero });
            ecb.AddComponent(monProto, new Heading { Value = new float3(0, 1, 0) });
            ecb.AddComponent(monProto, new Health { Value = 100f, Max = 100f });
            ecb.AddComponent(monProto, new MoveSpeed { Value = 1f });
            ecb.AddComponent(monProto, new State { Value = AgentState.Wander, StateTimer = 1f });
            ecb.AddComponent(monProto, new Target { Entity = Entity.Null, LastKnownPosition = float3.zero, DistanceSq = float.MaxValue });
            ecb.AddComponent(monProto, new Perception { SenseRadius = 5f, SenseRadiusSq = 25f, AttackRadius = 1f, AttackRadiusSq = 1f });
            ecb.AddComponent(monProto, new Attack { DamagePerHit = 10f, AttackSpeed = 1f, CooldownTimer = 0f });
            ecb.AddComponent(monProto, new WanderParams { MinIdleSeconds = 0.5f, MaxIdleSeconds = 2.5f, DirectionChangeInterval = 1.25f, Timer = 1f });
            ecb.AddComponent(monProto, new Team { Value = 1 });
            ecb.AddComponent(monProto, new RandomState { Rng = new Unity.Mathematics.Random(777u) });
            ecb.AddComponent(monProto, new PerceptionControl { CooldownSeconds = 0.1f, JitterFraction = 0.25f, MaxPerCellChecks = 12, MaxAgentChecks = 96, Timer = 0.1f });
            ecb.AddComponent<HpBarVisible>(monProto);
            ecb.AddComponent(monProto, new RecentlyDamaged { Timer = 0f });
            ecb.AddComponent<Alive>(monProto);
            ecb.AddBuffer<Damage>(monProto);
            ecb.AddComponent<VisibleTag>(monProto);
            // HP bar defaults
            ecb.AddComponent(monProto, new Hp01 { Value = 1f });
            ecb.AddComponent(monProto, new HpBarSize { Value = hpSize });
            ecb.AddComponent(monProto, new HpBarOffset { Value = hpOffset });
            ecb.AddComponent(monProto, new HpBarTint { Value = monHpTint });
            ecb.AddComponent(monProto, new HpBarBackTint { Value = backTint });

            var rng2 = new Unity.Mathematics.Random(0xFEDCBAu);
            for (int i = 0; i < spawner.MonsterCount; i++)
            {
                var e = ecb.Instantiate(monProto);
                float2 dir = rng2.NextFloat2Direction();
                float r = rng2.NextFloat(0f, math.max(0f, spawner.SpawnRadius));
                float2 p = dir * r;
                ecb.SetComponent(e, LocalTransform.FromPositionRotationScale(new float3(p.x, p.y, 0f), quaternion.identity, 1f));
                ecb.SetComponent(e, new Team { Value = 1 });
                ecb.AddComponent<MonsterTag>(e);
            }
            ecb.DestroyEntity(monProto);
        }

        // one-shot spawner
        ecb.DestroyEntity(SystemAPI.GetSingletonEntity<FlipbookSpawner>());
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}
