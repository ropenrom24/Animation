using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
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

    // ---------- explicit arrays per state that you asked for ----------
    //[Header("Per-state sprite arrays (editable)")]
    //public Sprite[] IdleSprites;
    //public Sprite[] WalkSprites;
    //public Sprite[] DeathSprites;
    //public Sprite[] AttackSprites;

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

    /// <summary>
    /// Returns the Texture2D associated with the MeshRenderer's material (common _MainTex).
    /// </summary>
//    public Texture2D GetTextureFromMeshRenderer()
//    {
//        var mr = GetComponent<MeshRenderer>();
//        if (mr == null) return null;
//        var mat = mr.sharedMaterial;
//        if (mat == null) return null;
//        if (mat.mainTexture is Texture2D t) return t;
//        if (mat.HasProperty("_MainTex")) return mat.GetTexture("_MainTex") as Texture2D;
//        return null;
//    }

//#if UNITY_EDITOR
//    /// <summary>
//    /// Finds all Sprite assets in the project that reference the given Texture2D.
//    /// Editor-only (uses AssetDatabase).
//    /// </summary>
//    private List<Sprite> FindAllSpritesReferencingTexture(Texture2D tex)
//    {
//        var list = new List<Sprite>();
//        string[] guids = AssetDatabase.FindAssets("t:Sprite");
//        for (int i = 0; i < guids.Length; i++)
//        {
//            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
//            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
//            foreach (var a in assets)
//            {
//                if (a is Sprite sp && sp.texture == tex)
//                {
//                    list.Add(sp);
//                }
//            }
//        }
//        return list;
//    }

//    /// <summary>
//    /// If sprite names contain an integer (e.g. "Knight_0", "Knight_12"), map int -> sprite.
//    /// Returns dictionary of number->sprite.
//    /// </summary>
//    private Dictionary<int, Sprite> BuildNumericNameMap(List<Sprite> sprites)
//    {
//        var map = new Dictionary<int, Sprite>();
//        var rx = new Regex(@"(\d+)(?!.*\d)"); // capture last number in name
//        foreach (var s in sprites)
//        {
//            var m = rx.Match(s.name);
//            if (m.Success)
//            {
//                if (int.TryParse(m.Groups[1].Value, out int n))
//                {
//                    if (!map.ContainsKey(n)) map[n] = s;
//                    // else collision: keep first found
//                }
//            }
//        }
//        return map;
//    }

//    /// <summary>
//    /// Assign by flattened grid: index = row * Cols + col
//    /// Uses Clip metadata for per-state length and start col.
//    /// </summary>
//    private void AssignByFlattenedGrid(List<Sprite> flattened)
//    {
//        // flattened is ordered lexicographically - attempt to treat as row-major.
//        // If your assets are named with indexes that give correct lexicographic order, this will work.
//        // Otherwise consider re-naming or use numeric map approach.
//        for (int i = 0; i < (Clips?.Length ?? 0); ++i)
//        {
//            var clip = Clips[i];
//            int frameCount = Math.Max(0, clip.Frames);
//            Sprite[] frames = new Sprite[frameCount];
//            for (int f = 0; f < frameCount; ++f)
//            {
//                int col = clip.StartCol + f;
//                int row = clip.RowIndex;
//                int idx = row * Cols + col;
//                if (idx >= 0 && idx < flattened.Count) frames[f] = flattened[idx];
//                else frames[f] = null;
//            }
//            AssignFramesToNamedArray(clip.State, frames);
//        }
//    }

//    /// <summary>
//    /// Assign using numeric map: compute expected global index = row*Cols + (startCol + f)
//    /// If found in numberMap, use it. Also try per-row index fallback (startCol+f).
//    /// </summary>
//    private bool AssignByNumericIndexes(Dictionary<int, Sprite> numberMap)
//    {
//        bool any = false;
//        for (int i = 0; i < (Clips?.Length ?? 0); ++i)
//        {
//            var clip = Clips[i];
//            int frameCount = Math.Max(0, clip.Frames);
//            Sprite[] frames = new Sprite[frameCount];
//            for (int f = 0; f < frameCount; ++f)
//            {
//                int desiredGlobal = clip.RowIndex * Cols + (clip.StartCol + f);
//                if (numberMap.TryGetValue(desiredGlobal, out var s1))
//                {
//                    frames[f] = s1;
//                    any = true;
//                    continue;
//                }

//                int desiredPerRow = clip.StartCol + f;
//                if (numberMap.TryGetValue(desiredPerRow, out var s2))
//                {
//                    frames[f] = s2;
//                    any = true;
//                    continue;
//                }

//                // try direct per-frame number matching (f)
//                if (numberMap.TryGetValue(f, out var s3))
//                {
//                    frames[f] = s3;
//                    any = true;
//                    continue;
//                }

//                frames[f] = null;
//            }
//            AssignFramesToNamedArray(clip.State, frames);
//        }
//        return any;
//    }

//    /// <summary>
//    /// Sequential fallback: consume sprites in order to fill each clip's frames.
//    /// </summary>
//    private void AssignBySequentialFallback(List<Sprite> ordered)
//    {
//        int ptr = 0;
//        for (int i = 0; i < (Clips?.Length ?? 0); ++i)
//        {
//            var clip = Clips[i];
//            int frameCount = Math.Max(0, clip.Frames);
//            Sprite[] frames = new Sprite[frameCount];
//            for (int f = 0; f < frameCount; ++f)
//            {
//                if (ptr < ordered.Count) frames[f] = ordered[ptr++];
//                else frames[f] = null;
//            }
//            AssignFramesToNamedArray(clip.State, frames);
//        }
//    }

