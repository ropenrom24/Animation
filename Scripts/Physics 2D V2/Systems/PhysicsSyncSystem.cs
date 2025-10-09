//using Unity.Burst;
//using Unity.Entities;
//using Unity.Transforms;
//using RPG.Components;
//using UnityEngine;

//namespace RPG.Systems
//{
//    [BurstCompile]
//    [UpdateInGroup(typeof(SimulationSystemGroup))]
//    [UpdateBefore(typeof(TransformSystemGroup))]
//    public partial struct PhysicsSyncSystem : ISystem
//    {
//        [BurstCompile]
//        public void OnUpdate(ref SystemState state)
//        {
//            // ✅ FULLY BURST + PARALLEL - No Create/Destroy
//            new SyncTransformToPhysicsJob().ScheduleParallel();
//        }

//        [BurstCompile]
//        [WithAll(typeof(Alive), typeof(PhysicsBodyRef))]
//        private partial struct SyncTransformToPhysicsJob : IJobEntity
//        {
//            private void Execute(in LocalTransform transform,
//                                in PhysicsBodyRef bodyRef)
//            {
//                if (!bodyRef.Body.isValid)
//                    return;

//                // ✅ Thread-safe: Writing position is allowed in Jobs!
//                bodyRef.Body.position = new Vector2(transform.Position.x, transform.Position.y);
//            }
//        }
//    }
//}