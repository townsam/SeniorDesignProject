using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Scans the repo <c>results/</c> folder next to <see cref="Application.dataPath"/> and parses
/// <c>run_logs/timers.json</c> gauge snapshots (same source TensorBoard uses indirectly).
/// </summary>
public static class MlRunsScanner
{
    static readonly Regex GaugeEntryRegex = new Regex(
        "\"([^\"]+)\"\\s*:\\s*\\{\\s*\"value\"\\s*:\\s*([-0-9.eE+]+)",
        RegexOptions.Compiled);

    public static string GetProjectRoot()
    {
        string assets = Application.dataPath;
        if (string.IsNullOrEmpty(assets))
        {
            return null;
        }

        return Path.GetDirectoryName(assets);
    }

    public static string GetResultsDirectory()
    {
        string root = GetProjectRoot();
        return string.IsNullOrEmpty(root) ? null : Path.Combine(root, "results");
    }

    public static string GetStartScriptPath()
    {
        string root = GetProjectRoot();
        if (string.IsNullOrEmpty(root))
        {
            return null;
        }

        // Training launchers are platform-specific.
        // - Windows: Start-MLSmoke.ps1 / .cmd (PowerShell + TensorBoard)
        // - macOS/Linux: Start-MLSmoke.sh (bash)
        if (Application.platform == RuntimePlatform.WindowsEditor ||
            Application.platform == RuntimePlatform.WindowsPlayer)
        {
            return Path.Combine(root, "Start-MLSmoke.ps1");
        }

        return Path.Combine(root, "Start-MLSmoke.sh");
    }

    /// <summary>
    /// Returns summaries sorted by most recently modified run folder first.
    /// </summary>
    public static List<MlRunSummary> ListRuns()
    {
        var list = new List<MlRunSummary>();
        string results = GetResultsDirectory();
        if (string.IsNullOrEmpty(results) || !Directory.Exists(results))
        {
            return list;
        }

        foreach (string dir in Directory.GetDirectories(results))
        {
            string runId = Path.GetFileName(dir);
            if (string.IsNullOrEmpty(runId))
            {
                continue;
            }

            list.Add(BuildSummaryForRunDirectory(runId, dir));
        }

        list.Sort((a, b) => b.LastActivityUtc.CompareTo(a.LastActivityUtc));
        return list;
    }

    static MlRunSummary BuildSummaryForRunDirectory(string runId, string dir)
    {
        DateTime last = Directory.GetLastWriteTimeUtc(dir);
        string timersPath = Path.Combine(dir, "run_logs", "timers.json");
        double? stepMean = null;
        double? rewardMean = null;

        if (File.Exists(timersPath))
        {
            try
            {
                last = File.GetLastWriteTimeUtc(timersPath);
                string json = File.ReadAllText(timersPath);
                ParseGaugeMetrics(json, out stepMean, out rewardMean);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MlRunsScanner: could not read {timersPath}: {e.Message}");
            }
        }

        int onnx = 0;
        try
        {
            onnx = Directory.GetFiles(dir, "*.onnx", SearchOption.AllDirectories).Length;
        }
        catch
        {
            // ignore
        }

        return new MlRunSummary(runId, dir, last, stepMean, rewardMean, onnx);
    }

    static void ParseGaugeMetrics(string json, out double? maxStepMean, out double? cumulativeRewardMean)
    {
        maxStepMean = null;
        cumulativeRewardMean = null;

        foreach (Match m in GaugeEntryRegex.Matches(json))
        {
            if (m.Groups.Count < 3)
            {
                continue;
            }

            string key = m.Groups[1].Value;
            if (!double.TryParse(m.Groups[2].Value, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double v))
            {
                continue;
            }

            if (key.EndsWith(".Step.mean", StringComparison.Ordinal))
            {
                if (!maxStepMean.HasValue || v > maxStepMean.Value)
                {
                    maxStepMean = v;
                }
            }
            else if (key.EndsWith(".Environment.CumulativeReward.mean", StringComparison.Ordinal))
            {
                cumulativeRewardMean = v;
            }
        }
    }
}
