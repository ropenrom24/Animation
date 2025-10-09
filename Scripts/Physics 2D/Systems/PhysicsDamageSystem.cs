//using Unity.Burst;
//using Unity.Collections;
//using Unity.Entities;
//using Unity.Jobs;
//using UnityEngine.LowLevelPhysics2D;
//using RPG.Components;
//using UnityEngine;

//using Physics2DWorld = UnityEngine.LowLevelPhysics2D.PhysicsWorld;

//namespace RPG.Systems
//{
//    [BurstCompile]
//    [UpdateInGroup(typeof(SimulationSystemGroup))]
//    public partial struct PhysicsDamageSystem : ISystem
//    {
//        [BurstCompile]
//        public void OnUpdate(ref SystemState state)
//        {
//            var world = Physics2DWorld.defaultWorld;
//            world.autoTriggerCallbacks = true;

//            // ✅ Get trigger events (managed API)
//            var beginEvents = world.triggerBeginEvents;
//            var endEvents = world.triggerEndEvents;

//            if (beginEvents.Length == 0 && endEvents.Length == 0)
//                return;

//            // ✅ Copy to native array to process in jobs
//            var beginArray = new NativeArray<TriggerData>(beginEvents.Length, Allocator.TempJob);
//            var endArray = new NativeArray<TriggerData>(endEvents.Length, Allocator.TempJob);

//            int beginCount = 0;
//            foreach (var evt in beginEvents)
//            {
//                var trigger = evt.triggerShape;
//                var visitor = evt.visitorShape;

//                if (!trigger.isValid || !visitor.isValid)
//                    continue;

//                beginArray[beginCount++] = new TriggerData
//                {
//                    entityA = trigger.body.userData.intValue,
//                    entityB = visitor.body.userData.intValue,
//                    maskA = trigger.body.userData.physicsMaskValue.bitMask,
//                    maskB = visitor.body.userData.physicsMaskValue.bitMask,
//                };
//            }

//            int endCount = 0;
//            foreach (var evt in endEvents)
//            {
//                var trigger = evt.triggerShape;
//                var visitor = evt.visitorShape;

//                if (!trigger.isValid || !visitor.isValid)
//                    continue;

//                endArray[endCount++] = new TriggerData
//                {
//                    entityA = trigger.body.userData.intValue,
//                    entityB = visitor.body.userData.intValue,
//                    maskA = trigger.body.userData.physicsMaskValue.bitMask,
//                    maskB = visitor.body.userData.physicsMaskValue.bitMask,
//                };
//            }

//            // ✅ Trim unused space (optional)
//            var beginSlice = beginArray.GetSubArray(0, beginCount);
//            var endSlice = endArray.GetSubArray(0, endCount);

//            // ✅ Schedule jobs in parallel
//            var beginJob = new TriggerBeginJob { Events = beginSlice }.ScheduleParallel(beginCount, 32, default);
//            var endJob = new TriggerEndJob { Events = endSlice }.ScheduleParallel(endCount, 32, beginJob);

//            endJob.Complete();

//            beginArray.Dispose();
//            endArray.Dispose();
//        }

//        // ✅ Struct for Burst-safe data
//        private struct TriggerData
//        {
//            public int entityA;
//            public int entityB;
//            public ulong maskA;
//            public ulong maskB;
//        }

//        // ✅ Burst jobs (can add more ECS logic here)
//        [BurstCompile]
//        private struct TriggerBeginJob : IJobFor
//        {
//            [ReadOnly] public NativeArray<TriggerData> Events;

//            public void Execute(int index)
//            {
//                var evt = Events[index];
//                if (evt.maskA == evt.maskB)
//                    return;


//                //Debug.Log($"[Trigger Begin] Trigger={evt.maskA}, Visitor={evt.maskA}");
//                // Example: lightweight action (avoid Debug.Log in Burst)
//                // You could instead write to a NativeQueue<DamageEvent> here.
//            }
//        }

//        [BurstCompile]
//        private struct TriggerEndJob : IJobFor
//        {
//            [ReadOnly] public NativeArray<TriggerData> Events;

//            public void Execute(int index)
//            {
//                // Example: mark separation event or clear status
//            }
//        }
//    }
//}
