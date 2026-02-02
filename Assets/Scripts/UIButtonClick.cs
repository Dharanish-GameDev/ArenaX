using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIButtonSound : MonoBehaviour
{
    public static UIButtonSound Instance;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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
            btn.onClick.RemoveListener(PlayClick); // prevent duplicates
            btn.onClick.AddListener(PlayClick);
        }
    }

    public void PlayClick()
    {
        if (audioSource != null && clickSound != null)
            audioSource.PlayOneShot(clickSound);
    }
}
