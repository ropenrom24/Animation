using Unity.Entities;

namespace RPG.Components
{
	// Empty tags to satisfy the project scenario requirements
	public struct NPCTag : IComponentData {}
	public struct MonsterTag : IComponentData {}

	// Marks an entity that just transitioned to Dead and should play death anim
	public struct DeathStartedTag : IComponentData { public float Timer; }

	// Team component for fast branch-free comparisons in jobs
	public struct Team : IComponentData
	{
		public byte Value; // 0 = NPC, 1 = Monster
	}

	// Enableable flag to include/exclude entities from most systems without structural changes
	public struct Alive : IComponentData, IEnableableComponent { }
}


