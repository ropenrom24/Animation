//using Unity.Burst;
//using Unity.Entities;
//using UnityEngine.LowLevelPhysics2D;
//using RPG.Components;
//using Unity.Collections;
//using UnityEngine;
//using System.Linq;
//using Unity.Transforms;

//namespace RPG.Systems
//{
//    /// <summary>
//    /// Updates physics shape when animation frame changes.
//    /// Synchronizes with shader-based animation frame calculation.
//    /// </summary>
//    [UpdateInGroup(typeof(SimulationSystemGroup))]
//    [UpdateAfter(typeof(FlipbookAnimSystem))] // Run after frame is computed
//    [UpdateBefore(typeof(PhysicsSyncSystem))]
//    public partial class PhysicsFrameUpdateSystem : SystemBase
//    {
//        protected override void OnUpdate()
//        {
//            var world = PhysicsWorld.defaultWorld;
//            Entities
//                .WithAll<PhysicsEnabled, Alive>()
//                .ForEach((ref PhysicsShapeRef shapeRef,
//                         ref ActivePhysicsFrame activePhysicsFrame,
//                         ref PhysicsBodyRef bodyRef,
//                         in ActiveAnim activeAnim,
//                         in CurrentAnimFrame currentFrame,
//                         in DynamicBuffer<AnimFramePhysicsGeometry> geometries,
//                         in LocalTransform transform,
//                         in Team team) =>
//                {
//                    // Check if state or frame changed
//                    bool stateChanged = activePhysicsFrame.CurrentState != activeAnim.State;
//                    bool frameChanged = activePhysicsFrame.CurrentFrame != currentFrame.FrameIndex;

//                    if (!stateChanged && !frameChanged)
//                    {
//                        return;
//                    }
//                    activePhysicsFrame.CurrentState = activeAnim.State;
//                    activePhysicsFrame.CurrentFrame = currentFrame.FrameIndex;
//                    //Debug.Log("shapeRef.Shape.isValid : " + shapeRef.Shape.isValid);
//                    //if (!shapeRef.Shape.isValid)
//                    //    return;

//                    // Find geometry for current state + frame
//                    //ChainGeometry newGeometry = default;
//                    for (int i = 0; i < geometries.Length; i++)
//                    {
//                        if (geometries[i].State == activeAnim.State &&
//                            geometries[i].FrameIndex == currentFrame.FrameIndex)
//                        {
//                            //newGeometry = geometries[i].Geometry;
//                            var segments = geometries[i].Blob.Value.Points.ToArray();
//                            for (int j = 0; j < segments.Length - 1; j++)
//                            {
//                                PhysicsShape.CreateShape(bodyRef.Body, new SegmentGeometry { point1 = segments[j], point2 = segments[j + 1] });
//                            }

//                            //for (int j = 0; j < segments.Length - 1; j++)
//                            //{
//                            //    shapeRef.Shape.GetSegments(Unity.Collections.Allocator.TempJob).ToList().Add(PhysicsShape.CreateShape(bodyRef.Body, new SegmentGeometry { point1 = segments[j], point2 = segments[j + 1] }));
//                            //}

//                            break;
//                        }
//                    }

//                    //NativeArray<PhysicsShape> shapes = new NativeArray<PhysicsShape>();
//                    //for (int j = 0; j < segments.Length - 1; j++)
//                    //{
//                    //    //if (newGeometry.isValid)
//                    //    //{
//                    //    //    if (shapeRef.Shape.isValid)
//                    //    //    {
//                    //    //        shapeRef.Shape.Destroy(shapeRef.OwnerKey);
//                    //    //    }
//                    //    //    var chainDef = new PhysicsChainDefinition
//                    //    //    {
//                    //    //        surfaceMaterial = new PhysicsShape.SurfaceMaterial
//                    //    //        {
//                    //    //            customColor = team.Value == 0
//                    //    //                    ? UnityEngine.Color.softBlue
//                    //    //                    : UnityEngine.Color.softGreen
//                    //    //        },
//                    //    //        isLoop = true
//                    //    //    };
//                    //    //    if (newGeometry.isValid)
//                    //    //    {
//                    //    //        shapeRef.Shape = bodyRef.Body.CreateChain(newGeometry, chainDef);
//                    //    //        shapeRef.OwnerKey = shapeRef.Shape.SetOwner(null);
//                    //    //    }

//                    //    //}
//                    //    //PhysicsShape.CreateShape(bodyRef.Body, new SegmentGeometry { point1 = segments[j], point2 = segments[j + 1] });
//                    //    //bodyRef.Body.CreateShape(new SegmentGeometry { point1 = segments[j], point2 = segments[j + 1] });

//                    //    //shapeRef.Shape.GetSegments(Unity.Collections.Allocator.TempJob).ToList().Add(PhysicsShape.CreateShape(bodyRef.Body, new SegmentGeometry { point1 = segments[j], point2 = segments[j + 1] }));
//                    //}



//                    // Fallback: use frame 0 of current state
//                    //if (!newGeometry.isValid)
//                    //{
//                    //    for (int i = 0; i < geometries.Length; i++)
//                    //    {
//                    //        if (geometries[i].State == activeAnim.State &&
//                    //            geometries[i].FrameIndex == 0)
//                    //        {
//                    //            newGeometry = geometries[i].Geometry;
//                    //            break;
//                    //        }
//                    //    }
//                    //}

//                    //if (!newGeometry.isValid)
//                    //    return; 








//                    //foreach (var item in shapeRef.Shape.GetSegments(Unity.Collections.Allocator.TempJob).ToArray<PhysicsShape>())
//                    //{
//                    //    item.segmentGeometry = new SegmentGeometry { point1 = new Vector2(-0.145f, -0.205f), point2 = new Vector2(-0.145f, -0.205f) };
//                    //}

//                    //var bodyDef = new PhysicsBodyDefinition
//                    //{
//                    //    bodyType = RigidbodyType2D.Kinematic,
//                    //    position = new UnityEngine.Vector2(transform.Position.x, transform.Position.y),
//                    //    rotation = new PhysicsRotate(0f)
//                    //};
//                    //shapeRef.Shape = shapeRef.Shape.body.CreateChain(newGeometry, chainDef);
//                    //newGeometry.
//                    //// Create a new chain shape from the new geometry
//                    //var chainDef = new PhysicsChainDefinition
//                    //{
//                    //    surfaceMaterial = new PhysicsShape.SurfaceMaterial
//                    //    {
//                    //        customColor = UnityEngine.Color.yellow // or team-based if you prefer
//                    //    },
//                    //    isLoop = true
//                    //};

//                    //var newShape = bodyRef.Body.CreateChain(newGeometry, chainDef);
//                    //bodyRef.Body.GetShapes().
//                    // Update cache

//                }).WithoutBurst().Run();
//        }
//    }
//}