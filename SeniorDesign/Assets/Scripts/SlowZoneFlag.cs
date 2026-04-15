using UnityEngine;

public class SlowZoneFlag : FlagEffectProvider, IFlagRadius
{
    [Header("Zone")]
    public float radius = 4f;

    [Header("Effect")]
    [Range(0f, 1f)]
    public float moveSpeedMultiplier = 0.6f;

    public override bool TryGetInfluence(Transform actor, out FlagInfluence influence)
    {
        influence = FlagInfluence.Neutral;

        if (actor == null)
            return false;

        if (Vector3.Distance(transform.position, actor.position) > radius)
            return false;

        influence = new FlagInfluence
        {
            MoveSpeedMultiplier = moveSpeedMultiplier,
            JumpForceMultiplier = 1f,
            RewardMultiplier = 1f,
            ContinuousReward = 0f
        };

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public float GetRadius()
    {
        return radius;
    }
}
