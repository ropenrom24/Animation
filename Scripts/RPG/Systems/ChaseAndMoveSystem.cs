using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(PerceptionSystem))]
	public partial struct ChaseAndMoveSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var job = new ChaseAndMoveJob { deltaTime = SystemAPI.Time.DeltaTime };
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(Alive))]
		private partial struct ChaseAndMoveJob : IJobEntity
		{
			public float deltaTime;

			private void Execute(ref State state,
							  ref Velocity velocity,
							  ref Heading heading,
							  in MoveSpeed moveSpeed,
							  in Target target,
							  ref LocalTransform transform,
							  in Perception perception)
			{
				if (state.Value == AgentState.Dead) return;
				if (state.Value == AgentState.Attack)
				{
					velocity.Value = float3.zero;
					return;
				}
				if (state.Value == AgentState.Chase)
				{
					var pos = transform.Position;
					float3 toTarget = new float3(target.LastKnownPosition.x - pos.x, target.LastKnownPosition.y - pos.y, 0);
					float distSq = math.lengthsq(toTarget);
					if (distSq <= perception.AttackRadiusSq)
					{
						state.Value = AgentState.Attack; velocity.Value = float3.zero;
					}
					else
					{
						heading.Value = math.normalizesafe(toTarget, heading.Value);
						velocity.Value = heading.Value * moveSpeed.Value;
					}
				}

				var v = velocity.Value;
				if (!math.all(v == float3.zero))
				{
					transform.Position = transform.Position + v * deltaTime;
				}
			}
		}
	}
}


