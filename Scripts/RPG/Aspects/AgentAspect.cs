using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using RPG.Components;

namespace RPG.Aspects
{
	// Aspect bundles commonly used components for cache-efficient jobs
	public readonly partial struct AgentAspect : IAspect
	{
		public readonly Entity Self;

		private readonly RefRW<LocalTransform> _transform;
		private readonly RefRW<Velocity> _velocity;
		private readonly RefRW<Heading> _heading;
		private readonly RefRW<State> _state;
		private readonly RefRW<Health> _health;
		private readonly RefRW<MoveSpeed> _speed;
		private readonly RefRW<Team> _team;
		private readonly RefRW<Target> _target;
		private readonly RefRW<Perception> _perception;
		private readonly RefRW<Attack> _attack;
		private readonly RefRW<WanderParams> _wander;
		private readonly RefRW<RandomState> _random;

		public float3 Position
		{
			get => _transform.ValueRO.Position;
			set
			{
				var t = _transform.ValueRO;
				t.Position = value;
				_transform.ValueRW = t;
			}
		}

		public float3 Forward
		{
			get => _heading.ValueRO.Value;
			set => _heading.ValueRW.Value = math.normalizesafe(value, new float3(0, 1, 0));
		}

		public void SetVelocity(float3 v) => _velocity.ValueRW.Value = v;
		public float3 GetVelocity() => _velocity.ValueRO.Value;

		public bool IsAlive => _health.ValueRO.Value > 0f && _state.ValueRO.Value != AgentState.Dead;

		public void MarkDead()
		{
			var s = _state.ValueRO; s.Value = AgentState.Dead; s.StateTimer = 0; _state.ValueRW = s;
			_velocity.ValueRW.Value = float3.zero;
		}

		public void SetState(AgentState newState, float timer)
		{
			var s = _state.ValueRO; s.Value = newState; s.StateTimer = timer; _state.ValueRW = s;
		}

		public void TickStateTimer(float dt)
		{
			var s = _state.ValueRO; s.StateTimer -= dt; _state.ValueRW = s;
		}

		public bool StateTimerElapsed => _state.ValueRO.StateTimer <= 0f;

		public void SetTarget(Entity e, float3 pos, float distanceSq)
		{
			var t = _target.ValueRO;
			t.Entity = e; t.LastKnownPosition = pos; t.DistanceSq = distanceSq;
			_target.ValueRW = t;
		}

		public void ClearTarget()
		{
			var t = _target.ValueRO; t.Entity = Entity.Null; t.DistanceSq = float.MaxValue; _target.ValueRW = t;
		}

		public byte TeamId => _team.ValueRO.Value;
		public float MoveSpeed => _speed.ValueRO.Value;
		public Perception Perception => _perception.ValueRO;
		public Attack Attack => _attack.ValueRO;
		public WanderParams Wander => _wander.ValueRO;

		public RandomState Random
		{
			get => _random.ValueRO;
			set => _random.ValueRW = value;
		}

		public AgentState CurrentState => _state.ValueRO.Value;

		public void SetWander(WanderParams value) => _wander.ValueRW = value;

		public void SetAttack(Attack value) => _attack.ValueRW = value;

		public Target CurrentTarget => _target.ValueRO;
	}
}


