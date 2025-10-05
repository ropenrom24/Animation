using Unity.Entities;

namespace RPG.Components
{
	public struct Perception : IComponentData
	{
		public float SenseRadius;      // 5 units
		public float SenseRadiusSq;
		public float AttackRadius;     // 1 unit
		public float AttackRadiusSq;
	}

	public struct Attack : IComponentData
	{
		public float DamagePerHit;
		public float AttackSpeed;      // attacks per second
		public float CooldownTimer;
	}

	public struct Damage : IBufferElementData
	{
		public float Value;
	}

	public struct DamageBufferTag : IComponentData {}

	// Controls perception update cadence and scan bounds
	public struct PerceptionControl : IComponentData
	{
		public float CooldownSeconds;     // base seconds between scans when not in Chase
		public float JitterFraction;      // +/- jitter fraction applied to cooldown
		public int MaxPerCellChecks;      // cap candidates checked per neighbor cell
		public int MaxAgentChecks;        // cap total candidates checked per agent per update
		public float Timer;               // internal countdown
	}
}


