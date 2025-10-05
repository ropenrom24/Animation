using Unity.Entities;
using UnityEngine;
using TMPro;
using RPG.Systems;

public class EcsCountersUI : MonoBehaviour
{
	[SerializeField] private TextMeshProUGUI text;
	[SerializeField] private int fontSize = 12;

	private EntityManager _em;
	private EntityQuery _q;
	private bool _initialized;

	void Awake()
	{
		TryInit();
	}

	void TryInit()
	{
		if (_initialized) return;
		var world = World.DefaultGameObjectInjectionWorld;
		if (world == null || !world.IsCreated) return;
		_em = world.EntityManager;
		_q = _em.CreateEntityQuery(ComponentType.ReadOnly<CounterSystem.RuntimeCounters>());
		_initialized = true;
		if (text) text.fontSize = fontSize;
	}

	void EnsureSingletonExists()
	{
		if (!_initialized) return;
		if (_q.IsEmpty)
		{
			// Create a default singleton so UI can bind immediately
			var e = _em.CreateEntity();
			_em.AddComponentData(e, new CounterSystem.RuntimeCounters());
			_em.SetName(e, "RuntimeCounters");
		}
	}

	void Update()
	{
		if (!text)
			return;

		if (!_initialized)
		{
			TryInit();
			if (!_initialized) return;
		}

		if (_q.IsEmpty)
		{
			EnsureSingletonExists();
			if (_q.IsEmpty) return;
		}

		var c = _q.GetSingleton<CounterSystem.RuntimeCounters>();
		text.text =
			$"Total Monster+NPC: {c.TotalAlive}\n" +
			$"Total Monster: {c.TotalAliveMonster}\n" +
			$"Total NPC: {c.TotalAliveNpc}\n" +
			$"Visible Monster+NPC: {c.TotalVisibleAlive}\n" +
			$"Visible Monster: {c.TotalVisibleAliveMonster}\n" +
			$"Visible NPC: {c.TotalVisibleAliveNpc}";
	}
}
