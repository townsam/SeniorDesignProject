using UnityEngine;

public struct FlagInfluence
{
    public float MoveSpeedMultiplier;
    public float JumpForceMultiplier;
    public float RewardMultiplier;
    public float ContinuousReward;

    public static FlagInfluence Neutral => new FlagInfluence
    {
        MoveSpeedMultiplier = 1f,
        JumpForceMultiplier = 1f,
        RewardMultiplier = 1f,
        ContinuousReward = 0f
    };

    public void Combine(in FlagInfluence other)
    {
        MoveSpeedMultiplier *= Mathf.Max(0f, other.MoveSpeedMultiplier);
        JumpForceMultiplier *= Mathf.Max(0f, other.JumpForceMultiplier);
        RewardMultiplier *= Mathf.Max(0f, other.RewardMultiplier);
        ContinuousReward += other.ContinuousReward;
    }
}
