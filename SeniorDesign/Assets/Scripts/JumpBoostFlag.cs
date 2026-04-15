using UnityEngine;

public class JumpBoostFlag : FlagEffectProvider, IFlagRadius
{
    [Header("Zone")]
    public float radius = 3.5f;

    [Header("Effect")]
    [Min(0.1f)]
    public float jumpForceMultiplier = 1.35f;

    public override bool TryGetInfluence(Transform actor, out FlagInfluence influence)
    {
        influence = FlagInfluence.Neutral;

        if (actor == null)
        {
            return false;
        }

        if (Vector3.Distance(transform.position, actor.position) > radius)
        {
            return false;
        }

        influence = new FlagInfluence
        {
            MoveSpeedMultiplier = 1f,
            JumpForceMultiplier = jumpForceMultiplier,
            RewardMultiplier = 1f,
            ContinuousReward = 0f
        };

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.65f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }

    public float GetRadius()
    {
        return radius;
    }
}
