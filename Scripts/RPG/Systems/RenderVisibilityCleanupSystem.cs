using Unity.Burst;
using Unity.Entities;
using Unity.Rendering;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(RenderVisibilitySystem))]
	public partial struct RenderVisibilityCleanupSystem : ISystem
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

			var job = new CleanupJob { ecb = ecb };
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(DisableRendering))]
		private partial struct CleanupJob : IJobEntity
		{
			public EntityCommandBuffer.ParallelWriter ecb;

			private void Execute([ChunkIndexInQuery] int chunkIndex, Entity entity, in State st)
			{
				if (st.Value != AgentState.Dead)
				{
					ecb.RemoveComponent<DisableRendering>(chunkIndex, entity);
				}
			}
		}
	}
}


