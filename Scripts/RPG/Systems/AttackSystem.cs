using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(PerceptionSystem))]
	public partial struct AttackSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

			var job = new AttackJob
			{
				deltaTime = SystemAPI.Time.DeltaTime,
				targetTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
				healthLookup = SystemAPI.GetComponentLookup<Health>(true),
				ecb = ecb
			};
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(Alive))]
		private partial struct AttackJob : IJobEntity
		{
			public float deltaTime;
			[ReadOnly] public ComponentLookup<LocalTransform> targetTransformLookup;
			[ReadOnly] public ComponentLookup<Health> healthLookup;
			public EntityCommandBuffer.ParallelWriter ecb;

			private void Execute([ChunkIndexInQuery] int chunkIndex,
						  ref State state,
						  ref Attack attack,
						  ref Target target,
						  ref Velocity velocity,
						  ref ActiveAnim activeAnim,
						  in Perception perception,
						  in LocalTransform transform)
			{
				if (state.Value != AgentState.Attack) return;

				// Validate target still valid and alive
				if (target.Entity == Entity.Null || !targetTransformLookup.HasComponent(target.Entity))
				{
					state.Value = AgentState.Wander; target.Entity = Entity.Null; return;
				}

				// If target died (health <= 0), drop it
				if (healthLookup.TryGetComponent(target.Entity, out var targetHealth) && targetHealth.Value <= 0f)
				{
					state.Value = AgentState.Wander; target.Entity = Entity.Null; return;
				}

				var myPos = transform.Position;
				var targetPos = targetTransformLookup[target.Entity].Position;
				float dsq = math.lengthsq(new float3(targetPos.x - myPos.x, targetPos.y - myPos.y, 0));
				if (dsq > perception.AttackRadiusSq)
				{
					state.Value = AgentState.Chase;
					return;
				}

				// Match animation speed to attack speed (attacks per second)
				float attacksPerSecond = math.max(0.01f, attack.AttackSpeed);
				activeAnim.State = AnimState.Attack;
				activeAnim.Loop = true; // continuous attack while in range
				activeAnim.Speed = attacksPerSecond; // animator interprets as playback speed multiplier

				// In range: hold position and attempt to attack when off cooldown
				velocity.Value = float3.zero;
				attack.CooldownTimer -= deltaTime;
				if (attack.CooldownTimer <= 0f)
				{
					ecb.AppendToBuffer(chunkIndex, target.Entity, new Damage { Value = attack.DamagePerHit });
					attack.CooldownTimer = 1f / attacksPerSecond;
				}
			}
		}
	}
}


