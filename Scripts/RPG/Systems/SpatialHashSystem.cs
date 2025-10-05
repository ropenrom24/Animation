using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Systems
{
	// Build a lightweight spatial hash of positions each frame for O(1) neighborhood queries.
	// Converted to XY plane.
	[BurstCompile]
	public partial struct SpatialHashSystem : ISystem
	{
		public struct GridCell
		{
			public int StartIndex;
			public int Count;
		}

		public struct Settings : IComponentData
		{
			public float CellSize; // should be >= sense radius (5) to bound neighbors (9 cells)
			public int2 GridHalfExtent; // number of cells from center in +/- x,y
		}

		public struct HashSingleton : IComponentData {}

		private Entity _singleton;

		// Scratch containers reused each frame to avoid main-thread allocations
		private NativeList<CellEntry> _entryScratch;
		private NativeList<CellIndex> _indexScratch;

		// Cached lookups (updated each frame)
		private BufferLookup<CellEntry> _entriesLookupA;
		private BufferLookup<CellIndex> _indexLookupA;
		private BufferLookup<CellStart> _startsLookup;
		private BufferLookup<CellCount> _countsLookup;

		public struct CellEntry : IBufferElementData
		{
			public int2 Cell;
			public float3 Position;
			public Entity Entity;
			public byte TeamId;
		}

		public struct CellIndex : IBufferElementData
		{
			public int2 Cell;
			public int Start;
			public int Count;
		}

		public struct CellStart : IBufferElementData
		{
			public int Value;
		}

		public struct CellCount : IBufferElementData
		{
			public int Value;
		}

		public struct HashState : IComponentData
		{
			public byte Active; // unused
		}

		public void OnCreate(ref SystemState state)
		{
			state.EntityManager.AddComponent<Settings>(state.SystemHandle);
			state.EntityManager.SetComponentData(state.SystemHandle, new Settings
			{
				CellSize = 2.5f,
				GridHalfExtent = new int2(24, 24)
			});

			_singleton = state.EntityManager.CreateEntity();
			state.EntityManager.AddComponent<HashSingleton>(_singleton);
			state.EntityManager.AddComponent<Settings>(_singleton);
			state.EntityManager.SetComponentData(_singleton, state.EntityManager.GetComponentData<Settings>(state.SystemHandle));
			state.EntityManager.AddBuffer<CellEntry>(_singleton);
			state.EntityManager.AddBuffer<CellIndex>(_singleton);
			state.EntityManager.AddBuffer<CellStart>(_singleton);
			state.EntityManager.AddBuffer<CellCount>(_singleton);

			_entryScratch = new NativeList<CellEntry>(Allocator.Persistent);
			_indexScratch = new NativeList<CellIndex>(Allocator.Persistent);

			_entriesLookupA = state.GetBufferLookup<CellEntry>(isReadOnly: false);
			_indexLookupA = state.GetBufferLookup<CellIndex>(isReadOnly: false);
			_startsLookup = state.GetBufferLookup<CellStart>(isReadOnly: false);
			_countsLookup = state.GetBufferLookup<CellCount>(isReadOnly: false);
		}

		[BurstCompile]
		public void OnUpdate(ref SystemState state)
		{
			_entriesLookupA.Update(ref state);
			_indexLookupA.Update(ref state);
			_startsLookup.Update(ref state);
			_countsLookup.Update(ref state);

			// Apply SimulationConfig if present
			var cfgQuery = SystemAPI.QueryBuilder().WithAll<SimulationConfig>().Build();
			if (!cfgQuery.IsEmpty)
			{
				var cfg = cfgQuery.GetSingleton<SimulationConfig>();
				var s = state.EntityManager.GetComponentData<Settings>(state.SystemHandle);
				if (cfg.SpatialHashCellSize > 0f) s.CellSize = cfg.SpatialHashCellSize;
				if (cfg.GridHalfExtent.x > 0 && cfg.GridHalfExtent.y > 0) s.GridHalfExtent = cfg.GridHalfExtent;
				state.EntityManager.SetComponentData(state.SystemHandle, s);
			}

			var settings = state.EntityManager.GetComponentData<Settings>(state.SystemHandle);
			state.EntityManager.SetComponentData(_singleton, settings);

			var aliveQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, Team>().Build();
			int aliveCount = aliveQuery.CalculateEntityCount();
			if (_entryScratch.Capacity < aliveCount)
				_entryScratch.Capacity = aliveCount;
			_entryScratch.Clear();

			var gatherJob = new GatherEntriesJob
			{
				cellSize = settings.CellSize,
				writer = _entryScratch.AsParallelWriter()
			};
			var gatherHandle = gatherJob.ScheduleParallel(state.Dependency);
			gatherHandle.Complete();
			var gathered = _entryScratch.AsArray();
			int entryCount = gathered.Length;

			int minX = -settings.GridHalfExtent.x;
			int minY = -settings.GridHalfExtent.y;
			int width = settings.GridHalfExtent.x * 2 + 1;
			int height = settings.GridHalfExtent.y * 2 + 1;
			int numCells = width * height;

			var counts = new NativeArray<int>(numCells, Allocator.TempJob, NativeArrayOptions.ClearMemory);
			var starts = new NativeArray<int>(numCells, Allocator.TempJob, NativeArrayOptions.ClearMemory);

			var countJob = new CountCellsJob
			{
				entries = gathered,
				counts = counts,
				numCells = numCells,
				minX = minX, minY = minY, width = width, height = height, cellSize = settings.CellSize
			};
			var countHandle = countJob.Schedule(default);

			var totalRef = new NativeReference<int>(Allocator.TempJob);
			var prefixJob = new PrefixSumJob { counts = counts, starts = starts, total = totalRef };
			var prefixHandle = prefixJob.Schedule(countHandle);

			_indexScratch.Clear();
			prefixHandle.Complete();
			int totalEntries = totalRef.Value;
			var outEntries = new NativeArray<CellEntry>(math.max(0, totalEntries), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			var cursors = new NativeArray<int>(numCells, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
			cursors.CopyFrom(starts);
			var initHandle = prefixHandle;

			var scatterJob = new ScatterEntriesJob
			{
				inEntries = gathered,
				outEntries = outEntries,
				cursors = cursors,
				starts = starts,
				minX = minX, minY = minY, width = width, height = height, cellSize = settings.CellSize
			};
			var scatterHandle = scatterJob.Schedule(initHandle);

			var buildIndexJob = new BuildIndicesFromCountsJob
			{
				counts = counts,
				starts = starts,
				indices = _indexScratch,
				minX = minX, minY = minY, width = width, height = height
			};
			var buildHandle = buildIndexJob.Schedule(scatterHandle);

			var copyJob = new CopyToBuffersJob
			{
				outEntries = outEntries,
				inIndices = _indexScratch.AsDeferredJobArray(),
				counts = counts,
				starts = starts,
				entriesLookupA = _entriesLookupA,
				indexLookupA = _indexLookupA,
				startsLookup = _startsLookup,
				countsLookup = _countsLookup,
				singleton = _singleton
			};
			var copyHandle = copyJob.Schedule(buildHandle);

			var disposeCounts = counts.Dispose(copyHandle);
			var disposeStarts = starts.Dispose(disposeCounts);
			var disposeCursors = cursors.Dispose(disposeStarts);
			var disposeTotal = totalRef.Dispose(disposeCursors);
			var disposeOut = outEntries.Dispose(disposeTotal);

			state.Dependency = disposeOut;
		}

		[BurstCompile]
		[WithAll(typeof(Alive))]
		private partial struct GatherEntriesJob : IJobEntity
		{
			public float cellSize;
			public NativeList<CellEntry>.ParallelWriter writer;

			private void Execute(Entity entity, in LocalTransform lt, in Team team)
			{
				var p = lt.Position;
				var cell = new int2((int)math.floor(p.x / cellSize), (int)math.floor(p.y / cellSize));
				writer.AddNoResize(new CellEntry
				{
					Cell = cell,
					Position = p,
					Entity = entity,
					TeamId = team.Value
				});
			}
		}

		[BurstCompile]
		private struct CountCellsJob : IJob
		{
			[ReadOnly] public NativeArray<CellEntry> entries;
			public NativeArray<int> counts;                      // numCells
			public int numCells;
			public int minX, minY, width, height;
			public float cellSize;
			public void Execute()
			{
				for (int i = 0; i < entries.Length; i++)
				{
					var p = entries[i].Position;
					int cx = (int)math.floor(p.x / cellSize);
					int cy = (int)math.floor(p.y / cellSize);
					cx = math.clamp(cx, minX, minX + width - 1);
					cy = math.clamp(cy, minY, minY + height - 1);
					int cellIdx = (cx - minX) + (cy - minY) * width;
					counts[cellIdx] += 1;
				}
			}
		}

		[BurstCompile]
		private struct PrefixSumJob : IJob
		{
			public NativeArray<int> counts;
			public NativeArray<int> starts;
			public NativeReference<int> total;
			public void Execute()
			{
				int sum = 0;
				for (int i = 0; i < counts.Length; i++)
				{
					starts[i] = sum;
					sum += counts[i];
				}
				total.Value = sum;
			}
		}

		[BurstCompile]
		private struct ReduceCountsJob : IJob
		{
			[ReadOnly] public NativeArray<int> countsPerThread; // numThreads * numCells
			public NativeArray<int> counts;                      // numCells
			public NativeArray<int> prefixPerThread;             // numThreads * numCells
			public int numCells;
			public int numThreads;
			public void Execute()
			{
				for (int cell = 0; cell < numCells; cell++)
				{
					int total = 0;
					for (int t = 0; t < numThreads; t++)
					{
						int idx = t * numCells + cell;
						prefixPerThread[idx] = total;
						int c = countsPerThread[idx];
						total += c;
					}
					counts[cell] = total;
				}
			}
		}

		[BurstCompile]
		private struct ScatterEntriesJob : IJob
		{
			[ReadOnly] public NativeArray<CellEntry> inEntries;
			public NativeArray<CellEntry> outEntries;
			public NativeArray<int> cursors;                     // size numCells
			[ReadOnly] public NativeArray<int> starts;           // numCells
			public int minX, minY, width, height;
			public float cellSize;
			public void Execute()
			{
				for (int i = 0; i < inEntries.Length; i++)
				{
					var e = inEntries[i];
					int cx = (int)math.floor(e.Position.x / cellSize);
					int cy = (int)math.floor(e.Position.y / cellSize);
					cx = math.clamp(cx, minX, minX + width - 1);
					cy = math.clamp(cy, minY, minY + height - 1);
					int cellIdx = (cx - minX) + (cy - minY) * width;
					int writeIndex = cursors[cellIdx]++;
					outEntries[writeIndex] = new CellEntry { Cell = new int2(cx, cy), Position = e.Position, Entity = e.Entity, TeamId = e.TeamId };
				}
			}
		}

		[BurstCompile]
		private struct BuildIndicesFromCountsJob : IJob
		{
			[ReadOnly] public NativeArray<int> counts;
			[ReadOnly] public NativeArray<int> starts;
			public NativeList<CellIndex> indices;
			public int minX, minY, width, height;
			public void Execute()
			{
				indices.Clear();
				for (int x = 0; x < width; x++)
				{
					for (int y = 0; y < height; y++)
					{
						int i = y * width + x;
						int count = counts[i];
						if (count == 0) continue;
						int start = starts[i];
						int cx = minX + x;
						int cy = minY + y;
						indices.Add(new CellIndex { Cell = new int2(cx, cy), Start = start, Count = count });
					}
				}
			}
		}

		public void OnDestroy(ref SystemState state)
		{
			if (_entryScratch.IsCreated) _entryScratch.Dispose();
			if (_indexScratch.IsCreated) _indexScratch.Dispose();
		}

		[BurstCompile]
		private struct CopyToBuffersJob : IJob
		{
			[ReadOnly] public NativeArray<CellEntry> outEntries;
			[ReadOnly] public NativeArray<CellIndex> inIndices;
			[ReadOnly] public NativeArray<int> counts;
			[ReadOnly] public NativeArray<int> starts;
			public BufferLookup<CellEntry> entriesLookupA;
			public BufferLookup<CellIndex> indexLookupA;
			public BufferLookup<CellStart> startsLookup;
			public BufferLookup<CellCount> countsLookup;
			public Entity singleton;

			public void Execute()
			{
				var eb = entriesLookupA[singleton];
				eb.Clear(); eb.ResizeUninitialized(outEntries.Length);
				var ebArr = eb.AsNativeArray();
				for (int i = 0; i < outEntries.Length; i++) ebArr[i] = outEntries[i];

				var ib = indexLookupA[singleton];
				ib.Clear(); ib.ResizeUninitialized(inIndices.Length);
				var ibArr = ib.AsNativeArray();
				for (int i = 0; i < inIndices.Length; i++) ibArr[i] = inIndices[i];

				var sb = startsLookup[singleton];
				var cb = countsLookup[singleton];
				sb.Clear(); cb.Clear();
				sb.ResizeUninitialized(starts.Length);
				cb.ResizeUninitialized(counts.Length);
				var sbArr = sb.AsNativeArray();
				var cbArr = cb.AsNativeArray();
				for (int i = 0; i < starts.Length; i++) sbArr[i] = new CellStart { Value = starts[i] };
				for (int i = 0; i < counts.Length; i++) cbArr[i] = new CellCount { Value = counts[i] };
			}
		}
	}
}


