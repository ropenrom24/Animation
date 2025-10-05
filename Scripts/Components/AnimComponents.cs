using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering; // [MaterialProperty] lives here (Entities Graphics)

public enum AnimState : byte { Idle, Walk, Death, Attack }

// ── Per-instance GPU properties (names MUST match the shader) ───────────────
[MaterialProperty("_Row")]        public struct AnimRow      : IComponentData { public float  Value; } // 0 = top row
[MaterialProperty("_FlipX")]      public struct AnimFlipX    : IComponentData { public float  Value; } // 0/1
[MaterialProperty("_Tint")]       public struct AnimTint     : IComponentData { public float4 Value; } // RGBA

[MaterialProperty("_Frames")]     public struct AnimFrames   : IComponentData { public float Value; }
[MaterialProperty("_FPS")]        public struct AnimFPS      : IComponentData { public float Value; }
[MaterialProperty("_StartCol")]   public struct AnimStartCol : IComponentData { public float Value; }
[MaterialProperty("_Speed")]      public struct AnimSpeedMul : IComponentData { public float Value; }
[MaterialProperty("_Phase")]      public struct AnimPhase    : IComponentData { public float Value; } // seconds offset
[MaterialProperty("_Loop")]       public struct AnimLoopFlag : IComponentData { public float Value; } // 1 loop / 0 clamp

// ── HP bar material properties (Dota-like rectangular overlay) ─────────────
[MaterialProperty("_Hp01")]           public struct Hp01         : IComponentData { public float  Value; } // 0..1; <0 hide
[MaterialProperty("_HpBarSize")]      public struct HpBarSize    : IComponentData { public float2 Value; }
[MaterialProperty("_HpBarOffset")]    public struct HpBarOffset  : IComponentData { public float2 Value; }
[MaterialProperty("_HpBarTint")]      public struct HpBarTint    : IComponentData { public float4 Value; }
[MaterialProperty("_HpBarBackTint")]  public struct HpBarBackTint: IComponentData { public float4 Value; }

// ── Logical state (we no longer track Time on CPU) ──────────────────────────
public struct ActiveAnim : IComponentData
{
    public AnimState State;
    public float Speed;  // playback speed multiplier
    public bool  Loop;   // single-shot if false (e.g., Death)
}

// Small cache so we only touch GPU properties when something actually changes
public struct AppliedAnim : IComponentData
{
    public AnimState State;
    public float     Speed;
}

// Atlas row metadata
public struct AnimClipInfo : IBufferElementData
{
    public AnimState State;
    public int   RowIndex; // 0 = top row
    public int   Frames;   // frames in that row
    public float FPS;      // frames per second
    public int   StartCol; // starting column
    public byte  Loop;     // 1 loop, 0 clamp
}

// Simple helper to map gameplay state -> animation state
public static class AnimStateMapper
{
    public static AnimState FromAgentState(RPG.Components.AgentState agent)
    {
        switch (agent)
        {
            case RPG.Components.AgentState.Wander:
            case RPG.Components.AgentState.Chase:
                return AnimState.Walk;
            case RPG.Components.AgentState.Attack:
                return AnimState.Attack;
            case RPG.Components.AgentState.Dead:
                return AnimState.Death;
            case RPG.Components.AgentState.Idle:
            default:
                return AnimState.Idle;
        }
    }
}
