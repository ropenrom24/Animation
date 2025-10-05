// // Assets/Scripts/Systems/VisibleCountDebug.cs
// using Unity.Entities;
// using UnityEngine;

// [UpdateInGroup(typeof(SimulationSystemGroup))]
// public partial class VisibleCountDebug : SystemBase
// {
//     float _t;
//     protected override void OnUpdate()
//     {
//         _t += SystemAPI.Time.DeltaTime;
//         if (_t < 1f) return;
//         _t = 0f;

//         int enabled = 0, total = 0;
//         foreach (var _ in SystemAPI.Query<VisibleTag>()) total++;
//         foreach (var vis in SystemAPI.Query<EnabledRefRO<VisibleTag>>())
//             if (vis.ValueRO) enabled++;

//         Debug.Log($"[VisibleTag] enabled {enabled} / total {total}");
//     }
// }