using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using RPG.Components;

// Aspect groups all the components we need so the query has only 1 generic arg.
public readonly partial struct FlipbookAnimAspect : IAspect
{
    // READ
    public readonly RefRO<ActiveAnim> Active;

    // READ/WRITE
    public readonly RefRW<AppliedAnim>   Applied;
    public readonly RefRW<AnimRow>       Row;
    public readonly RefRW<AnimFrames>    Frames;
    public readonly RefRW<AnimFPS>       FPS;
    public readonly RefRW<AnimStartCol>  StartCol;
    public readonly RefRW<AnimSpeedMul>  SpeedMul;
    public readonly RefRW<AnimLoopFlag>  LoopFlag;

    // Buffer
    public readonly DynamicBuffer<AnimClipInfo> Clips;
}

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct FlipbookAnimSystem : ISystem
{
    [BurstCompile] public void OnCreate(ref SystemState state) {}
    [BurstCompile] public void OnDestroy(ref SystemState state) {}

    // Apply GPU properties only when State/Speed changes (no per-frame uploads).
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new ApplyAnimJob { elapsedTime = (float)SystemAPI.Time.ElapsedTime };
        job.ScheduleParallel();
    }

    [BurstCompile]
    [WithAll(typeof(VisibleTag))]
    [WithChangeFilter(typeof(ActiveAnim), typeof(Heading))]
    private partial struct ApplyAnimJob : IJobEntity
    {
        public float elapsedTime;
        private void Execute(EnabledRefRO<VisibleTag> visible,
                             in ActiveAnim active,
                             in Heading heading,
                             ref AppliedAnim applied,
                             ref AnimRow row,
                             ref AnimFrames frames,
                             ref AnimFPS fps,
                             ref AnimStartCol startCol,
                             ref AnimSpeedMul speedMul,
                             ref AnimLoopFlag loopFlag,
                             ref AnimPhase phase,
                             ref AnimFlipX flip,
                             DynamicBuffer<AnimClipInfo> clips)
        {
            if (!visible.ValueRO) return;

            // Always update flip from heading.x even if ActiveAnim didn't change
            const float deadZone = 0.0005f;
            float hx = heading.Value.x;
            float targetFlip = flip.Value;
            if (hx > deadZone) targetFlip = 0f;        // face right
            else if (hx < -deadZone) targetFlip = 1f;  // face left
            if (targetFlip != flip.Value) flip.Value = targetFlip;

            bool needApply = active.State != applied.State || active.Speed != applied.Speed;
            if (!needApply) return;

            // Find matching clip
            AnimClipInfo chosen = default;
            bool found = false;
            var clipsArr = clips.AsNativeArray();
            for (int i = 0; i < clipsArr.Length; i++)
            {
                if (clipsArr[i].State == active.State) { chosen = clipsArr[i]; found = true; break; }
            }
            if (!found) return;

            // Write per-instance GPU params once; shader computes frames from _Time
            row   = new AnimRow      { Value = chosen.RowIndex };
            frames= new AnimFrames   { Value = math.max(1, chosen.Frames) };
            fps   = new AnimFPS      { Value = math.max(0.0001f, chosen.FPS) };
            startCol = new AnimStartCol { Value = math.max(0, chosen.StartCol) };
            float speedVal = math.max(0.0001f, active.Speed);
            speedMul = new AnimSpeedMul { Value = speedVal };
            loopFlag = new AnimLoopFlag { Value = (active.Loop && chosen.Loop == 1) ? 1f : 0f };

            // Reset phase so the new clip starts at its first frame
            phase = new AnimPhase { Value = -elapsedTime * speedVal };

            applied = new AppliedAnim { State = active.State, Speed = active.Speed };
        }
    }
}
