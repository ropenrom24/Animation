#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

// Ensures auto-refresh is enabled and triggers compilation when scripts change.
// Provides menu items to force-refresh and force-compile.
[InitializeOnLoad]
public static class AutoCompileUtility
{
	private const string AutoRefreshMenu = "Tools/Auto Refresh/Enable (Unity Preferences)";
	private const string ForceRefreshMenu = "Tools/Auto Refresh/Force Refresh";
	private const string ForceCompileMenu = "Tools/Auto Refresh/Force Compile";

	static AutoCompileUtility()
	{
		// Ensure auto-refresh is enabled so file changes trigger imports
		if (!EditorPrefs.GetBool("kAutoRefresh", true))
		{
			EditorPrefs.SetBool("kAutoRefresh", true);
			Debug.Log("[AutoCompileUtility] Enabled Auto Refresh (kAutoRefresh)");
		}

		// Hook to asset changes via AssetPostprocessor indirectly through delay call
		EditorApplication.projectChanged -= OnProjectChanged;
		EditorApplication.projectChanged += OnProjectChanged;

		// Hook compile lifecycle logs
		CompilationPipeline.compilationStarted -= OnCompilationStarted;
		CompilationPipeline.compilationStarted += OnCompilationStarted;
		CompilationPipeline.compilationFinished -= OnCompilationFinished;
		CompilationPipeline.compilationFinished += OnCompilationFinished;
	}

	[MenuItem(AutoRefreshMenu, false, 1)]
	private static void ToggleAutoRefresh()
	{
		bool enabled = EditorPrefs.GetBool("kAutoRefresh", true);
		EditorPrefs.SetBool("kAutoRefresh", !enabled);
		Debug.Log($"[AutoCompileUtility] Auto Refresh set to {!enabled}");
	}

	[MenuItem(ForceRefreshMenu, false, 2)]
	private static void ForceRefresh()
	{
		AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
		Debug.Log("[AutoCompileUtility] Forced AssetDatabase.Refresh");
	}

	[MenuItem(ForceCompileMenu, false, 3)]
	private static void ForceCompile()
	{
		// RequestScriptCompilation flags: CleanBuildRequest will force full rebuild
		CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache);
		Debug.Log("[AutoCompileUtility] Requested script compilation");
	}

	private static double _lastRequestTime;
	private const double RequestDebounceSeconds = 0.25; // debounce rapid bursts
	private static double _compileStartTime;

	private static void OnProjectChanged()
	{
		// Debounce to avoid spamming compile requests during large imports
		if (EditorApplication.timeSinceStartup - _lastRequestTime < RequestDebounceSeconds)
			return;

		_lastRequestTime = EditorApplication.timeSinceStartup;

		// If there are pending script changes, request compilation
		CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.None);
		Debug.Log("[AutoCompileUtility] Project changed: requested script compilation");
	}

	private static void OnCompilationStarted(object _)
	{
		_compileStartTime = EditorApplication.timeSinceStartup;
		Debug.Log("[AutoCompileUtility] Compilation started");
	}

	private static void OnCompilationFinished(object _)
	{
		double duration = EditorApplication.timeSinceStartup - _compileStartTime;
		Debug.Log($"[AutoCompileUtility] Compilation finished in {duration:F2}s");
	}
}
#endif
