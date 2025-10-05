using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FlipbookSpawnerAuthoring : MonoBehaviour
{
    public GameObject NpcPrefab;
    public GameObject MonsterPrefab;
    [Tooltip("Number of NPC-tagged agents to spawn")] public int NpcCount = 100;
    [Tooltip("Number of Monster-tagged agents to spawn")] public int MonsterCount = 100;
    [Tooltip("Spawn radius (XY plane) for random placement")] public float SpawnRadius = 20f;

    class Baker : Baker<FlipbookSpawnerAuthoring>
    {
        public override void Bake(FlipbookSpawnerAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var npcPrefabEntity = authoring.NpcPrefab ? GetEntity(authoring.NpcPrefab, TransformUsageFlags.Renderable) : Entity.Null;
            var monsterPrefabEntity = authoring.MonsterPrefab ? GetEntity(authoring.MonsterPrefab, TransformUsageFlags.Renderable) : Entity.Null;

            AddComponent(e, new FlipbookSpawner
            {
                NpcPrefab     = npcPrefabEntity,
                MonsterPrefab = monsterPrefabEntity,
                MonsterCount  = math.max(0, authoring.MonsterCount),
                NpcCount      = math.max(0, authoring.NpcCount),
                SpawnRadius   = math.max(0.01f, authoring.SpawnRadius)
            });
        }
    }
}
