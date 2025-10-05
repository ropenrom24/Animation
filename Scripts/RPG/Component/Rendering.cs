using Unity.Entities;

namespace RPG.Components
{
	public struct RenderToggle : IComponentData
	{
		public byte Enabled; // 1 = render enabled, 0 = disabled
	}
}