//    /// <summary>
//    /// Writes frames into the named arrays by state.
//    /// </summary>
//    private void AssignFramesToNamedArray(AnimState state, Sprite[] frames)
//    {
//        switch (state)
//        {
//            case AnimState.Idle: IdleSprites = frames; break;
//            case AnimState.Walk: WalkSprites = frames; break;
//            case AnimState.Death: DeathSprites = frames; break;
//            case AnimState.Attack: AttackSprites = frames; break;
//            default: break;
//        }
//    }


//#endif

//#if UNITY_EDITOR
//    /// <summary>
//    /// Populate the named arrays by mapping Sprite assets to grid positions using sprite.textureRect (pixel coords).
//    /// This is robust when your sprites are regular slices on the texture.
//    /// Falls back to numeric name map if needed for missing frames.
//    /// </summary>
//    [ContextMenu("Populate Named State Arrays From Material Texture (position-mapped)")]
//    public bool PopulateNamedStateArraysFromMaterialTexture_PositionMapped()
//    {
//        Texture2D tex = GetTextureFromMeshRenderer();
//        if (tex == null)
//        {
//            Debug.LogWarning("No texture found on MeshRenderer material.");
//            return false;
//        }

//        var sprites = FindAllSpritesReferencingTexture(tex);
//        if (sprites == null || sprites.Count == 0)
//        {
//            Debug.LogWarning("No Sprite assets reference the material texture.");
//            return false;
//        }

//        // frame pixel dimensions (floating)
//        float frameW = (float)tex.width / Cols;
//        float frameH = (float)tex.height / Rows;
//        if (frameW <= 0 || frameH <= 0)
//        {
//            Debug.LogWarning("Invalid Rows/Cols or texture size.");
//            return false;
//        }

//        // map from globalIndex (row * Cols + col) -> Sprite
//        var posMap = new Dictionary<int, Sprite>();

//        foreach (var s in sprites)
//        {
//            // prefer textureRect which gives pixel rect inside texture (bottom-left origin)
//            Rect r = s.textureRect;

//            // compute col from x
//            float approxColF = r.x / frameW;
//            float approxRowFromBottomF = r.y / frameH;

//            int col = Mathf.RoundToInt(approxColF);
//            int rowFromBottom = Mathf.RoundToInt(approxRowFromBottomF);

//            // clamp to range
//            col = Mathf.Clamp(col, 0, Cols - 1);
//            rowFromBottom = Mathf.Clamp(rowFromBottom, 0, Rows - 1);

//            // convert to our RowIndex convention: if RowIndex 0 is top row in Clips, convert:
//            int row = (Rows - 1 - rowFromBottom);

//            int globalIndex = row * Cols + col;

//            // Only add if slot not already filled (avoid collisions)
//            if (!posMap.ContainsKey(globalIndex))
//                posMap[globalIndex] = s;
//            else
//            {
//                // keep first found; you can log collisions if needed
//                // Debug.LogWarning($"Collision mapping sprite to index {globalIndex}: {s.name}");
//            }
//        }

//        // Now use Clips to build arrays
//        bool anyAssigned = false;
//        for (int i = 0; i < (Clips?.Length ?? 0); ++i)
//        {
//            var clip = Clips[i];
//            int frameCount = Math.Max(0, clip.Frames);
//            var frames = new Sprite[frameCount];
//            for (int f = 0; f < frameCount; ++f)
//            {
//                int col = clip.StartCol + f;
//                int row = clip.RowIndex;
//                int global = row * Cols + col;
//                if (posMap.TryGetValue(global, out var sp))
//                {
//                    frames[f] = sp;
//                    anyAssigned = true;
//                }
//                else
//                {
//                    frames[f] = null; // left for fallback or manual fill
//                }
//            }
//            AssignFramesToNamedArray(clip.State, frames);
//        }

//        // If nothing assigned (e.g. sprites are tightly packed or trimmed and textureRect mapping failed),
//        // fall back to numeric-name mapping for best-effort (like Knight_0, Knight_1 style).
//        if (!anyAssigned)
//        {
//            var numberMap = BuildNumericNameMap(sprites); // reuses your numeric regex helper
//            if (numberMap.Count > 0)
//            {
//                bool assignedByNumber = AssignByNumericIndexes(numberMap);
//                if (assignedByNumber)
//                {
//                    EditorUtility.SetDirty(this);
//                    Debug.Log("Populated named arrays by numeric suffix fallback.");
//                    return true;
//                }
//            }

//            // final fallback: sequential lexicographic assignment
//            sprites.Sort((a, b) => String.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
//            AssignBySequentialFallback(sprites);
//            EditorUtility.SetDirty(this);
//            Debug.Log("Populated named arrays by sequential fallback (lexicographic). Verify in inspector.");
//            return true;
//        }

//        EditorUtility.SetDirty(this);
//        Debug.Log("Populated named arrays using texture-position mapping (preferred). Please verify ordering in inspector for any nulls.");
//        return true;
//    }
//#endif

}
