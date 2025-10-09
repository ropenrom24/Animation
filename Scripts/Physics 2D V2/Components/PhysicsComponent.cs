using Unity.Entities;
using UnityEngine.LowLevelPhysics2D;

namespace RPG.Components
{
    // Store physics handles per entity
    public struct PhysicsBodyRef : IComponentData
    {
        public PhysicsBody Body;
        public int OwnerKey;
    }

    public struct PhysicsShapeRef : IComponentData
    {
        public PhysicsShape Shape;
        public int OwnerKey;
    }

    // Define collision geometry per entity type
    public struct CollisionGeometry : IComponentData
    {
        public float Radius; // For circle colliders
        // Expand later: public PolygonGeometry CustomShape;
    }

    // Enable/disable physics per entity
    public struct PhysicsEnabled : IComponentData, IEnableableComponent { }
}