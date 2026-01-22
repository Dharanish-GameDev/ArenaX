using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneName(string name) => SceneManager.LoadScene(name);
    public void LoadSceneIndex(int index) => SceneManager.LoadScene(index);
    public void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    public void LoadNext() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    public void Quit() => Application.Quit();
}