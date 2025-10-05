using Unity.Entities;
using UnityEngine;

// Singleton data the culling job reads each frame.
public struct CameraFrustumPlanes : IComponentData
{
    public Unity.Mathematics.float4 L, R, B, T, N, F;
}

// Drop this anywhere in the scene once.
public class CameraFrustumAuthoring : MonoBehaviour
{
    class Baker : Baker<CameraFrustumAuthoring>
    {
        public override void Bake(CameraFrustumAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent<CameraFrustumPlanes>(e);
        }
    }
}
