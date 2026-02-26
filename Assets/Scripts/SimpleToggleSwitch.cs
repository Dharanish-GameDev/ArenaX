using UnityEngine;
using UnityEngine.UI;
using System;

public class SimpleToggleSwitch : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button toggleButton;
    [SerializeField] private GameObject onVisual;
    [SerializeField] private GameObject offVisual;

    public bool IsOn { get; private set; }

    public Action<bool> OnToggleChanged;

    private void Awake()
    {
        toggleButton.onClick.AddListener(Toggle);
        UpdateVisual();
    }

    public void Toggle()
    {
        SetState(!IsOn);
    }

    public void SetState(bool state, bool notify = true)
    {
        IsOn = state;
        UpdateVisual();

        if (notify)
            OnToggleChanged?.Invoke(IsOn);
    }

    private void UpdateVisual()
    {
        if (onVisual) onVisual.SetActive(IsOn);
        if (offVisual) offVisual.SetActive(!IsOn);
    }
}