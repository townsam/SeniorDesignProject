using UnityEngine;

public interface IFlagEffectProvider
{
    bool TryGetInfluence(Transform actor, out FlagInfluence influence);
}

public abstract class FlagEffectProvider : MonoBehaviour, IFlagEffectProvider
{
    protected virtual void OnEnable()
    {
        FlagEffectRegistry.Register(this);
    }

    protected virtual void OnDisable()
    {
        FlagEffectRegistry.Unregister(this);
    }

    public abstract bool TryGetInfluence(Transform actor, out FlagInfluence influence);
}
