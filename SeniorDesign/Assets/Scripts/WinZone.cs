using System;
using System.Collections.Generic;
using UnityEngine;

public class WinZone : MonoBehaviour
{
    public ActorSpawner actorSpawner; // Reference in Inspector

    public event Action LevelCompleted;

    private HashSet<GameObject> actorsInZone = new HashSet<GameObject>();
    private bool levelComplete;

    void Awake()
    {
        if (actorSpawner == null)
        {
            actorSpawner = FindFirstObjectByType<ActorSpawner>();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            actorsInZone.Add(other.gameObject);
            CheckWinCondition();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Agent"))
        {
            actorsInZone.Remove(other.gameObject);
        }
    }

    void CheckWinCondition()
    {
        if (actorSpawner == null)
        {
            actorSpawner = FindFirstObjectByType<ActorSpawner>();
            if (actorSpawner == null)
            {
                UnityEngine.Debug.LogWarning("WinZone: actorSpawner reference is not assigned.");
                return;
            }
        }

        if (actorsInZone.Count == actorSpawner.actorCount)
        {
            UnityEngine.Debug.Log("Level Complete!");
            OnWin();
        }
    }

    void OnWin()
    {
        if (levelComplete)
        {
            return;
        }

        levelComplete = true;
        UnityEngine.Debug.Log("Level complete!");
        LevelCompleted?.Invoke();
    }
}