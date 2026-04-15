using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayMenu : MonoBehaviour
{
    private void OnEnable()
    {
        RefreshLevelButtons();
    }

    public void RefreshLevelButtons()
    {
        ApplyLevelGate(0, "Level1Button");
        ApplyLevelGate(1, "Level2Button");
        ApplyLevelGate(2, "Level3Button");
    }

    private static void ApplyLevelGate(int slot, string objectName)
    {
        GameObject go = GameObject.Find(objectName);
        if (go == null)
        {
            return;
        }

        Button btn = go.GetComponent<Button>();
        if (btn != null)
        {
            btn.interactable = LevelProgress.IsUnlocked(slot);
        }

        if (slot < 0 || slot >= LevelCatalog.DisplayNames.Length)
        {
            return;
        }

        string title = LevelCatalog.DisplayNames[slot];
        Text label = go.GetComponentInChildren<Text>();
        if (label != null)
        {
            label.text = LevelProgress.IsUnlocked(slot) ? title : $"{title} (locked)";
        }
    }

    public void LoadLevelOne()
    {
        TryLoadSlot(0);
    }

    public void LoadLevelTwo()
    {
        TryLoadSlot(1);
    }

    public void LoadLevelThree()
    {
        TryLoadSlot(2);
    }

    private static void TryLoadSlot(int slot)
    {
        if (slot < 0 || slot >= LevelCatalog.SceneNames.Length)
        {
            return;
        }

        if (!LevelProgress.IsUnlocked(slot))
        {
            string name = slot >= 0 && slot < LevelCatalog.DisplayNames.Length
                ? LevelCatalog.DisplayNames[slot]
                : $"Level {slot + 1}";
            UnityEngine.Debug.Log($"PlayMenu: \"{name}\" is locked. Clear the previous level to unlock.");
            return;
        }

        SceneManager.LoadScene(LevelCatalog.SceneNames[slot]);
    }
}
