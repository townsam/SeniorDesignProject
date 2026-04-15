using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class ActorSpawner : MonoBehaviour
{
    [Header("Actor Settings")]
    public GameObject actorPrefab;
    public int actorCount = 1;

    [Header("Target Settings")]
    public Transform defaultTarget;

    [Header("Spawn Settings")]
    public Vector3 spawnPoint = Vector3.zero;
    [Tooltip("Extra space along X between actors. Use 0 so everyone spawns at spawnPoint; increase slightly if rigidbodies overlap badly.")]
    public float spawnSlotSpacing = 0f;

    [Header("Episode Settings")]
    public float episodeLength = 10f;
    private float nextResetTime;

    [Header("Debug Options")]
    public bool showSpawnPoint = true;
    public bool randomizeSeed = true;

    private List<GameObject> spawnedActors = new List<GameObject>();
    private List<int> actorSeeds = new List<int>();

    private bool simulationRunning = false;
    private bool episodeLoopStopped;

    public void StopEpisodeLoop()
    {
        episodeLoopStopped = true;
    }

    /// <summary>
    /// Return to build phase: stop sim loop timing, hide actors, reset poses for the next run.
    /// </summary>
    public void EnterBuildPhaseFromSimulation()
    {
        simulationRunning = false;
        episodeLoopStopped = false;
        DisableActors();
        ResetEnvironment();
    }

    void Start()
    {
        if (actorPrefab == null)
        {
            UnityEngine.Debug.LogError("ActorSpawner: No actor prefab assigned!");
            return;
        }

        if (defaultTarget == null)
        {
            ResolveDefaultTargetFromWinZone();
        }

        SpawnAllActors();
        DisableActors(); // start hidden during build mode
    }

    private void ResolveDefaultTargetFromWinZone()
    {
        WinZone[] zones = FindObjectsByType<WinZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (zones == null || zones.Length == 0)
        {
            UnityEngine.Debug.LogWarning("ActorSpawner: No defaultTarget assigned and no WinZone found. ActorAgent targets will remain unset.");
            return;
        }

        if (zones.Length > 1)
        {
            UnityEngine.Debug.LogWarning($"ActorSpawner: Found {zones.Length} WinZones in scene. Using the first one found: '{zones[0].name}'. Assign defaultTarget explicitly to avoid ambiguity.");
        }

        defaultTarget = zones[0].transform;
        UnityEngine.Debug.Log($"ActorSpawner: defaultTarget resolved to WinZone '{defaultTarget.name}' at {defaultTarget.position}.");
    }

    void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (episodeLoopStopped)
        {
            return;
        }

        if (!simulationRunning)
        {
            if (GameManager.Instance.CurrentPhase == GamePhase.Simulation)
            {
                BeginSimulation();
            }
            return;
        }

        if (Time.time >= nextResetTime)
        {
            ResetEnvironment();
            ScheduleNextReset();
        }
    }

    void SpawnAllActors()
    {
        spawnedActors.Clear();
        actorSeeds.Clear();

        for (int i = 0; i < actorCount; i++)
        {
            Vector3 offset = GetSpawnOffset(i);
            GameObject actor = Instantiate(actorPrefab, spawnPoint + offset, Quaternion.identity, transform);

            int seed = randomizeSeed ? UnityEngine.Random.Range(0, 999999) : 0;
            actorSeeds.Add(seed);

            if (actor.TryGetComponent(out ActorBehavior behavior))
            {
                behavior.InitializeWithSeed(seed);
            }

            if (actor.TryGetComponent(out ActorAgent agent))
            {
                agent.target = defaultTarget;
                if (defaultTarget != null)
                {
                    UnityEngine.Debug.Log($"ActorSpawner: Set {actor.name}.ActorAgent.target -> '{defaultTarget.name}'.");
                }
            }

            ApplyColorFromSeed(actor, seed);
            spawnedActors.Add(actor);
        }

        UnityEngine.Debug.Log($"Spawned {spawnedActors.Count} actors.");
    }

    void DisableActors()
    {
        foreach (var actor in spawnedActors)
        {
            actor.SetActive(false);
        }
    }

    void EnableActors()
    {
        foreach (var actor in spawnedActors)
        {
            actor.SetActive(true);
        }
    }

    void BeginSimulation()
    {
        simulationRunning = true;

        EnableActors();
        ResetEnvironment();
        ScheduleNextReset();

        UnityEngine.Debug.Log("Simulation started.");
    }

    void ScheduleNextReset()
    {
        nextResetTime = Time.time + episodeLength;
    }

    void ApplyColorFromSeed(GameObject actor, int seed)
    {
        UnityEngine.Random.InitState(seed);

        Color randomColor = new Color(
            UnityEngine.Random.value,
            UnityEngine.Random.value,
            UnityEngine.Random.value
        );

        Renderer rend = actor.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.material = new Material(rend.material);
            rend.material.color = randomColor;
        }
    }

    public void ResetEnvironment()
    {
        for (int i = 0; i < spawnedActors.Count; i++)
        {
            var actor = spawnedActors[i];
            if (actor == null) continue;

            Rigidbody rb = actor.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            actor.transform.position = spawnPoint + GetSpawnOffset(i);
            actor.transform.rotation = Quaternion.identity;

            int seed = actorSeeds[i];

            if (actor.TryGetComponent(out ActorBehavior behavior))
            {
                behavior.InitializeWithSeed(seed);
                ApplyColorFromSeed(actor, seed);
                behavior.ResetSteps();
            }

            if (actor.TryGetComponent(out ActorAgent agent))
            {
                agent.ForceResetActor();
            }
        }

        UnityEngine.Debug.Log($"Environment reset at time {Time.time:F2}");
    }

    void OnDrawGizmosSelected()
    {
        if (!showSpawnPoint) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(spawnPoint, 0.5f);
        Gizmos.DrawLine(spawnPoint, spawnPoint + Vector3.up * 2f);
    }

    private Vector3 GetSpawnOffset(int index)
    {
        return new Vector3(index * spawnSlotSpacing, 0f, 0f);
    }
}