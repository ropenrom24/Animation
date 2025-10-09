using Unity.Entities;

namespace RPG.Components
{
    public struct RecentlyDamaged : IComponentData
    {
        public float Timer;
        public float TintTimer;
    }

    public struct HpBarVisible : IComponentData, IEnableableComponent { }
}