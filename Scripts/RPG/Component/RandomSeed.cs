using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Components
{
	// Store RNG per entity for deterministic, thread-safe randomness
	public struct RandomState : IComponentData
	{
		public Random Rng;
	}
}


