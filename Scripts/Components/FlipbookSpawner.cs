using Unity.Entities;

public struct FlipbookSpawner : IComponentData
{
    public Entity NpcPrefab;
    public Entity MonsterPrefab;
    public int MonsterCount;
    public int NpcCount;
    public float SpawnRadius;
}
