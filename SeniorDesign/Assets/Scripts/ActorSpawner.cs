using UnityEngine;
using System.Collections.Generic;

public class ActorSpawner : MonoBehaviour
{
    [Header("Actor Settings")]
    public GameObject actorPrefab;
    public int actorCount = 100;

    [Header("Spawn Settings")]
    public Vector3 spawnPoint = Vector3.zero;

    [Header("Episode Settings")]
    public float episodeLength = 10f;
    private float nextResetTime;

    [Header("Debug Options")]
    public bool showSpawnPoint = true;
    public bool randomizeSeed = true;

    private List<GameObject> spawnedActors = new List<GameObject>();
    private List<int> actorSeeds = new List<int>(); // persistent seeds per actor

    void Start()
    {
        if (actorPrefab == null)
        {
            UnityEngine.Debug.LogError("ActorSpawner: No actor prefab assigned!");
            return;
        }

        SpawnAllActors();
        ScheduleNextReset();
    }

    void Update()
    {
        if (Time.time >= nextResetTime)
        {
            ResetEnvironment();
            ScheduleNextReset();
        }
    }

    void ScheduleNextReset()
    {
        nextResetTime = Time.time + episodeLength;
    }

    void SpawnAllActors()
    {
        spawnedActors.Clear();
        actorSeeds.Clear();

        for (int i = 0; i < actorCount; i++)
        {
            Quaternion rotation = Quaternion.identity;
            GameObject actor = Instantiate(actorPrefab, spawnPoint, rotation, transform);

            // Generate persistent seed
            int seed = randomizeSeed ? UnityEngine.Random.Range(0, 999999) : 0;
            actorSeeds.Add(seed);

            if (actor.TryGetComponent(out ActorBehavior behavior))
            {
                behavior.InitializeWithSeed(seed);
            }

            ApplyColorFromSeed(actor, seed);
            spawnedActors.Add(actor);
        }

        UnityEngine.Debug.Log($"Spawned {spawnedActors.Count} actors at the same point.");
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
            rend.material = new Material(rend.material); // unique material per actor
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

            actor.transform.position = spawnPoint;
            actor.transform.rotation = Quaternion.identity;

            int seed = actorSeeds[i]; // use persistent seed
            if (actor.TryGetComponent(out ActorBehavior behavior))
            {
                behavior.InitializeWithSeed(seed);
                ApplyColorFromSeed(actor, seed);
                behavior.ResetSteps();
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
}
