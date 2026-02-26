using UnityEngine;

public class SoundToggleBinder : MonoBehaviour
{
    [SerializeField] private SimpleToggleSwitch toggleSwitch;

    private void Start()
    {
        if (!UIButtonSound.Instance || !toggleSwitch)
            return;

        toggleSwitch.SetState(UIButtonSound.Instance.IsSoundOn, false);
        toggleSwitch.OnToggleChanged += OnToggleChanged;
    }

    private void OnToggleChanged(bool isOn)
    {
        UIButtonSound.Instance.SetSoundEnabled(isOn);
    }

    private void OnDestroy()
    {
        if (toggleSwitch != null)
            toggleSwitch.OnToggleChanged -= OnToggleChanged;
    }
}