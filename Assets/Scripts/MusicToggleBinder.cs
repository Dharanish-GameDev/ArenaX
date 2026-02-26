using UnityEngine;

public class MusicToggleBinder : MonoBehaviour
{
    [SerializeField] private SimpleToggleSwitch toggleSwitch;

    private void Start()
    {
        if (!MusicManager.Instance || !toggleSwitch)
            return;

        // Set initial UI state from saved preference
        toggleSwitch.SetState(MusicManager.Instance.IsMusicOn, false);

        // Bind toggle callback
        toggleSwitch.OnToggleChanged += OnToggleChanged;
    }

    private void OnToggleChanged(bool isOn)
    {
        MusicManager.Instance.SetMusicEnabled(isOn);
    }

    private void OnDestroy()
    {
        if (toggleSwitch != null)
            toggleSwitch.OnToggleChanged -= OnToggleChanged;
    }
}