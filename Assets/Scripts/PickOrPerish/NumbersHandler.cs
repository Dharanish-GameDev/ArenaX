using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NumbersHandler : MonoBehaviour
{
    [SerializeField] private NumberButton buttonRef;

    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private RectTransform firstButtonContainer;

    private int currentValue = -1;

    [SerializeField] private int maxValue = 100;

    private List<NumberButton> numberButtons = new List<NumberButton>();

    [SerializeField] private Button submitButton;

    // ✅ Assign this in Inspector: the whole panel/object that contains number buttons + submit
    [Header("UI Root (Hide when eliminated)")]
    [SerializeField] private GameObject numbersUIRoot;

    public int CurrentValue => currentValue;

    private static NumbersHandler instance;

    public static NumbersHandler Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<NumbersHandler>(FindObjectsInactive.Include);

            if (instance == null)
                Debug.LogError("NumbersHandler instance not initialized! Make sure it's in the scene.");

            return instance;
        }
    }

    private bool inputLocked = false;

    private void Awake()
    {
        InitializeSingleton();
        PopulateButtons();
    }

    private void InitializeSingleton()
    {
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"Multiple NumbersHandler instances found. Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void PopulateButtons()
    {
        numberButtons.Clear();

        // 0 button
        NumberButton zeroButton = Instantiate(buttonRef, firstButtonContainer);
        if (zeroButton != null)
        {
            numberButtons.Add(zeroButton);
            zeroButton.transform.localPosition = Vector3.zero;
            zeroButton.ConfigureButton(0, () => OnNumberClicked(zeroButton));
        }

        // 1..max buttons
        for (int i = 1; i <= maxValue; i++)
        {
            NumberButton button = Instantiate(buttonRef, buttonContainer);
            numberButtons.Add(button);

            NumberButton captured = button;
            captured.ConfigureButton(i, () => OnNumberClicked(captured));
        }
    }

    private void OnNumberClicked(NumberButton button)
    {
        if (inputLocked) return;

        currentValue = button.Number;
        SelectButton(button);
    }

    private void SelectButton(NumberButton button)
    {
        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (numberButtons[i] == button) numberButtons[i].Select();
            else numberButtons[i].Deselect();
        }
    }

    public void ResetValues()
    {
        currentValue = -1;

        for (int i = 0; i < numberButtons.Count; i++)
            numberButtons[i].Deselect();

        if (submitButton != null)
            submitButton.interactable = !inputLocked;
    }

    // ✅ THIS is what PlayerUISet is calling now
    public void SetInputVisibleAndInteractable(bool visible, bool resetSelection = false)
    {
        inputLocked = !visible;

        // Hide entire UI
        if (numbersUIRoot != null)
            numbersUIRoot.SetActive(visible);

        // Extra safety: disable submit + buttons
        if (submitButton != null)
            submitButton.interactable = visible;

        for (int i = 0; i < numberButtons.Count; i++)
        {
            var uiBtn = numberButtons[i].GetComponent<Button>();
            if (uiBtn != null)
                uiBtn.interactable = visible;
        }

        if (resetSelection)
        {
            currentValue = -1;
            for (int i = 0; i < numberButtons.Count; i++)
                numberButtons[i].Deselect();
        }
    }

    // Keep old name too (optional) so other scripts won't break
    public void SetInputInteractable(bool interactable, bool resetSelection = false)
    {
        SetInputVisibleAndInteractable(interactable, resetSelection);
    }

    public void ClearSubmitButtonEvents()
    {
        if (submitButton != null)
            submitButton.onClick.RemoveAllListeners();
    }

    public void SetSubmitButtonEvent(Action onSubmitButtonClicked)
    {
        if (submitButton == null) return;

        submitButton.onClick.RemoveAllListeners();

        submitButton.onClick.AddListener(() =>
        {
            if (inputLocked) return;

            onSubmitButtonClicked?.Invoke();
            submitButton.interactable = false; // your original behavior
        });
    }
}
