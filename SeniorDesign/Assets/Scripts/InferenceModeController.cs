using System;
using System.IO;
using Unity.MLAgents.Policies;
using UnityEngine;

#if UNITY_EDITOR
using Unity.Barracuda;
using UnityEditor;
#endif

/// <summary>
/// Editor-focused helper to switch all agents to inference using a trained model exported to results/.
/// In a player build, trained artifacts under results/ are not available; the methods no-op with warnings.
/// </summary>
public static class InferenceModeController
{
    public const string DefaultBehaviorName = "ActorBehavior";

    /// <summary>Switch all agents with matching BehaviorName to InferenceOnly and assign the NNModel.</summary>
    public static void EnableInferenceForAll(string behaviorName, UnityEngine.Object nnModelAsset)
    {
        if (string.IsNullOrEmpty(behaviorName))
        {
            behaviorName = DefaultBehaviorName;
        }

        if (nnModelAsset == null)
        {
            Debug.LogWarning("InferenceMode: No model asset provided; cannot enable inference.");
            return;
        }

        int changed = 0;
        BehaviorParameters[] all = UnityEngine.Object.FindObjectsByType<BehaviorParameters>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BehaviorParameters bp in all)
        {
            if (bp == null)
            {
                continue;
            }

            if (!string.Equals(bp.BehaviorName, behaviorName, StringComparison.Ordinal))
            {
                continue;
            }

            bp.Model = nnModelAsset as NNModel;
            bp.BehaviorType = BehaviorType.InferenceOnly;
            changed++;
        }

        Debug.Log($"InferenceMode: Enabled inference for BehaviorName='{behaviorName}' on {changed} agents.");
    }

    /// <summary>Switch all agents with matching BehaviorName back to Default (trainer/heuristic decides).</summary>
    public static void DisableInferenceForAll(string behaviorName)
    {
        if (string.IsNullOrEmpty(behaviorName))
        {
            behaviorName = DefaultBehaviorName;
        }

        int changed = 0;
        BehaviorParameters[] all = UnityEngine.Object.FindObjectsByType<BehaviorParameters>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BehaviorParameters bp in all)
        {
            if (bp == null)
            {
                continue;
            }

            if (!string.Equals(bp.BehaviorName, behaviorName, StringComparison.Ordinal))
            {
                continue;
            }

            bp.BehaviorType = BehaviorType.Default;
            changed++;
        }

        Debug.Log($"InferenceMode: Disabled inference for BehaviorName='{behaviorName}' on {changed} agents.");
    }

    /// <summary>
    /// Editor-only: copy the newest ONNX under results/&lt;runId&gt;/... to Assets and import it as an NNModel,
    /// then enable inference for all matching agents.
    /// </summary>
    public static void EnableInferenceFromResultsRun(string runId, string behaviorName = DefaultBehaviorName)
    {
#if UNITY_EDITOR
        string root = MlRunsScanner.GetProjectRoot();
        if (string.IsNullOrEmpty(root))
        {
            Debug.LogWarning("InferenceMode: Could not locate project root.");
            return;
        }

        string results = Path.Combine(root, "results", runId);
        if (!Directory.Exists(results))
        {
            Debug.LogWarning($"InferenceMode: results run folder not found: {results}");
            return;
        }

        string newest = FindNewestOnnx(results);
        if (string.IsNullOrEmpty(newest))
        {
            Debug.LogWarning($"InferenceMode: No .onnx found under: {results}");
            return;
        }

        string modelsDir = Path.Combine(Application.dataPath, "ML-Agents", "Models");
        Directory.CreateDirectory(modelsDir);

        string dstName = $"Inference_{runId}_{Path.GetFileName(newest)}";
        string dstFull = Path.Combine(modelsDir, dstName);
        File.Copy(newest, dstFull, overwrite: true);

        string assetPath = "Assets/ML-Agents/Models/" + dstName.Replace('\\', '/');
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        UnityEngine.Object nn = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
        if (nn == null)
        {
            Debug.LogWarning($"InferenceMode: Failed to load imported model asset at {assetPath}");
            return;
        }

        EnableInferenceForAll(behaviorName, nn);
#else
        Debug.LogWarning("InferenceMode: EnableInferenceFromResultsRun is Editor-only.");
#endif
    }

    public static void EnableInferenceFromBest(string behaviorName = DefaultBehaviorName)
    {
        EnableInferenceFromResultsRun("best", behaviorName);
    }

#if UNITY_EDITOR
    static string FindNewestOnnx(string rootDir)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(rootDir, "*.onnx", SearchOption.AllDirectories);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"InferenceMode: Failed to search for .onnx: {e.Message}");
            return null;
        }

        if (files == null || files.Length == 0)
        {
            return null;
        }

        string newest = files[0];
        DateTime newestTime = File.GetLastWriteTimeUtc(newest);
        for (int i = 1; i < files.Length; i++)
        {
            DateTime t = File.GetLastWriteTimeUtc(files[i]);
            if (t > newestTime)
            {
                newest = files[i];
                newestTime = t;
            }
        }

        return newest;
    }
#endif
}

