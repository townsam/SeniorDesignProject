using UnityEngine;
using UnityEngine.UI;

public class FlagButton : MonoBehaviour
{
    public GameObject prefab;

    public void SelectObject()
    {
        if (prefab == null)
        {
            UnityEngine.Debug.LogWarning("FlagButton: No prefab assigned.");
            return;
        }

        PlacementManager placementManager = FindFirstObjectByType<PlacementManager>();
        if (placementManager == null)
        {
            UnityEngine.Debug.LogWarning("FlagButton: No PlacementManager found in scene.");
            return;
        }

        placementManager.SelectObject(prefab);
    }
}