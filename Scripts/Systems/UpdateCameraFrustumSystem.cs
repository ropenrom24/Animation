using Unity.Entities;
using UnityEngine;
using static Unity.Mathematics.math;
using Plane = UnityEngine.Plane;

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(FrustumCullingSystem))]
public partial class UpdateCameraFrustumSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (!SystemAPI.TryGetSingletonRW<CameraFrustumPlanes>(out var rw)) return;

        var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
        if (!cam) return;

        var planes = GeometryUtility.CalculateFrustumPlanes(cam);

        static Unity.Mathematics.float4 P(Plane p) =>
            new Unity.Mathematics.float4(p.normal.x, p.normal.y, p.normal.z, p.distance);

        var f = rw.ValueRW;
        f.L = P(planes[0]); f.R = P(planes[1]);
        f.B = P(planes[2]); f.T = P(planes[3]);
        f.N = P(planes[4]); f.F = P(planes[5]);
        rw.ValueRW = f;
    }
}
