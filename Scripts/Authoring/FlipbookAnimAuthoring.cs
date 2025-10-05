using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FlipbookAnimAuthoring : MonoBehaviour
{
    [Header("Atlas (match the material)")]
    public int Rows = 5;
    public int Cols = 23;

    [Header("Per-row clips")]
    public ClipDef[] Clips =
    {
        new ClipDef{ State=AnimState.Idle,   RowIndex=0, Frames=9,  FPS=9,  StartCol=0, Loop=true },
        new ClipDef{ State=AnimState.Walk,   RowIndex=1, Frames=9,  FPS=12, StartCol=0, Loop=true },
        new ClipDef{ State=AnimState.Death,  RowIndex=2, Frames=9,  FPS=9,  StartCol=0, Loop=false },
        new ClipDef{ State=AnimState.Attack, RowIndex=3, Frames=9,  FPS=12, StartCol=0, Loop=false },
    };

    [Header("Initial state")]
    public AnimState InitialState = AnimState.Idle;
    public float InitialSpeed = 1f;
    public bool  InitialLoop  = true;

    [Serializable]
    public struct ClipDef
    {
        public AnimState State;
        public int   RowIndex;
        public int   Frames;
        public float FPS;
        public int   StartCol;
        public bool  Loop;
    }

    class Baker : Baker<FlipbookAnimAuthoring>
    {
        public override void Bake(FlipbookAnimAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Renderable);

            // logical state + cache
            AddComponent(e, new ActiveAnim  { State=a.InitialState, Speed=math.max(0.0001f,a.InitialSpeed), Loop=a.InitialLoop });
            AddComponent(e, new AppliedAnim { State=(AnimState)255, Speed=-1f }); // force initial apply

            // per-instance defaults (shader computes frames; these are just initial values)
            AddComponent(e, new AnimRow      { Value = 0 });
            AddComponent(e, new AnimFlipX    { Value = 0 });
            AddComponent(e, new AnimTint     { Value = new float4(1,1,1,1) });

            AddComponent(e, new AnimFrames   { Value = 1 });
            AddComponent(e, new AnimFPS      { Value = 1 });
            AddComponent(e, new AnimStartCol { Value = 0 });
            AddComponent(e, new AnimSpeedMul { Value = a.InitialSpeed });
            AddComponent(e, new AnimPhase    { Value = 0 });                  // randomized in spawner (optional)
            AddComponent(e, new AnimLoopFlag { Value = a.InitialLoop ? 1f:0f });

            // row/clip table
            var buf = AddBuffer<AnimClipInfo>(e);
            foreach (var c in a.Clips)
            {
                buf.Add(new AnimClipInfo
                {
                    State    = c.State,
                    RowIndex = math.max(0, c.RowIndex),
                    Frames   = math.max(1, c.Frames),
                    FPS      = math.max(0.0001f, c.FPS),
                    StartCol = math.max(0, c.StartCol),
                    Loop     = (byte)(c.Loop ? 1 : 0)
                });
            }
        }
    }
}
