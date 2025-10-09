using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.LowLevelPhysics2D;
using RPG.Components;

namespace RPG.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(PerceptionSystem))]
    public partial struct ChaseAndMoveSystem : ISystem
    {
        private EntityQuery _query;

        public void OnCreate(ref SystemState state)
        {
            _query = state.GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    ComponentType.ReadWrite<State>(),
                    ComponentType.ReadWrite<Velocity>(),
                    ComponentType.ReadWrite<Heading>(),
                    ComponentType.ReadWrite<LocalTransform>(),
                    ComponentType.ReadOnly<MoveSpeed>(),
                    ComponentType.ReadOnly<Target>(),
                    ComponentType.ReadOnly<Perception>(),
                    ComponentType.ReadOnly<PhysicsBodyRef>(),
                    ComponentType.ReadOnly<Alive>()
                }
            });
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            int count = _query.CalculateEntityCount();
            if (count == 0)
                return;

            var batchTransforms = new NativeArray<PhysicsBody.BatchTransform>(
                count,
                Allocator.TempJob,
                NativeArrayOptions.UninitializedMemory
            );

            float dt = SystemAPI.Time.DeltaTime;

            var job = new ChaseAndMoveJob
            {
                DeltaTime = dt,
                BatchTransforms = batchTransforms
            };

            var jobHandle = job.ScheduleParallel(_query, state.Dependency);
            jobHandle.Complete();

            PhysicsBody.SetBatchTransform(batchTransforms);
            batchTransforms.Dispose();

            state.Dependency = default;
        }

        [BurstCompile]
        private partial struct ChaseAndMoveJob : IJobEntity
        {
            public float DeltaTime;

            [NativeDisableParallelForRestriction]
            public NativeArray<PhysicsBody.BatchTransform> BatchTransforms;

            private void Execute(
                ref State state,
                ref Velocity velocity,
                ref Heading heading,
                ref LocalTransform transform,
                in MoveSpeed moveSpeed,
                in Target target,
                in Perception perception,
                in PhysicsBodyRef bodyRef,
                [EntityIndexInQuery] int entityIndex)
            {
                AgentState currentState = state.Value;
                float3 pos = transform.Position;

                // Dead entities - early exit
                if (currentState == AgentState.Dead)
                {
                    WriteBatchTransform(entityIndex, bodyRef.Body, pos.xy, default);
                    return;
                }

                // Process Chase state (most common)
                if (currentState == AgentState.Chase)
                {
                    float3 toTarget = target.LastKnownPosition - pos;

                    // Manual distance squared calculation (faster than math.lengthsq)
                    float distSq = toTarget.x * toTarget.x + toTarget.y * toTarget.y;

                    if (distSq <= perception.AttackRadiusSq)
                    {
                        // Transition to attack
                        state.Value = AgentState.Attack;
                        velocity.Value = float3.zero;
                    }
                    else if (distSq > 1e-8f)
                    {
                        // Fast inverse square root
                        float invDist = math.rsqrt(distSq);

                        // Inline normalization (avoids function call overhead)
                        float3 normalizedDir;
                        normalizedDir.x = toTarget.x * invDist;
                        normalizedDir.y = toTarget.y * invDist;
                        normalizedDir.z = 0f;

                        heading.Value = normalizedDir;
                        velocity.Value = normalizedDir * moveSpeed.Value;
                    }
                }
                else if (currentState == AgentState.Attack)
                {
                    velocity.Value = float3.zero;
                }

                // Integrate position (manual check faster than lengthsq)
                float3 vel = velocity.Value;
                if (vel.x != 0f || vel.y != 0f || vel.z != 0f)
                {
                    pos.x += vel.x * DeltaTime;
                    pos.y += vel.y * DeltaTime;
                    pos.z += vel.z * DeltaTime;
                    transform.Position = pos;
                }

                // Compute rotation direction
                float2 headingXY = heading.Value.xy;
                float headingLenSq = headingXY.x * headingXY.x + headingXY.y * headingXY.y;

                PhysicsRotate rotation = default;
                if (headingLenSq > 1e-8f)
                {
                    rotation.direction = headingXY;
                }
                else
                {
                    rotation.direction = new float2(1f, 0f);
                }

                WriteBatchTransform(entityIndex, bodyRef.Body, pos.xy, rotation);
            }

            [BurstCompile]
            private void WriteBatchTransform(int index, PhysicsBody body, float2 position, PhysicsRotate rotation)
            {
                BatchTransforms[index] = new PhysicsBody.BatchTransform
                {
                    physicsBody = body,
                    position = position,
                    rotation = rotation
                };
            }
        }
    }
}