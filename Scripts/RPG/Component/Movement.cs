using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Components
{
	public struct Velocity : IComponentData
	{
		public float3 Value;
	}

	public struct Heading : IComponentData
	{
		public float3 Value; // normalized direction in XY plane
	}

	public struct WanderParams : IComponentData
	{
		public float MinIdleSeconds;
		public float MaxIdleSeconds;
		public float DirectionChangeInterval;
		public float Timer;
	}
}


