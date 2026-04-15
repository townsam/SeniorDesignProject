using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

/// <summary>
/// While the Python trainer is connected, activates pre-placed flag objects and/or spawns flag prefabs
/// along the line from <see cref="ActorSpawner.spawnPoint"/> to the goal so <see cref="FlagEffectRegistry"/>
/// is not empty and observations/rewards actually vary with zones.
/// </summary>
public class MlTrainingFlagSeeder : MonoBehaviour
{
    [Tooltip("If true, only run when mlagents-learn has connected (normal gameplay is unchanged).")]
    public bool onlyWhenTrainerConnected = true;

    [Header("Option A — enable existing objects (inactive by default in hierarchy)")]
    public GameObject[] activateWhenTraining;

    [Header("Option B — spawn between spawn and goal")]
    public ActorSpawner spawner;
    public Transform goalOverride;
    public GameObject[] flagPrefabsAlongPath;
    [Tooltip("One entry per prefab slot; 0=start, 1=goal. If empty, prefabs are spaced evenly.")]
    public float[] normalizedPathPositions = new float[] { 0.22f, 0.48f, 0.72f };

    bool applied;
    readonly List<GameObject> spawnedInstances = new List<GameObject>();

    void Update()
    {
        if (applied)
        {
            return;
        }

        if (onlyWhenTrainerConnected && (Academy.Instance == null || !Academy.Instance.IsCommunicatorOn))
        {
            return;
        }

        ApplyActivateGroup();
        ApplySpawnsAlongPath();
        applied = true;

        int reg = FlagEffectRegistry.GetActiveProviderCount();
        UnityEngine.Debug.Log($"MlTrainingFlagSeeder: applied training layout. FlagEffectRegistry providers = {reg}.");
    }

    void ApplyActivateGroup()
    {
        if (activateWhenTraining == null)
        {
            return;
        }

        foreach (GameObject go in activateWhenTraining)
        {
            if (go != null)
            {
                go.SetActive(true);
            }
        }
    }

    void ApplySpawnsAlongPath()
    {
        if (flagPrefabsAlongPath == null || flagPrefabsAlongPath.Length == 0)
        {
            return;
        }

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<ActorSpawner>();
        }

        if (spawner == null)
        {
            UnityEngine.Debug.LogWarning("MlTrainingFlagSeeder: no ActorSpawner; cannot spawn along path.");
            return;
        }

        Transform goal = goalOverride != null ? goalOverride : spawner.defaultTarget;
        if (goal == null)
        {
            WinZone wz = FindFirstObjectByType<WinZone>();
            if (wz != null)
            {
                goal = wz.transform;
            }
        }

        if (goal == null)
        {
            UnityEngine.Debug.LogWarning("MlTrainingFlagSeeder: no goal transform; cannot spawn along path.");
            return;
        }

        Vector3 start = spawner.spawnPoint;
        Vector3 end = goal.position;
        bool useEvenSpacing = normalizedPathPositions == null || normalizedPathPositions.Length == 0;
        int count = flagPrefabsAlongPath.Length;

        for (int i = 0; i < count; i++)
        {
            GameObject prefab = flagPrefabsAlongPath[i];
            if (prefab == null)
            {
                continue;
            }

            float t = useEvenSpacing
                ? (i + 1f) / (count + 1f)
                : Mathf.Clamp01(normalizedPathPositions[Mathf.Min(i, normalizedPathPositions.Length - 1)]);

            Vector3 p = Vector3.Lerp(start, end, t);
            p.y = start.y;
            GameObject inst = Instantiate(prefab, p, Quaternion.identity);
            spawnedInstances.Add(inst);

            if (inst.GetComponentInChildren<FlagEffectProvider>(true) == null)
            {
                UnityEngine.Debug.LogWarning($"MlTrainingFlagSeeder: prefab '{prefab.name}' has no FlagEffectProvider; it will not affect training.");
            }
        }
    }

    void OnDestroy()
    {
        foreach (GameObject go in spawnedInstances)
        {
            if (go != null)
            {
                Destroy(go);
            }
        }

        spawnedInstances.Clear();
    }
}
