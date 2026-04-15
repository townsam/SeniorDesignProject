using System.Collections.Generic;
using UnityEngine;

public static class FlagEffectRegistry
{
    private static readonly List<IFlagEffectProvider> providers = new List<IFlagEffectProvider>();

    public static void Register(IFlagEffectProvider provider)
    {
        if (provider == null || providers.Contains(provider))
            return;

        providers.Add(provider);
    }

    public static void Unregister(IFlagEffectProvider provider)
    {
        providers.Remove(provider);
    }

    /// <summary>Non-null providers currently registered (e.g. for ML training diagnostics).</summary>
    public static int GetActiveProviderCount()
    {
        int n = 0;
        for (int i = 0; i < providers.Count; i++)
        {
            if (providers[i] != null)
            {
                n++;
            }
        }

        return n;
    }

    public static FlagInfluence GetCombinedInfluence(Transform actor)
    {
        FlagInfluence combined = FlagInfluence.Neutral;

        for (int i = 0; i < providers.Count; i++)
        {
            var provider = providers[i];
            if (provider == null)
                continue;

            if (provider.TryGetInfluence(actor, out var influence))
            {
                combined.Combine(influence);
            }
        }

        return combined;
    }
}

public class RadiusFlagEffect : FlagEffectProvider
{
    [Header("Effect Zone")]
    public float radius = 3f;

    [Header("Influence")]
    public float moveSpeedMultiplier = 1f;
    public float jumpForceMultiplier = 1f;
    public float rewardMultiplier = 1f;
    public float continuousReward = 0f;

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
            JumpForceMultiplier = jumpForceMultiplier,
            RewardMultiplier = rewardMultiplier,
            ContinuousReward = continuousReward
        };

        return true;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
