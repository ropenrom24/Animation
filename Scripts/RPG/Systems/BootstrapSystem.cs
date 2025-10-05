using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Systems
{
	[BurstCompile]
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	public partial struct BootstrapSystem : ISystem
	{
		[BurstCompile]
		public void OnCreate(ref SystemState state)
		{
			state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			// Run once
			state.Enabled = false;

			var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
				.CreateCommandBuffer(state.WorldUnmanaged);

			// Create a global toggle to disable rendering if needed
			var toggle = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(toggle, new RPG.Components.RenderToggle { Enabled = 0 });
			state.EntityManager.SetName(toggle, "RenderToggle");

			// Create SimulationConfig singleton with defaults
			var cfg = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(cfg, new SimulationConfig
			{
				AdaptivePerceptionEnabled = 0,
				SpatialHashCellSize = 2.5f,
				GridHalfExtent = new int2(24, 24)
			});
			state.EntityManager.SetName(cfg, "SimulationConfig");

			// Create TeamColors singleton
			var colors = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(colors, new TeamColors
			{
				NpcRgba = new float4(0.2f, 0.8f, 1f, 1f),
				MonsterRgba = new float4(1f, 0.3f, 0.2f, 1f)
			});
			state.EntityManager.SetName(colors, "TeamColors");

			// Entity population is handled by FlipbookSpawnerSystem using the prefab prototype approach.
		}
	}
}


