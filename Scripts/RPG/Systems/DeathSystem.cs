using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(DamageSystem))]
	public partial struct DeathSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
			var job = new DeathJob { ecb = ecb, deltaTime = SystemAPI.Time.DeltaTime };
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(DeathStartedTag))]
		private partial struct DeathJob : IJobEntity
		{
			public EntityCommandBuffer.ParallelWriter ecb;
			public float deltaTime;
			private void Execute([ChunkIndexInQuery] int chunkIndex,
						  ref State state,
						  ref Velocity velocity,
						  ref ActiveAnim active,
						  ref DeathStartedTag dst,
						  EnabledRefRW<Alive> alive,
						  Entity entity)
			{
				// First-time setup when entering Dead
				if (state.Value != AgentState.Dead)
				{
					active.Loop = false;
					state.Value = AgentState.Dead;
					state.StateTimer = dst.Timer;
					velocity.Value = float3.zero;
					// trigger death animation
					active.State = AnimState.Death;
					active.Speed = 1f;
				}
				// countdown timer
				dst.Timer -= deltaTime;
				state.StateTimer = dst.Timer;
				if (dst.Timer <= 0f)
				{
					alive.ValueRW = false;
					ecb.RemoveComponent<DeathStartedTag>(chunkIndex, entity);
				}
			}
		}
	}
}


