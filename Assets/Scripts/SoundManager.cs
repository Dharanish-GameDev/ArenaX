using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] private AudioSource audioSource;

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

        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    public void SetVolume(float v)
    {
        if (audioSource) audioSource.volume = v;
    }

    public void StopMusic()
    {
        if (audioSource) audioSource.Stop();
    }

    public void PlayMusic()
    {
        if (audioSource && !audioSource.isPlaying) audioSource.Play();
    }
}
