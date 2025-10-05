using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(SpatialHashSystem))]
	[UpdateAfter(typeof(PerceptionSystem))]
	public partial struct WanderSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var job = new WanderJob { deltaTime = SystemAPI.Time.DeltaTime };
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(Alive))]
		private partial struct WanderJob : IJobEntity
		{
			public float deltaTime;

			private void Execute(ref State state,
						  ref WanderParams wander,
						  ref Velocity velocity,
						  ref Heading heading,
						  ref RandomState random,
						  in MoveSpeed moveSpeed,
						  ref ActiveAnim active)
			{
				if (state.Value == AgentState.Dead) return;

				switch (state.Value)
				{
					case AgentState.Idle:
					{
						state.StateTimer -= deltaTime;
						if (state.StateTimer <= 0f)
						{
							state.Value = AgentState.Wander; state.StateTimer = wander.DirectionChangeInterval;
						}
						velocity.Value = float3.zero;
						break;
					}
					case AgentState.Wander:
					{
						wander.Timer -= deltaTime;
						if (wander.Timer <= 0f)
						{
							var rng = random.Rng;
							float2 dir = rng.NextFloat2Direction();
							heading.Value = new float3(dir.x, dir.y, 0);
							random.Rng = rng;
							wander.Timer = wander.DirectionChangeInterval;
						}

						velocity.Value = heading.Value * moveSpeed.Value;

						{
							var rng2 = random.Rng;
							bool goIdle = rng2.NextFloat(0f, 1f) < 0.02f * deltaTime;
							random.Rng = rng2;
							if (goIdle)
							{
								var rng3 = random.Rng;
								float idleTime = rng3.NextFloat(wander.MinIdleSeconds, wander.MaxIdleSeconds);
								random.Rng = rng3;
								state.Value = AgentState.Idle; state.StateTimer = idleTime;
								velocity.Value = float3.zero;
							}
						}
						break;
					}
				}
			}
		}
	}
}
