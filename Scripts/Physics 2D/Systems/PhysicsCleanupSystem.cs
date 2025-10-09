//using Unity.Burst;
//using Unity.Entities;
//using RPG.Components;

//namespace RPG.Systems
//{
//    // Destroys physics bodies when entities die
//    [UpdateInGroup(typeof(SimulationSystemGroup))]
//    [UpdateAfter(typeof(DeathSystem))]
//    public partial class PhysicsCleanupSystem : SystemBase
//    {
//        protected override void OnUpdate()
//        {
//            var ecb = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>()
//                .CreateCommandBuffer(World.Unmanaged);

//            // Clean up physics for dead entities
//            Entities
//                .WithNone<Alive>()
//                .WithAll<PhysicsBodyRef, CollisionGeometry>()
//                .ForEach((Entity entity,
//                         in PhysicsBodyRef bodyRef,
//                         in PhysicsShapeRef shapeRef) =>
//                {
//                    if (shapeRef.Shape.isValid)
//                    {
//                        shapeRef.Shape.Destroy(true, shapeRef.OwnerKey);
//                    }
//                    if (bodyRef.Body.isValid)
//                    {
//                        bodyRef.Body.Destroy(bodyRef.OwnerKey);
//                    }

//                    // Remove physics components
//                    ecb.RemoveComponent<PhysicsBodyRef>(entity);
//                    ecb.RemoveComponent<PhysicsShapeRef>(entity);
//                    ecb.RemoveComponent<PhysicsEnabled>(entity);

//                }).WithoutBurst().Run(); // Destroy must be on main thread
//        }
//    }
//}