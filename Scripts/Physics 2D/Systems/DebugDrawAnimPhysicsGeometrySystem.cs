//using Unity.Entities;
//using Unity.Transforms;
//using Unity.Mathematics;
//using Unity.Mathematics;
//using UnityEngine;
//using RPG.Components;

//// Put this in your project (e.g. Assets/Scripts/Debug/DebugDrawAnimPhysicsGeometrySystem.cs)
//[UpdateInGroup(typeof(PresentationSystemGroup))]
//public partial class DebugDrawAnimPhysicsGeometrySystem : SystemBase
//{
//    protected override void OnUpdate()
//    {
//        float duration = (float)SystemAPI.Time.DeltaTime;
//        Entities
//            .WithAll<PhysicsEnabled>() // only draw for physics-enabled entities (adjust as needed)
//            .ForEach((in LocalToWorld ltw, in DynamicBuffer<AnimFramePhysicsGeometry> buffer) =>
//            {
//                // localToWorld matrix for full transform (handles rotation/scale/translation)
//                float4x4 ltwMat = ltw.Value;

//                for (int gi = 0; gi < buffer.Length; gi++)
//                {
//                    var geom = buffer[gi];
//                    var poly = geom.Geometry;

//                    if (!poly.isValid)
//                        continue;

//                    int count = poly.count;
//                    if (count <= 0)
//                        continue;

//                    for (int i = 0; i < count; i++)
//                    {
//                        // Access vertices through the ShapeArray indexer
//                        Vector2 a = poly.vertices[i];
//                        Vector2 b = poly.vertices[(i + 1) % count];

//                        // Transform vertex (local polygon coords) into world space
//                        float3 wa_f = math.transform(ltwMat, new float3(a.x, a.y, 0f));
//                        float3 wb_f = math.transform(ltwMat, new float3(b.x, b.y, 0f));

//                        // Convert float3 -> Vector3 for Debug.DrawLine
//                        Vector3 wa = new Vector3(wa_f.x, wa_f.y, wa_f.z);
//                        Vector3 wb = new Vector3(wb_f.x, wb_f.y, wb_f.z);

//                        Debug.DrawLine(wa, wb, Color.cyan, duration, false);
//                    }
//                }
//            })
//            .WithoutBurst() // Debug.DrawLine is not Burst-compatible
//            .Run();
//    }
//}