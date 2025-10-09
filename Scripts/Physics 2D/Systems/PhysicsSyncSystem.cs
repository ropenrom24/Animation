//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Mathematics;
//using Unity.Transforms;
//using UnityEngine;
//using UnityEngine.LowLevelPhysics2D;
//using RPG.Components;

//namespace RPG.Systems
//{
//    /// <summary>
//    /// Syncs ECS transforms to Physics2D bodies safely.
//    /// Runs jobs in parallel to gather data, then applies physics changes on the main thread.
//    /// </summary>
//    [UpdateInGroup(typeof(SimulationSystemGroup))]
//    [UpdateBefore(typeof(TransformSystemGroup))]
//    public partial struct PhysicsSyncSystem : ISystem
//    {
//        private EntityQuery _query;

//        public void OnCreate(ref SystemState state)
//        {
//            _query = SystemAPI.QueryBuilder()
//                .WithAll<Alive>()
//                .WithAll<PhysicsEnabled>()
//                .WithAll<LocalTransform>()
//                .WithAll<PhysicsBodyRef>()
//                .WithAll<ActiveAnim>()
//                .WithAll<CurrentAnimFrame>()
//                .WithAll<ActivePhysicsFrame>()
//                .WithAll<PhysicsChainCollisionShape>()
//                .Build();
//        }

//        [BurstCompile]
//        public void OnUpdate(ref SystemState state)
//        {
//            // Preallocate result list to avoid AddNoResize overflow
//            int entityCount = _query.CalculateEntityCount();
//            if (entityCount == 0)
//                return;

//            var results = new NativeList<ChainSyncData>(1000, Allocator.TempJob);

//            // Collect phase (thread-safe)
//            new CollectPhysicsSyncJob
//            {
//                Results = results.AsParallelWriter()
//            }.ScheduleParallel(_query, state.Dependency).Complete();

//            // Apply phase (main thread)
//            for (int i = 0; i < results.Length; i++) 
//            {
//                var data = results[i];

//                // Defensive guard for invalid or destroyed physics bodies
//                if (data.Body.Equals(default(PhysicsBody)))
//                    continue;

//                try
//                {
//                    if (data.Enabled)
//                    {
//                        data.Body.position = data.Position;
//                        data.Body.enabled = true;
//                    }
//                    else
//                    {
//                        data.Body.enabled = false;
//                    }
//                }
//                catch (UnityException)
//                {
//                    // PhysicsBody handle was invalid (destroyed or missing)
//                    // Skip safely without crashing.
//                }
//            }

//            results.Dispose();
//        }

//        // Collects which bodies should be updated/enabled
//        [BurstCompile]
//        private partial struct CollectPhysicsSyncJob : IJobEntity
//        {
//            public NativeList<ChainSyncData>.ParallelWriter Results;

//            private void Execute(in LocalTransform transform,
//                                 in ActiveAnim activeAnim,
//                                 in CurrentAnimFrame currentFrame,
//                                 in ActivePhysicsFrame activePhysicsFrame,
//                                 in DynamicBuffer<PhysicsChainCollisionShape> chains)
//            {
//                for (int i = 0; i < chains.Length; i++)
//                {
//                    var chain = chains[i];

//                    bool enable = (chain.frameindex == currentFrame.FrameIndex &&
//                                   activePhysicsFrame.CurrentState == chain.State);

//                    Results.AddNoResize(new ChainSyncData
//                    {
//                        Body = chain.Body,
//                        Enabled = enable,
//                        Position = new Vector2(transform.Position.x, transform.Position.y)
//                    });
//                }
//            }
//        }

//        private struct ChainSyncData
//        {
//            public PhysicsBody Body;
//            public bool Enabled;
//            public Vector2 Position;
//        }
//    }
//}
