//using Unity.Entities;
//using Unity.Collections;
//using UnityEngine;
//using UnityEngine.LowLevelPhysics2D;
//using RPG.Components;
//using System;
//using System.Collections.Generic;
//using Unity.Physics;
//using static UnityEngine.Rendering.HableCurve;



//#if UNITY_EDITOR
//using UnityEditor;
//#endif

///// <summary>
///// Extracts custom physics shapes from each frame of each animation state.
///// Automatically pulls sprites from FlipbookAnimAuthoring's named arrays.
///// Based on SceneSpriteShape from PhysicsExamples2D.
///// </summary>
//public class AnimationPhysicsAuthoring : MonoBehaviour
//{
//    [Header("Physics Shapes Per Animation Frame")]
//    [Tooltip("Sprites auto-populated from FlipbookAnimAuthoring")]
//    public AnimStateFrames[] AnimationStates =
//    {
//        new AnimStateFrames { State = AnimState.Idle },
//        new AnimStateFrames { State = AnimState.Walk },
//        new AnimStateFrames { State = AnimState.Attack },
//        new AnimStateFrames { State = AnimState.Death },
//    };

//    [Tooltip("Use triggers for overlap detection")]
//    public bool IsTrigger = true;

//    [Tooltip("Close shape loop automatically")]
//    public bool IsLoop = true;

//    [Serializable]
//    public struct AnimStateFrames
//    {
//        public AnimState State;
//        [Tooltip("Sprites for each frame - auto-populated from FlipbookAnimAuthoring")]
//        public Sprite[] Frames;
//    }

//#if UNITY_EDITOR
//    [ContextMenu("Populate From FlipbookAnimAuthoring Sprite Arrays")]
//    public void PopulateFromFlipbookAnimAuthoring()
//    {
//        var animSource = GetComponent<FlipbookAnimAuthoring>();
//        if (animSource == null)
//        {
//            Debug.LogError("No FlipbookAnimAuthoring found on this GameObject!");
//            return;
//        }

//        var statesList = new List<AnimStateFrames>();
//        statesList.Add(new AnimStateFrames { State = AnimState.Idle, Frames = animSource.IdleSprites });
//        statesList.Add(new AnimStateFrames { State = AnimState.Walk, Frames = animSource.WalkSprites });
//        statesList.Add(new AnimStateFrames { State = AnimState.Attack, Frames = animSource.AttackSprites });
//        statesList.Add(new AnimStateFrames { State = AnimState.Death, Frames = animSource.DeathSprites });

//        AnimationStates = statesList.ToArray();

//        Debug.Log("<color=green>[AnimationPhysicsAuthoring]</color> Populated sprite arrays from FlipbookAnimAuthoring.", this);
//        EditorUtility.SetDirty(this);
//    }
//#endif

//    class Baker : Baker<AnimationPhysicsAuthoring>
//    {
//        public override void Bake(AnimationPhysicsAuthoring authoring)
//        {
//            var entity = GetEntity(TransformUsageFlags.Dynamic);

//            Debug.Log($"═══ BAKER START: {authoring.gameObject.name} ═══");

//            AddComponent(entity, new PhysicsShapeConfig { IsTrigger = authoring.IsTrigger });
//            AddComponent<PhysicsEnabled>(entity);
//            AddBuffer<AnimFrame>(entity);
//            SetComponentEnabled<PhysicsEnabled>(entity, true);
//            var collisionShapePerFrame = AddBuffer<PhysicsChainCollisionShape>(entity);
//            var animationSource = authoring.GetComponent<FlipbookAnimAuthoring>();
//            if (animationSource == null)
//            {
//                Debug.LogError("BAKER: No FlipbookAnimAuthoring!", authoring);
//                return;
//            }

//            if (authoring.AnimationStates == null || authoring.AnimationStates.Length == 0)
//            {
//                Debug.LogError("BAKER: AnimationStates is EMPTY! Run 'Populate From FlipbookAnimAuthoring Sprite Arrays' and SAVE the prefab!", authoring);
//                return;
//            }

//            var geometryBuffer = AddBuffer<AnimFramePhysicsGeometry>(entity);
//            int totalShapesExtracted = 0;

//            // We create a temporary PhysicsWorld to build the chain geometry per frame
//            var tempWorld = UnityEngine.LowLevelPhysics2D.PhysicsWorld.Create();

//            foreach (var animState in authoring.AnimationStates)
//            {
//                Debug.Log($"[BAKER] Processing {animState.State}: {animState.Frames?.Length ?? 0} frames");

//                if (animState.Frames == null || animState.Frames.Length == 0)
//                    continue;

//                for (int frameIndex = 0; frameIndex < animState.Frames.Length; frameIndex++)
//                {
//                    var sprite = animState.Frames[frameIndex];
//                    if (sprite == null)
//                        continue;

//                    int physicsShapeCount = sprite.GetPhysicsShapeCount();

//                    // ✅ Create body and chain geometry for preview and structure
//                    var body = tempWorld.CreateBody(new PhysicsBodyDefinition
//                    {
//                        position = Vector2.zero
//                    });


//                    if (sprite != null)
//                    {
//                        int shapeCount = sprite.GetPhysicsShapeCount();

//                        List<Vector2> path = new List<Vector2>();

//                        for (int i = 0; i < shapeCount; i++)
//                        {
//                            sprite.GetPhysicsShape(i, path);
//                        }

//                        // ✅ Store simplified version in ECS buffer
//                        if (path.Count >= 4)
//                        {
//                            using (var builder = new BlobBuilder(Allocator.Temp))
//                            {
//                                ref var root = ref builder.ConstructRoot<AnimFramePhysicsBlob>();
//                                var array = builder.Allocate(ref root.Points, path.Count);

//                                for (int i = 0; i < path.Count; i++)
//                                    array[i] = path[i];

//                                var blobRef = builder.CreateBlobAssetReference<AnimFramePhysicsBlob>(Allocator.Persistent);
//                                var chain = new ChainGeometry(path.ToArray());

//                                geometryBuffer.Add(new AnimFramePhysicsGeometry
//                                {
//                                    State = animState.State,
//                                    FrameIndex = frameIndex,
//                                    Geometry = chain,
//                                    Blob = blobRef
//                                });
//                                AddBlobAsset(ref blobRef, out _);
//                            }
//                        }
//                        totalShapesExtracted++;
//                        Debug.Log($"[BAKER] ✓ Added chain! State={animState.State}, Frame={frameIndex}, Vertices={path.Count}");
//                    }
//                }
//            }

//            tempWorld.Destroy();

//            Debug.Log($"═══ BAKER END: Extracted {totalShapesExtracted} shapes, Buffer.Length={geometryBuffer.Length} ═══");

//            if (geometryBuffer.Length == 0)
//                Debug.LogError("[BAKER] Buffer is empty after extraction! Check logs above for geometry validation failures.", authoring);
//        }
//    }
//}
