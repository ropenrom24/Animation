using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using RPG.Components;

namespace RPG.Systems
{
	// One-time setup to attach Entities Graphics components for rendering
	// Uses RenderMeshUtility with RenderMeshArray + MaterialMeshInfo
	[UpdateInGroup(typeof(InitializationSystemGroup))]
	[UpdateAfter(typeof(global::FlipbookSpawnerSystem))]
	public partial class RenderingBootstrapSystem : SystemBase
	{
		private EntityQuery _missingRenderQuery;
		private Mesh _mesh;
		private Material _npcMat;
		private Material _monsterMat;
		private RenderMeshArray _rma;
		private RenderMeshDescription _desc;

		protected override void OnCreate()
		{
			_missingRenderQuery = GetEntityQuery(new EntityQueryDesc
			{
				All = new[] { ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<Team>() },
				None = new[] { ComponentType.ReadOnly<MaterialMeshInfo>() }
			});
			RequireForUpdate(_missingRenderQuery);

			_mesh = CreateQuadMeshXY();
			_npcMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { enableInstancing = true, color = new Color(0.2f, 0.8f, 1f, 1f) };
			_monsterMat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { enableInstancing = true, color = new Color(1f, 0.3f, 0.2f, 1f) };
			_rma = new RenderMeshArray(new[] { _npcMat, _monsterMat }, new[] { _mesh });
			_desc = new RenderMeshDescription(shadowCastingMode: ShadowCastingMode.Off, receiveShadows: false);
		}

		protected override void OnUpdate()
		{
			var em = EntityManager;

			var toggleQuery = GetEntityQuery(ComponentType.ReadOnly<RPG.Components.RenderToggle>());
			if (!toggleQuery.IsEmpty)
			{
				var t = EntityManager.GetComponentData<RPG.Components.RenderToggle>(toggleQuery.GetSingletonEntity());
				if (t.Enabled == 0)
				{
					Enabled = false; return;
				}
			}

			Entities
				.WithNone<MaterialMeshInfo>()
				.WithAll<LocalTransform, Team>()
				.WithStructuralChanges()
				.WithoutBurst()
				.ForEach((Entity e, in Team team) =>
				{
					if (!em.HasComponent<LocalToWorld>(e)) em.AddComponent<LocalToWorld>(e);
					var mmi = MaterialMeshInfo.FromRenderMeshArrayIndices(team.Value == 0 ? 0 : 1, 0);
					RenderMeshUtility.AddComponents(e, em, _desc, _rma, mmi);
				})
				.Run();
		}

		private static Mesh CreateQuadMeshXY()
		{
			var m = new Mesh();
			// Quad in XY plane centered at origin, size 2.0 units for visibility
			var v = new Vector3[4]
			{
				new Vector3(-1.0f, -1.0f, 0f),
				new Vector3(-1.0f,  1.0f, 0f),
				new Vector3( 1.0f,  1.0f, 0f),
				new Vector3( 1.0f, -1.0f, 0f)
			};
			var uv = new Vector2[4]
			{
				new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0)
			};
			var tris = new int[6] { 0, 1, 2, 0, 2, 3 };
			m.SetVertices(v);
			m.SetUVs(0, uv);
			m.SetTriangles(tris, 0);
			m.RecalculateBounds();
			m.RecalculateNormals();
			return m;
		}
	}
}
