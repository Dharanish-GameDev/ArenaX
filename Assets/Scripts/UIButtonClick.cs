using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIButtonSound : MonoBehaviour
{
    public static UIButtonSound Instance;

    private const string SOUND_PREF_KEY = "SoundEnabled";

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    public bool IsSoundOn { get; private set; }

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

        LoadSoundState();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void LoadSoundState()
    {
        IsSoundOn = PlayerPrefs.GetInt(SOUND_PREF_KEY, 1) == 1;
    }

    public void SetSoundEnabled(bool enabled)
    {
        IsSoundOn = enabled;

        PlayerPrefs.SetInt(SOUND_PREF_KEY, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddClickSoundToAllButtons();
    }

    private void Start()
    {
        AddClickSoundToAllButtons();
    }

    private void AddClickSoundToAllButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);

        foreach (Button btn in buttons)
        {
            btn.onClick.RemoveListener(PlayClick);
            btn.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (!IsSoundOn) return;

        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
}