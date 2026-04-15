using UnityEngine;

public class GameSfx : MonoBehaviour
{
    public static GameSfx Instance { get; private set; }

    [Header("Optional clips (assign in Inspector for sound)")]
    public AudioClip winClip;
    public AudioClip loseClip;
    public AudioClip uiClickClip;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayWin()
    {
        PlayOne(winClip);
    }

    public void PlayLose()
    {
        PlayOne(loseClip);
    }

    public void PlayClick()
    {
        PlayOne(uiClickClip);
    }

    private void PlayOne(AudioClip clip)
    {
        if (clip == null || audioSource == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}
