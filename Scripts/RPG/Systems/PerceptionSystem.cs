using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Systems
{
	// Uses SpatialHashSystem buffers for O(1) neighborhood lookup
	[BurstCompile]
	[UpdateInGroup(typeof(SimulationSystemGroup))]
	[UpdateAfter(typeof(SpatialHashSystem))]
	public partial struct PerceptionSystem : ISystem
	{
		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			var singletonQuery = SystemAPI.QueryBuilder().WithAll<SpatialHashSystem.HashSingleton>().Build();
			if (singletonQuery.IsEmpty) return;
			var singleton = singletonQuery.GetSingletonEntity();
			var settings = state.EntityManager.GetComponentData<SpatialHashSystem.Settings>(singleton);

			byte adaptiveEnabled = 0;
			var cfgQuery = SystemAPI.QueryBuilder().WithAll<SimulationConfig>().Build();
			if (!cfgQuery.IsEmpty)
			{
				adaptiveEnabled = cfgQuery.GetSingleton<SimulationConfig>().AdaptivePerceptionEnabled;
			}

			var job = new PerceptionJob
			{
				entries = state.EntityManager.GetBuffer<SpatialHashSystem.CellEntry>(singleton).AsNativeArray(),
				denseStarts = state.EntityManager.GetBuffer<SpatialHashSystem.CellStart>(singleton).AsNativeArray().Reinterpret<int>(),
				denseCounts = state.EntityManager.GetBuffer<SpatialHashSystem.CellCount>(singleton).AsNativeArray().Reinterpret<int>(),
				gridHalfExtent = settings.GridHalfExtent,
				cellSize = settings.CellSize,
				deltaTime = SystemAPI.Time.DeltaTime,
				adaptivePerceptionEnabled = adaptiveEnabled
			};
			job.ScheduleParallel();
		}

		[BurstCompile]
		[WithAll(typeof(Alive))]
		private partial struct PerceptionJob : IJobEntity
		{
			[ReadOnly] public NativeArray<SpatialHashSystem.CellEntry> entries;
			[ReadOnly] public NativeArray<int> denseStarts; // width*height
			[ReadOnly] public NativeArray<int> denseCounts; // width*height
			public int2 gridHalfExtent;
			public float cellSize;
			public float deltaTime;
			public byte adaptivePerceptionEnabled;

			private void Execute(ref State state,
						  ref Target target,
						  ref Heading heading,
						  ref Velocity velocity,
						  in Perception perception,
						  in LocalTransform transform,
						  in Team team,
						  in WanderParams wander,
						  ref PerceptionControl control,
						  ref RandomState random)
			{
				if (state.Value == AgentState.Dead) return;
				if (state.Value == AgentState.Attack) return;

				// Staggered updates when not in Chase
				if (state.Value != AgentState.Chase)
				{
					control.Timer -= deltaTime;
					if (control.Timer > 0f) return;
				}

				var pos = transform.Position;
				var cell = new int2((int)math.floor(pos.x / cellSize), (int)math.floor(pos.y / cellSize));
				int minX = -gridHalfExtent.x;
				int minY = -gridHalfExtent.y;
				int width = gridHalfExtent.x * 2 + 1;
				if (denseStarts.Length == 0 || denseCounts.Length == 0) return;

				Entity closest = Entity.Null;
				float closestDistSq = perception.SenseRadiusSq;
				float3 closestPos = float3.zero;
				bool foundImmediate = false;

				int remaining = control.MaxAgentChecks > 0 ? control.MaxAgentChecks : int.MaxValue;
				bool exhausted = false;
				int ring = (int)math.ceil(perception.SenseRadius / cellSize);
				ring = math.max(1, ring);

				int totalInRing = 0;
				if (adaptivePerceptionEnabled != 0)
				{
					for (int dy = -ring; dy <= ring; dy++)
					for (int dx = -ring; dx <= ring; dx++)
					{
						var c0 = new int2(cell.x + dx, cell.y + dy);
						int cx0 = math.clamp(c0.x, minX, gridHalfExtent.x);
						int cy0 = math.clamp(c0.y, minY, gridHalfExtent.y);
						int idx0 = (cx0 - minX) + (cy0 - minY) * width;
						totalInRing += denseCounts[idx0];
					}
				}

				for (int dy = -ring; dy <= ring && !exhausted; dy++)
				{
					for (int dx = -ring; dx <= ring && !exhausted; dx++)
					{
						var c = new int2(cell.x + dx, cell.y + dy);
						int cx = math.clamp(c.x, minX, gridHalfExtent.x);
						int cy = math.clamp(c.y, minY, gridHalfExtent.y);
						int idx = (cx - minX) + (cy - minY) * width;
						int start = denseStarts[idx];
						int count = denseCounts[idx];
						if (count <= 0) continue;

						int limit = control.MaxPerCellChecks > 0 ? math.min(count, control.MaxPerCellChecks) : count;
						if (adaptivePerceptionEnabled != 0 && totalInRing > 0)
						{
							float sampleFraction = math.saturate((float)remaining / (float)totalInRing);
							sampleFraction = math.clamp(sampleFraction, 0.05f, 1f);
							int adaptiveLimit = (int)math.ceil(count * sampleFraction);
							limit = math.min(limit, adaptiveLimit);
						}

						int offset = 0;
						if (count > limit)
						{
							var rng = random.Rng;
							offset = rng.NextInt(0, count);
							random.Rng = rng;
						}
						for (int n = 0; n < limit; n++)
						{
							int i = start + ((offset + n) % count);
							var entry = entries[i];
							if (entry.TeamId == team.Value) continue;
							float3 dp = entry.Position - pos;
							float dsq = math.lengthsq(new float3(dp.x, dp.y, 0));
							if (dsq < closestDistSq)
							{
								closestDistSq = dsq;
								closest = entry.Entity;
								closestPos = entry.Position;
								if (dsq <= perception.AttackRadiusSq)
								{
									foundImmediate = true;
									break;
								}
							}
							remaining--;
							if (remaining <= 0) { exhausted = true; break; }
						}
						if (foundImmediate) break;
					}
					if (foundImmediate) break;
				}

				if (closest != Entity.Null)
				{
					if (closestDistSq <= perception.AttackRadiusSq)
					{
						state.Value = AgentState.Attack; state.StateTimer = 0;
						velocity.Value = float3.zero;
						target.Entity = closest; target.LastKnownPosition = closestPos; target.DistanceSq = closestDistSq;
					}
					else
					{
						state.Value = AgentState.Chase; state.StateTimer = 0;
						target.Entity = closest; target.LastKnownPosition = closestPos; target.DistanceSq = closestDistSq;
						heading.Value = math.normalizesafe(new float3(closestPos.x - pos.x, closestPos.y - pos.y, 0), heading.Value);
					}
				}
				else if (state.Value == AgentState.Chase)
				{
					state.Value = AgentState.Wander; state.StateTimer = wander.DirectionChangeInterval;
					target.Entity = Entity.Null; target.DistanceSq = float.MaxValue; target.LastKnownPosition = pos;
				}

				// Reset perception timer with jitter (only when not in Chase)
				if (state.Value != AgentState.Chase)
				{
					var rng2 = random.Rng;
					float jitter = rng2.NextFloat(-control.JitterFraction, control.JitterFraction);
					random.Rng = rng2;
					control.Timer = math.max(0.01f, control.CooldownSeconds * (1f + jitter));
				}
			}
		}
	}
}


