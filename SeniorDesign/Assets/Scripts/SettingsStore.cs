using UnityEngine;

public static class SettingsStore
{
    private const string MasterVolumeKey = "settings_master_volume";

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        set
        {
            float v = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(MasterVolumeKey, v);
            PlayerPrefs.Save();
            ApplyToAudioListener();
        }
    }

    public static void ApplyToAudioListener()
    {
        AudioListener.volume = MasterVolume;
    }
}
