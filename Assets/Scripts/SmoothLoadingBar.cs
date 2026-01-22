using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class SmoothLoadingBar : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image loadingBarFill;
    [SerializeField] private TextMeshProUGUI loadingText;
    
    [Header("Loading Settings")]
    [SerializeField] private float loadingDuration = 3f;
    
    [Header("Events")]
    [SerializeField] private UnityEvent onLoadingComplete;

    private float currentProgress = 0f;
    private float elapsedTime = 0f;
    private bool isLoading = false;
    private bool isComplete = false;

    void Start()
    {
        StartLoading();
    }

    void Update()
    {
        if (!isLoading || isComplete) return;
        
        elapsedTime += Time.deltaTime;
        
        // Direct linear progress - matches exact duration
        currentProgress = Mathf.Clamp01(elapsedTime / loadingDuration);
        
        // Update UI
        loadingBarFill.fillAmount = currentProgress;
        loadingText.text = $"{(currentProgress * 100):F0}%";
        
        // Check if loading is complete
        if (currentProgress >= 0.999f && !isComplete)
        {
            CompleteLoading();
        }
    }

    public void StartLoading()
    {
        ResetLoadingBar();
        isLoading = true;
        isComplete = false;
    }

    // For real loading operations
    public void UpdateProgress(float progress)
    {
        currentProgress = Mathf.Clamp01(progress);
        isLoading = true;
    }

    private void CompleteLoading()
    {
        isComplete = true;
        isLoading = false;
        
        // Ensure 100% display
        currentProgress = 1f;
        loadingBarFill.fillAmount = 1f;
        loadingText.text = "100%";
        
        // Invoke the OnComplete event
        onLoadingComplete?.Invoke();
        
        Debug.Log($"Loading Complete! Time taken: {elapsedTime:F2}s (Target: {loadingDuration}s)");
    }

    // Reset the loading bar to start fresh
    public void ResetLoadingBar()
    {
        currentProgress = 0f;
        elapsedTime = 0f;
        isLoading = false;
        isComplete = false;
        loadingBarFill.fillAmount = 0f;
        loadingText.text = "0%";
    }

    // Check if loading is complete
    public bool IsLoadingComplete()
    {
        return isComplete;
    }
}