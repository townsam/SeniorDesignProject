using UnityEngine;

public class RewardBoostFlag : FlagEffectProvider, IFlagRadius
{
    [Header("Zone")]
    public float radius = 3f;

    [Header("Effect")]
    public float rewardMultiplier = 1.5f;
    public float continuousReward = 0.001f;

    public override bool TryGetInfluence(Transform actor, out FlagInfluence influence)
    {
        influence = FlagInfluence.Neutral;

        if (actor == null)
            return false;

        if (Vector3.Distance(transform.position, actor.position) > radius)
            return false;

        influence = new FlagInfluence
        {
            MoveSpeedMultiplier = 1f,
            JumpForceMultiplier = 1f,
            RewardMultiplier = rewardMultiplier,
            ContinuousReward = continuousReward
        };

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public float GetRadius()
    {
        return radius;
    }
}
