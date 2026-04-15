using UnityEngine;

public static class LevelCatalog
{
    public static readonly string[] SceneNames = { "Level01", "Level02", "SampleScene" };

    public static readonly string[] DisplayNames = { "Level 1", "Level 2", "Sample Scene" };

    public static int GetLevelIndex(string sceneName)
    {
        for (int i = 0; i < SceneNames.Length; i++)
        {
            if (SceneNames[i] == sceneName)
            {
                return i;
            }
        }

        return 0;
    }

    public static bool TryGetNextScene(int clearedIndex, out string nextScene)
    {
        int next = clearedIndex + 1;
        if (next >= 0 && next < SceneNames.Length)
        {
            nextScene = SceneNames[next];
            return true;
        }

        nextScene = null;
        return false;
    }
}

public static class LevelProgress
{
    private const string MaxUnlockedKey = "level_max_unlocked_index";

    public static int MaxUnlockedIndex
    {
        get => PlayerPrefs.GetInt(MaxUnlockedKey, 0);
        private set
        {
            PlayerPrefs.SetInt(MaxUnlockedKey, value);
            PlayerPrefs.Save();
        }
    }

    public static bool IsUnlocked(int levelIndex)
    {
        return levelIndex >= 0 && levelIndex <= MaxUnlockedIndex;
    }

    public static void RegisterLevelCleared(int clearedLevelIndex)
    {
        int cap = LevelCatalog.SceneNames.Length - 1;
        int next = Mathf.Max(MaxUnlockedIndex, clearedLevelIndex + 1);
        next = Mathf.Min(next, cap);
        MaxUnlockedIndex = next;
    }

    public static void ResetAllProgress()
    {
        PlayerPrefs.DeleteKey(MaxUnlockedKey);
        PlayerPrefs.Save();
    }

    public static void UnlockAllLevels()
    {
        int cap = Mathf.Max(0, LevelCatalog.SceneNames.Length - 1);
        MaxUnlockedIndex = cap;
    }
}
