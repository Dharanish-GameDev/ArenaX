using UnityEngine;
using UnityEngine.SceneManagement;

public enum PlayMode : byte
{
    VsAI = 0,
    PassAndPlay = 1,
    Multiplayer = 2
}

public static class GameModeSelection
{
    public const string PREF_KEY = "PLAY_MODE";

    public static PlayMode SelectedMode
    {
        get => (PlayMode)PlayerPrefs.GetInt(PREF_KEY, (int)PlayMode.Multiplayer);
        set { PlayerPrefs.SetInt(PREF_KEY, (int)value); PlayerPrefs.Save(); }
    }
}

public class GameModeSelectionUI : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplay"; // change if needed

    public void OnVsAIPressed()
    {
        GameModeSelection.SelectedMode = PlayMode.VsAI;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnPassAndPlayPressed()
    {
        GameModeSelection.SelectedMode = PlayMode.PassAndPlay;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void OnMultiplayerPressed()
    {
        GameModeSelection.SelectedMode = PlayMode.Multiplayer;
        SceneManager.LoadScene(gameplaySceneName);
    }
}
