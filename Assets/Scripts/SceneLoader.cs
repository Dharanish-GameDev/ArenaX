using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneName(string name)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(name);
    }

    public void LoadSceneIndex(int index)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(index);
    }

    public void ReloadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void LoadNext()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Quit() => Application.Quit();
}
