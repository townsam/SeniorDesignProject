using System.Diagnostics;
using UnityEngine;

public enum GamePhase
{
    Build,
    Simulation
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GamePhase CurrentPhase;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            UnityEngine.Debug.LogWarning("GameManager: Duplicate instance detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (GetComponent<LevelFlowController>() == null)
        {
            gameObject.AddComponent<LevelFlowController>();
        }

        if (GetComponent<GameSfx>() == null)
        {
            gameObject.AddComponent<GameSfx>();
        }
    }

    void Start()
    {
        SetPhase(GamePhase.Build);
    }

    public void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;

        switch (phase)
        {
            case GamePhase.Build:
                UnityEngine.Debug.Log("Build Phase");
                break;

            case GamePhase.Simulation:
                UnityEngine.Debug.Log("Simulation Phase");
                break;
        }
    }

    public void StartSimulation()
    {
        SetPhase(GamePhase.Simulation);
    }

    public void StartBuild()
    {
        SetPhase(GamePhase.Build);
    }
}