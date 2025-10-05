using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Components
{
	public struct Health : IComponentData
	{
		public float Value;
		public float Max;
	}

	public struct MoveSpeed : IComponentData
	{
		public float Value;
	}

	// State machine flags kept small for cache friendliness
	public enum AgentState : byte
	{
		Idle = 0,
		Wander = 1,
		Chase = 2,
		Attack = 3,
		Dead = 4,
	}

	public struct State : IComponentData
	{
		public AgentState Value;
		public float StateTimer; // counts down for idle/attack cooldowns
	}

	public struct Target : IComponentData
	{
		public Entity Entity;
		public float3 LastKnownPosition;
		public float DistanceSq;
	}
}


