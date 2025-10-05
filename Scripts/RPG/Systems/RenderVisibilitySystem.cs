using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(DeathSystem))]
	public partial struct RenderVisibilitySystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
			var q = state.GetEntityQuery(ComponentType.ReadOnly<RPG.Components.RenderToggle>());
			if (!q.IsEmpty)
			{
				var t = state.EntityManager.GetComponentData<RPG.Components.RenderToggle>(q.GetSingletonEntity());
				if (t.Enabled == 0) state.Enabled = false;
			}
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

			var job = new HideDeadJob { ecb = ecb };
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithNone(typeof(DisableRendering))]
		[WithNone(typeof(DeathStartedTag))]
		private partial struct HideDeadJob : IJobEntity
		{
			public EntityCommandBuffer.ParallelWriter ecb;

			private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in State state)
			{
				if (state.Value == AgentState.Dead)
				{
					ecb.AddComponent<DisableRendering>(chunkIndex, entity);
				}
			}
		}
	}
}


