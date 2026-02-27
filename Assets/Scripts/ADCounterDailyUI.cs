using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AdCounterDailyUI : MonoBehaviour
{
    [Header("UI References")]
    public Button adButton;
    public TextMeshProUGUI counterText;
    public TextMeshProUGUI resetTimerText;

    [Header("Fade Target (IMPORTANT)")]
    [Tooltip("Assign the exact graphic you want to fade (Button Image or a child Image). If empty, script will auto-pick.")]
    public Graphic fadeGraphic;

    private void Reset()
    {
        if (adButton == null) adButton = GetComponentInChildren<Button>(true);
        if (counterText == null) counterText = GetComponentInChildren<TextMeshProUGUI>(true);
        if (fadeGraphic == null && adButton != null)
            fadeGraphic = adButton.GetComponent<Graphic>();
    }
}