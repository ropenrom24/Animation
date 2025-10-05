using Unity.Entities;

namespace RPG.Components
{
    public struct RecentlyDamaged : IComponentData
    {
        public float Timer;
    }

    public struct HpBarVisible : IComponentData, IEnableableComponent { }
}