using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AdCounterDaily : MonoBehaviour
{
    public static AdCounterDaily Instance;

    [Header("Settings")]
    [SerializeField] private int maxAdClicksPerDay = 5;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.35f;
    [SerializeField] private float disabledAlpha = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private const string KEY_REMAINING = "AD_REMAINING";
    private const string KEY_LAST_DATE = "AD_LAST_DATE_YYYYMMDD";

    private int remaining;

    private AdCounterDailyUI ui;

    private Coroutine timerRoutine;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        LoadAndMaybeResetDaily();
        BindUIIfPresent();
        ForceApplyUIStateNow();

        if (timerRoutine == null)
            timerRoutine = StartCoroutine(UpdateResetTimer());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ✅ Always reload state on every scene
        LoadAndMaybeResetDaily();

        BindUIIfPresent();
        ForceApplyUIStateNow();
    }

    private void BindUIIfPresent()
    {
        var found = FindObjectOfType<AdCounterDailyUI>(true);

        if (found == null)
        {
            if (debugLogs) Debug.LogWarning("[AdCounterDaily] AdCounterDailyUI not found in this scene. (Expected on ads panel)");
            UnbindUI();
            return;
        }

        // same binder
        if (ui == found) return;

        UnbindUI();
        ui = found;

        if (ui.adButton == null)
        {
            if (debugLogs) Debug.LogWarning("[AdCounterDaily] UI binder found but adButton is NULL. Assign it in inspector.");
            return;
        }

        // Hook click (ensure nobody overwrites without us reapplying later)
        ui.adButton.onClick.RemoveListener(OnAdButtonClicked);
        ui.adButton.onClick.AddListener(OnAdButtonClicked);

        if (debugLogs) Debug.Log("[AdCounterDaily] Bound UI + click listener.");
    }

    private void UnbindUI()
    {
        if (ui != null && ui.adButton != null)
            ui.adButton.onClick.RemoveListener(OnAdButtonClicked);

        ui = null;

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }
    }

    private void ForceApplyUIStateNow()
    {
        UpdateUIInstant();
        StartCoroutine(ApplyUIEndOfFrame()); // beats scene-init scripts that re-enable button
    }

    private IEnumerator ApplyUIEndOfFrame()
    {
        yield return null;
        UpdateUIInstant();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        LoadAndMaybeResetDaily();
        ForceApplyUIStateNow();
    }

    private void LoadAndMaybeResetDaily()
    {
        int today = GetTodayYYYYMMDD();
        int savedDay = PlayerPrefs.GetInt(KEY_LAST_DATE, 0);

        if (savedDay != today)
        {
            remaining = maxAdClicksPerDay;
            PlayerPrefs.SetInt(KEY_REMAINING, remaining);
            PlayerPrefs.SetInt(KEY_LAST_DATE, today);
            PlayerPrefs.Save();

            if (debugLogs) Debug.Log($"[AdCounterDaily] New day/reset. remaining={remaining}");
        }
        else
        {
            remaining = PlayerPrefs.GetInt(KEY_REMAINING, maxAdClicksPerDay);
            remaining = Mathf.Clamp(remaining, 0, maxAdClicksPerDay);
        }
    }

    private void SaveState()
    {
        PlayerPrefs.SetInt(KEY_REMAINING, remaining);
        PlayerPrefs.SetInt(KEY_LAST_DATE, GetTodayYYYYMMDD());
        PlayerPrefs.Save();
    }

    private void OnAdButtonClicked()
    {
        // Safety: re-load in case any other script changed prefs
        LoadAndMaybeResetDaily();

        if (debugLogs) Debug.Log($"[AdCounterDaily] Click received. remaining(before)={remaining}");

        if (remaining <= 0)
        {
            ForceApplyUIStateNow();
            return;
        }

        remaining--;
        SaveState();

        ForceApplyUIStateNow();

        if (remaining <= 0)
            DisableAndFadeOut();
    }

    private Graphic GetFadeGraphic()
    {
        if (ui == null || ui.adButton == null) return null;

        // 1) If user assigned fadeGraphic, use it
        if (ui.fadeGraphic != null) return ui.fadeGraphic;

        // 2) Button graphic
        var g = ui.adButton.GetComponent<Graphic>();
        if (g != null) return g;

        // 3) First child graphic (common if visuals are nested)
        return ui.adButton.GetComponentInChildren<Graphic>(true);
    }

    private void SetGraphicAlpha(Graphic g, float a)
    {
        if (g == null) return;
        Color c = g.color;
        c.a = a;
        g.color = c;
    }

    private void UpdateUIInstant()
    {
        if (ui == null) return;

        if (ui.counterText != null)
            ui.counterText.text = $"x{remaining}";

        if (ui.adButton == null) return;

        bool enabled = remaining > 0;
        ui.adButton.interactable = enabled;

        var fadeG = GetFadeGraphic();
        SetGraphicAlpha(fadeG, enabled ? 1f : disabledAlpha);
    }

    private void DisableAndFadeOut()
    {
        if (ui == null || ui.adButton == null) return;

        ui.adButton.interactable = false;

        var fadeG = GetFadeGraphic();
        if (fadeG == null)
        {
            if (debugLogs) Debug.LogWarning("[AdCounterDaily] No fade graphic found. Assign 'fadeGraphic' in AdCounterDailyUI.");
            return;
        }

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeGraphic(fadeG, fadeG.color.a, disabledAlpha, fadeDuration));
    }

    private IEnumerator FadeGraphic(Graphic g, float from, float to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            p = p * p * (3f - 2f * p); // smoothstep

            SetGraphicAlpha(g, Mathf.Lerp(from, to, p));
            yield return null;
        }

        SetGraphicAlpha(g, to);
    }

    private IEnumerator UpdateResetTimer()
    {
        while (true)
        {
            var timeLeft = GetTimeUntilMidnight();

            if (ui != null && ui.resetTimerText != null)
            {
                ui.resetTimerText.text =
                    $"Resets in: {timeLeft.Hours:00}:{timeLeft.Minutes:00}:{timeLeft.Seconds:00}";
            }

            if (timeLeft.TotalSeconds <= 1)
            {
                LoadAndMaybeResetDaily();
                ForceApplyUIStateNow();
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private TimeSpan GetTimeUntilMidnight()
    {
        DateTime now = DateTime.Now;
        DateTime midnight = now.Date.AddDays(1);
        return midnight - now;
    }

    private int GetTodayYYYYMMDD()
    {
        DateTime now = DateTime.Now;
        return now.Year * 10000 + now.Month * 100 + now.Day;
    }

    public void ForceResetNow()
    {
        remaining = maxAdClicksPerDay;
        SaveState();
        ForceApplyUIStateNow();
    }
}