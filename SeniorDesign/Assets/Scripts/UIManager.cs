using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject buildUI;
    public GameObject simulationUI;

    private void Start()
    {
        if (buildUI == null || simulationUI == null)
        {
            UnityEngine.Debug.LogWarning("UIManager: buildUI and/or simulationUI are not assigned.");
        }
    }

    void Update()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        if (buildUI == null || simulationUI == null)
        {
            return;
        }

        if (GameManager.Instance.CurrentPhase == GamePhase.Build)
        {
            buildUI.SetActive(true);
            simulationUI.SetActive(false);
        }
        else if (GameManager.Instance.CurrentPhase == GamePhase.Simulation)
        {
            buildUI.SetActive(false);
            simulationUI.SetActive(true);
        }
    }
}