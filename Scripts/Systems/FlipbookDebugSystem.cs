// using Unity.Burst;
// using Unity.Entities;
// using Unity.Mathematics;

// [BurstCompile]
// public partial struct FlipbookDebugSystem : ISystem
// {
//     float timer;

//     [BurstCompile] public void OnCreate(ref SystemState state) { }
//     [BurstCompile] public void OnDestroy(ref SystemState state) { }

//     public void OnUpdate(ref SystemState state)
//     {
//         float dt = SystemAPI.Time.DeltaTime;
//         timer += dt;

//         foreach (var (frame, row, tint) in 
//                  SystemAPI.Query<RefRW<AnimFrame>, RefRW<AnimRow>, RefRW<AnimTint>>())
//         {
//             // Animate frame value slowly (debug)
//             frame.ValueRW.Value = (int)(timer * 5) % 10;
//             row.ValueRW.Value   = (int)(timer) % 5;

//             // Assign tint based on row index
//             switch ((int)row.ValueRW.Value % 3)
//             {
//                 case 0: tint.ValueRW.Value = new float4(1,0,0,1); break; // red
//                 case 1: tint.ValueRW.Value = new float4(0,1,0,1); break; // green
//                 case 2: tint.ValueRW.Value = new float4(0,0,1,1); break; // blue
//             }
//         }
//     }
// }
