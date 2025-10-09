using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.LowLevelPhysics2D;
using RPG.Components;
using UnityEngine;

namespace RPG.Systems
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(BeginInitializationEntityCommandBufferSystem))]
    public partial struct PhysicsInitSystem : ISystem
    {
        private NativeQueue<PhysicsCreationRequest> _creationRequests;
        private PhysicsWorld world;

        private struct PhysicsCreationRequest
        {
            public Entity Entity;
            public float3 Position;
            public float Radius;
            public byte TeamId;
        }

        public void OnCreate(ref SystemState state)
        {
            world = PhysicsWorld.Create(new PhysicsWorldDefinition { drawOptions = PhysicsWorld.DrawOptions.Off });
            state.RequireForUpdate<BeginInitializationEntityCommandBufferSystem.Singleton>();
            _creationRequests = new NativeQueue<PhysicsCreationRequest>(Allocator.Persistent);
        }

        public void OnDestroy(ref SystemState state)
        {
            if (_creationRequests.IsCreated)
                _creationRequests.Dispose();
        }

        // Remove [BurstCompile] from OnUpdate since it calls non-Burst method 
        public void OnUpdate(ref SystemState state)
        {
            _creationRequests.Clear();

            // Phase 1: Parallel gathering (Burst-compiled)
            new GatherCreationRequestsJob
            {
                requests = _creationRequests.AsParallelWriter()
            }.ScheduleParallel();

            // Wait for gather to complete
            state.Dependency.Complete();

            // Phase 2: Process sequentially on main thread (no Burst)
            ProcessCreationRequests(ref state);
        }

        [BurstCompile]
        [WithNone(typeof(PhysicsBodyRef))]
        [WithAll(typeof(CollisionGeometry))]
        [WithAll(typeof(Alive))]
        private partial struct GatherCreationRequestsJob : IJobEntity
        {
            public NativeQueue<PhysicsCreationRequest>.ParallelWriter requests;

            private void Execute(Entity entity,
                                in LocalTransform transform,
                                in CollisionGeometry geometry,
                                in Team team)
            {
                requests.Enqueue(new PhysicsCreationRequest
                {
                    Entity = entity,
                    Position = transform.Position,
                    Radius = geometry.Radius,
                    TeamId = team.Value
                });
            }
        }

        // This method cannot be Burst-compiled due to PhysicsUserData containing managed references
        private void ProcessCreationRequests(ref SystemState state)
        {
            if (_creationRequests.Count == 0)
                return;

            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            while (_creationRequests.TryDequeue(out var request))
            {
                // Create body
                var bodyDef = new PhysicsBodyDefinition
                {
                    bodyType = RigidbodyType2D.Static,
                    position = new Vector2(request.Position.x, request.Position.y),
                    rotation = new PhysicsRotate(0f)
                };

                var body = world.CreateBody(bodyDef);
                var bodyOwnerKey = body.SetOwner(null);

                // Set userData - this is why we can't use Burst here
                body.userData = new PhysicsUserData
                {
                    intValue = request.Entity.Index,
                    physicsMaskValue = new PhysicsMask { bitMask = (ulong)(1 << request.TeamId) }
                };

                // Create shape
                var shapeDef = new PhysicsShapeDefinition
                {
                    isTrigger = true,
                    surfaceMaterial = new PhysicsShape.SurfaceMaterial
                    {
                        customColor = request.TeamId == 0 ? UnityEngine.Color.blue : UnityEngine.Color.red
                    }
                };

                var circleGeometry = new CircleGeometry { radius = request.Radius };
                var shape = body.CreateShape(circleGeometry, shapeDef);
                var shapeOwnerKey = shape.SetOwner(null);
                shape.triggerEvents = true;

                ecb.AddComponent(request.Entity, new PhysicsBodyRef
                {
                    Body = body,
                    OwnerKey = bodyOwnerKey
                });

                ecb.AddComponent(request.Entity, new PhysicsShapeRef
                {
                    Shape = shape,
                    OwnerKey = shapeOwnerKey
                });
            }
        }
    }
}