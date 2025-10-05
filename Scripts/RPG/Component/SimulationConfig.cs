using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Components
{
	// Global simulation knobs. Defaults match current behavior to avoid changes.
	public struct SimulationConfig : IComponentData
	{
		public byte AdaptivePerceptionEnabled; // 0 = off (default), 1 = on
		public float SpatialHashCellSize;      // default 2.5f
		public int2 GridHalfExtent;            // default (24,24)
	}
}


