//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine.LowLevelPhysics2D;
//using RPG.Components;
//using UnityEngine;

//namespace RPG.Systems
//{
//    /// <summary>
//    /// Creates physics bodies with sprite-based custom shapes.
//    /// Reads from AnimFramePhysicsGeometry buffer baked by AnimationPhysicsAuthoring.
//    /// </summary>
//    [UpdateInGroup(typeof(InitializationSystemGroup))]
//    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
//    public partial class PhysicsInitSystem : SystemBase
//    {
//        protected override void OnCreate()
//        {
//            RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
//        }

//        protected override void OnUpdate()
//        {
//            var world = PhysicsWorld.defaultWorld;
//            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
//                .CreateCommandBuffer(World.Unmanaged);

//            // Create physics for entities with sprite-based geometry
//            Entities
//                .WithNone<PhysicsBodyRef>()
//                .WithAll<PhysicsEnabled>()
//                .ForEach((Entity entity, int entityInQueryIndex,
//                         ref DynamicBuffer<AnimFramePhysicsGeometry> geometries,
//                         ref DynamicBuffer<PhysicsChainCollisionShape> chains,
//                         in LocalTransform transform,
//                         in PhysicsShapeConfig config,
//                         in ActiveAnim activeAnim,
//                         in Team team) =>
//                {
//                    if (geometries.Length == 0)
//                    {
//                        UnityEngine.Debug.LogWarning(
//                            $"Entity has PhysicsEnabled but no AnimFramePhysicsGeometry! " +
//                            $"Make sure AnimationPhysicsAuthoring is configured on the prefab."
//                        );
//                        return;
//                    }

//                    // Find geometry for initial state + frame 0
//                    for (int i = 0; i < geometries.Length; i++)
//                    {
//                        ChainGeometry geometry = geometries[i].Geometry;

//                        // Create body
//                        var bodyDef = new PhysicsBodyDefinition
//                        {
//                            bodyType = RigidbodyType2D.Kinematic,
//                            position = new UnityEngine.Vector2(transform.Position.x, transform.Position.y),
//                            rotation = new PhysicsRotate(0f)
//                        };

//                        var body = world.CreateBody(bodyDef);

//                        // Store entity reference in user data for collision callbacks
//                        body.userData = new PhysicsUserData
//                        {
//                            intValue = entity.Index,
//                            physicsMaskValue = (ulong)(1 << team.Value)
//                        };

//                        var bodyOwnerKey = body.SetOwner(null);

//                        // Create shape
//                        var shapeDef = new PhysicsShapeDefinition
//                        {
//                            isTrigger = config.IsTrigger,
//                            triggerEvents = true,
//                            surfaceMaterial = new PhysicsShape.SurfaceMaterial
//                            {
//                                customColor = team.Value == 0
//                                    ? UnityEngine.Color.cyan
//                                    : UnityEngine.Color.magenta
//                            }
//                        };

//                        var chainDef = new PhysicsChainDefinition
//                        {
//                            surfaceMaterial = new PhysicsShape.SurfaceMaterial
//                            {
//                                customColor = team.Value == 0
//                                    ? UnityEngine.Color.cyan
//                                    : UnityEngine.Color.magenta
//                            },
//                            isLoop = true
//                        };

//                        var shape = body.CreateChain(geometry, chainDef);

//                        if (!shape.isValid)
//                        {
//                            body.Destroy(bodyOwnerKey);
//                            UnityEngine.Debug.LogError($"Failed to create physics shape!");
//                            return;
//                        }
//                        //body.enabled = false;
//                        var shapeOwnerKey = shape.SetOwner(null);

//                        chains.Add(
//                                new PhysicsChainCollisionShape { Body = body, enabled = false, PhysicsChain = shape, frameindex = geometries[i].FrameIndex, State = geometries[i].State }
//                            );
//                    }


//                    //shape.triggerEvents = true;
//                    var bodyDefs = new PhysicsBodyDefinition
//                    {
//                        bodyType = RigidbodyType2D.Kinematic,
//                        position = new UnityEngine.Vector2(transform.Position.x, transform.Position.y),
//                        rotation = new PhysicsRotate(0f)
//                    };

//                    var bodys = world.CreateBody(bodyDefs);

//                    // Store references
//                    ecb.AddComponent(entity, new PhysicsBodyRef
//                    {
//                        Body = bodys,
//                        OwnerKey = bodys.SetOwner(null)
//                    });

//                    //// Track current physics state
//                    ecb.AddComponent(entity, new ActivePhysicsFrame
//                    {
//                        CurrentState = activeAnim.State,
//                        CurrentFrame = 0
//                    });

//                    //// Add frame tracker

//                    ecb.AddComponent(entity, new CurrentAnimFrame { FrameIndex = 0 });

//                }).WithoutBurst().Run();
//        }
//    }
//}