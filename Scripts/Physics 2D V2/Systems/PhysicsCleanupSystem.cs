using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using RPG.Components;
using UnityEngine.LowLevelPhysics2D;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace RPG.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(DeathSystem))]
    public partial struct PhysicsCleanupSystem : ISystem
    {
        private EntityQuery _cleanupQuery;

        public void OnCreate(ref SystemState state)
        {
            // Create query once - entities without Alive component but with physics 
            _cleanupQuery = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadOnly<PhysicsBodyRef>()
                },
                None = new ComponentType[]
                {
                    ComponentType.ReadOnly<Alive>()
                }
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = _cleanupQuery.CalculateEntityCount();
            if (count == 0)
                return;

            // Allocate arrays for batch destruction
            var entities = _cleanupQuery.ToEntityArray(Allocator.TempJob);
            var bodyRefs = _cleanupQuery.ToComponentDataArray<PhysicsBodyRef>(Allocator.TempJob);

            // Prepare batch data
            var batchBodies = new NativeArray<PhysicsBody>(count, Allocator.TempJob, NativeArrayOptions.UninitializedMemory);

            // Job to prepare batch arrays (can run in parallel if needed)
            var prepareJob = new PrepareDestructionBatchJob
            {
                BodyRefs = bodyRefs,
                BatchBodies = batchBodies
            };

            // For small counts, immediate execution is faster than scheduling overhead
            if (count < 100)
            {
                prepareJob.Run(count);
            }
            else
            {
                state.Dependency = prepareJob.Schedule(count, 64, state.Dependency);
                state.Dependency.Complete();
            }
            // Filter out invalid bodies (compact arrays)
            int validCount = 0;
            for (int i = 0; i < count; i++)
            {
                if (batchBodies[i].isValid)
                {
                    if (validCount != i)
                    {
                        batchBodies[validCount] = batchBodies[i];
                    }
                    validCount++;
                }
            }

            // Perform batch destruction (main thread only - API requirement)
            if (validCount > 0)
            {
                // Convert NativeSlice to Span for API compatibility
                unsafe
                {
                    var bodiesPtr = (PhysicsBody*)batchBodies.GetUnsafeReadOnlyPtr();
                    var bodiesSpan = new System.ReadOnlySpan<PhysicsBody>(bodiesPtr, validCount);

                    PhysicsBody.DestroyBatch(bodiesSpan);
                }
            }

            // Remove components from all entities using ECB
            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
                .CreateCommandBuffer(state.WorldUnmanaged);

            for (int i = 0; i < count; i++)
            {
                ecb.RemoveComponent<PhysicsBodyRef>(entities[i]);
                ecb.RemoveComponent<PhysicsShapeRef>(entities[i]);
                ecb.RemoveComponent<PhysicsEnabled>(entities[i]);
            }

            // Cleanup
            entities.Dispose();
            bodyRefs.Dispose();
            batchBodies.Dispose();
        }

        [BurstCompile]
        private struct PrepareDestructionBatchJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<PhysicsBodyRef> BodyRefs;
            [WriteOnly] public NativeArray<PhysicsBody> BatchBodies;

            public void Execute(int index)
            {
                var bodyRef = BodyRefs[index];
                BatchBodies[index] = bodyRef.Body;
            }
        }
    }
}