using Unity.Burst;
using Unity.Entities;
using RPG.Components;

namespace RPG.Systems
{
	// Sync lightweight animation state from gameplay state only when it changes.
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(DeathSystem))] // run after state transitions
	[UpdateAfter(typeof(global::FrustumCullingSystem))]
	public partial struct AnimSyncSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var job = new SyncAnimJob { defaultSpeed = 1f };
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(Alive))]
		[WithChangeFilter(typeof(State))]
		private partial struct SyncAnimJob : IJobEntity
		{
			public float defaultSpeed;

			private void Execute(EnabledRefRO<VisibleTag> visible,
						  in State state,
						  ref ActiveAnim active)
			{
				if (!visible.ValueRO) return; // only visible entities

				var mapped = AnimStateMapper.FromAgentState(state.Value);
				if (mapped == active.State) return;

				active.State = mapped;
				// Reset speed back to default whenever we are NOT attacking
				if (mapped != AnimState.Attack)
				{
					active.Speed = defaultSpeed;
				}
				// Death should not loop; others do
				active.Loop = mapped != AnimState.Death;
			}
		}
	}
}
