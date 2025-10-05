using Unity.Burst;
using Unity.Entities;
using RPG.Components;

namespace RPG.Systems
{
	// Aggregates counts each frame using EntityQuery.CalculateEntityCount (respects enableable flags).
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	public partial struct CounterSystem : ISystem
	{
		public struct RuntimeCounters : IComponentData
		{
			public int TotalAlive;
			public int TotalAliveNpc;
			public int TotalAliveMonster;
			public int TotalVisibleAlive;
			public int TotalVisibleAliveNpc;
			public int TotalVisibleAliveMonster;
		}

		private Entity _singleton;
		private EntityQuery _npcAliveQ;
		private EntityQuery _monAliveQ;
		private EntityQuery _npcVisibleAliveQ;
		private EntityQuery _monVisibleAliveQ;

		public void OnCreate(ref SystemState state)
		{
			_singleton = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponentData(_singleton, new RuntimeCounters());
			state.EntityManager.SetName(_singleton, "RuntimeCounters");

			_npcAliveQ = SystemAPI.QueryBuilder()
				.WithAll<Alive, NPCTag>()
				.Build();

			_monAliveQ = SystemAPI.QueryBuilder()
				.WithAll<Alive, MonsterTag>()
				.Build();

			_npcVisibleAliveQ = SystemAPI.QueryBuilder()
				.WithAll<Alive, NPCTag, VisibleTag>()
				.Build();

			_monVisibleAliveQ = SystemAPI.QueryBuilder()
				.WithAll<Alive, MonsterTag, VisibleTag>()
				.Build();
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			int npcAlive  = _npcAliveQ.CalculateEntityCount();
			int monAlive  = _monAliveQ.CalculateEntityCount();
			int npcVis    = _npcVisibleAliveQ.CalculateEntityCount();
			int monVis    = _monVisibleAliveQ.CalculateEntityCount();

			var counters = new RuntimeCounters
			{
				TotalAlive               = npcAlive + monAlive,
				TotalAliveNpc            = npcAlive,
				TotalAliveMonster        = monAlive,
				TotalVisibleAlive        = npcVis + monVis,
				TotalVisibleAliveNpc     = npcVis,
				TotalVisibleAliveMonster = monVis
			};
			state.EntityManager.SetComponentData(_singleton, counters);
		}
	}
}
