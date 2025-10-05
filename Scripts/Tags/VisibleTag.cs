using Unity.Entities;

// Must implement BOTH to be enable/disable-able and usable in queries.
public struct VisibleTag : IComponentData, IEnableableComponent {}
