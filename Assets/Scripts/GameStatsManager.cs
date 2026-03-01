using System;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Arena.API.Models;
using TMPro;

public class GameStatsManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI totalGamesText;
    [SerializeField] private TextMeshProUGUI winsText;
    [SerializeField] private TextMeshProUGUI lossesText;
    [SerializeField] private TextMeshProUGUI drawsText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI fastestWinText;
    [SerializeField] private TextMeshProUGUI fastestLossText;
    
    
    [Header("Loading")]
    [SerializeField] private GameObject loadingIndicator;
    
    private GameStatsResponse currentStats;

    public void FetchGameStats()
    {
        // Show loading if assigned
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);

        ApiManager.Instance.SendRequest<GameStatsResponse>(
            ApiEndPoints.User.GetUserStats,
            RequestMethod.GET,
            (response) =>
            {
                // Hide loading
                if (loadingIndicator != null)
                    loadingIndicator.SetActive(false);
                
                // Store stats and update UI
                currentStats = response;
                UpdateUI();
                
                Debug.Log($"[GameStats] Fetched successfully: {response.gameWon} wins, {response.gameLost} losses");
            },
            (error) =>
            {
                // Hide loading
                if (loadingIndicator != null)
                    loadingIndicator.SetActive(false);
                
                Debug.LogError($"[GameStats] Failed to fetch: {error}");
                
                // Optional: Show error message to user
                // ShowErrorMessage("Failed to load game stats");
            }
        );
    }

    private void UpdateUI()
    {
        if (currentStats == null)
        {
            Debug.LogWarning("[GameStats] No stats to display");
            return;
        }

        // Calculate total games (wins + losses + draws)
        int totalGames = currentStats.gameWon + currentStats.gameLost + currentStats.gameDraw;
        
        // Update UI texts with null checks
        if (totalGamesText != null)
            totalGamesText.text = totalGames.ToString();
        
        if (winsText != null)
            winsText.text = currentStats.gameWon.ToString();
        
        if (lossesText != null)
            lossesText.text = currentStats.gameLost.ToString();
        
        if (drawsText != null)
            drawsText.text = currentStats.gameDraw.ToString();
        
        if (killsText != null)
            killsText.text = currentStats.kills.ToString();
        
        // Format times nicely
        if (fastestWinText != null)
            fastestWinText.text = FormatTime(currentStats.fastestGameWon);
        
        if (fastestLossText != null)
            fastestLossText.text = FormatTime(currentStats.fastestGameLost);
        
        Debug.Log($"[GameStats] UI Updated - Total: {totalGames}, Win Rate: {GetWinRate():F1}%");
    }

    private string FormatTime(int seconds)
    {
        if (seconds <= 0) return "N/A";
        
        TimeSpan time = TimeSpan.FromSeconds(seconds);
        
        if (time.Hours > 0)
            return $"{time.Hours}h {time.Minutes}m";
        else if (time.Minutes > 0)
            return $"{time.Minutes}m {time.Seconds}s";
        else
            return $"{time.Seconds}s";
    }

    // Public methods to access stats
    public int GetWins() => currentStats?.gameWon ?? 0;
    public int GetLosses() => currentStats?.gameLost ?? 0;
    public int GetDraws() => currentStats?.gameDraw ?? 0;
    public int GetTotalGames() => (currentStats?.gameWon ?? 0) + (currentStats?.gameLost ?? 0) + (currentStats?.gameDraw ?? 0);
    public int GetKills() => currentStats?.kills ?? 0;
    
    public float GetWinRate()
    {
        int total = GetTotalGames();
        return total > 0 ? (float)GetWins() / total * 100 : 0;
    }
    
    public int GetFastestWin() => currentStats?.fastestGameWon ?? 0;
    public int GetFastestLoss() => currentStats?.fastestGameLost ?? 0;
    
    // Refresh stats manually
    public void RefreshStats()
    {
        FetchGameStats();
    }
}