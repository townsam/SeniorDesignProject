using System;

/// <summary>
/// Lightweight summary of one ML-Agents results folder (results/&lt;run-id&gt;/).
/// Metrics are read from run_logs/timers.json when present.
/// </summary>
public sealed class MlRunSummary
{
    public string RunId { get; }
    public string RunDirectory { get; }
    public DateTime LastActivityUtc { get; }
    public double? LatestStepMean { get; }
    public double? CumulativeRewardMean { get; }
    public int OnnxCheckpointCount { get; }

    public MlRunSummary(
        string runId,
        string runDirectory,
        DateTime lastActivityUtc,
        double? latestStepMean,
        double? cumulativeRewardMean,
        int onnxCheckpointCount)
    {
        RunId = runId;
        RunDirectory = runDirectory;
        LastActivityUtc = lastActivityUtc;
        LatestStepMean = latestStepMean;
        CumulativeRewardMean = cumulativeRewardMean;
        OnnxCheckpointCount = onnxCheckpointCount;
    }

    public string BuildDisplayLine()
    {
        string step = LatestStepMean.HasValue
            ? $"step ≈ {LatestStepMean.Value:0}"
            : "step: (no timers yet)";
        string reward = CumulativeRewardMean.HasValue
            ? $"  reward μ {CumulativeRewardMean.Value:0.###}"
            : "";
        string ckpt = OnnxCheckpointCount > 0 ? $"  · {OnnxCheckpointCount} .onnx" : "";
        return $"{RunId}  ·  {step}{reward}{ckpt}  ·  updated {LastActivityUtc.ToLocalTime():g}";
    }
}
