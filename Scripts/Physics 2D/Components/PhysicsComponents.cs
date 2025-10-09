//using Unity.Entities;
//using UnityEngine.LowLevelPhysics2D;
//using Unity.Collections;

//namespace RPG.Components
//{
//    public struct PhysicsChainCollisionShape: IBufferElementData
//    {
//        public PhysicsBody Body;
//        public PhysicsChain PhysicsChain;
//        public bool enabled;
//        public AnimState State;
//        public int frameindex;
//    }

//    // Store physics handles per entity
//    public struct PhysicsBodyRef : IComponentData
//    {
//        public PhysicsBody Body;
//        public int OwnerKey;
//    }

//    [ChunkSerializable]
//    public struct PhysicsShapeRef : IComponentData
//    {
//        public PhysicsChain Shape;
//        public PhysicsShape physicsShape;
//        public int OwnerKey;
//    }

//    public struct PhysicsShapeRefV2 : IBufferElementData
//    {
//        public ChainGeometry Shape;

//        public int OwnerKey;
//    }

//    // Define collision geometry per entity type
//    public struct CollisionGeometry : IComponentData
//    {
//        public float Radius; // For circle colliders
//        // Expand later: public PolygonGeometry CustomShape;
//    }

//    public struct SpritePhysicsGeometry : IBufferElementData
//    {
//        public PolygonGeometry Geometry;
//    }

//    // Configuration for physics shape creation
//    public struct PhysicsShapeConfig : IComponentData
//    {
//        public bool IsTrigger;
//        public bool UseDelaunay; // Triangulate complex shapes
//    }

//    // Store pre-computed polygon geometries from sprite physics shapes
//    public struct AnimStatePhysicsGeometry : IBufferElementData
//    {
//        public AnimState State;
//        public PolygonGeometry Geometry;
//    }

//    public struct ActivePhysicsState : IComponentData
//    {
//        public AnimState CurrentState;
//    }
//    // Enable/disable physics per entity
//    public struct PhysicsEnabled : IComponentData, IEnableableComponent { }

//    /// <summary>
//    /// Stores physics geometry for each frame of each animation state.
//    /// Organized as: State → Frame Index → Geometry
//    /// </summary>
//    /// 
//    [ChunkSerializable]
//    public struct AnimFramePhysicsGeometry : IBufferElementData
//    {
//        public AnimState State;
//        public int FrameIndex;           // Which frame in this animation
//        public ChainGeometry Geometry; // Physics shape for this frame
//        public BlobAssetReference<AnimFramePhysicsBlob> Blob;
//    }

//    public struct AnimFrame : IBufferElementData
//    {
//        public PhysicsShape physicsShape;
//        public int OwnerKey;
//    }

//    public struct AnimFramePhysicsBlob
//    {
//        public BlobArray<UnityEngine.Vector2> Points;
//    }

//    /// <summary>
//    /// Tracks current animation state and frame for physics shape updates.
//    /// </summary>
//    public struct ActivePhysicsFrame : IComponentData
//    {
//        public AnimState CurrentState;
//        public int CurrentFrame; // Cached to detect frame changes
//    }
//    public struct CurrentAnimFrame : IComponentData
//    {
//        public int FrameIndex; // Current frame being displayed (0, 1, 2, ...)
//    }
//}