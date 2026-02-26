using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    private const string MUSIC_PREF_KEY = "MusicEnabled";

    [SerializeField] private AudioSource audioSource;

    public bool IsMusicOn { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        LoadMusicState();
    }

    private void LoadMusicState()
    {
        IsMusicOn = PlayerPrefs.GetInt(MUSIC_PREF_KEY, 1) == 1;
        ApplyMusicState();
    }

    private void ApplyMusicState()
    {
        if (!audioSource) return;

        audioSource.volume = 1f;

        if (IsMusicOn)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();
        }
        else
        {
            audioSource.Stop();
        }
    }

    public void SetMusicEnabled(bool enabled)
    {
        IsMusicOn = enabled;

        PlayerPrefs.SetInt(MUSIC_PREF_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();

        ApplyMusicState();
    }

    public void ToggleMusic()
    {
        SetMusicEnabled(!IsMusicOn);
    }

    public void SetVolume(float v)
    {
        if (audioSource)
            audioSource.volume = v;
    }

    public void StopMusic()
    {
        if (audioSource)
            audioSource.Stop();
    }

    public void PlayMusic()
    {
        if (audioSource && IsMusicOn && !audioSource.isPlaying)
            audioSource.Play();
    }
}