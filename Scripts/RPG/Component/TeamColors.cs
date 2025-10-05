using Unity.Entities;
using Unity.Mathematics;

namespace RPG.Components
{
	public struct TeamColors : IComponentData
	{
		public float4 NpcRgba;     // default cyan
		public float4 MonsterRgba; // default red
	}
}


