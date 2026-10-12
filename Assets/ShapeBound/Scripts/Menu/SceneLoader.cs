using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneByIndex(int buildIndex)
    {
        SceneManager.LoadSceneAsync(buildIndex);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}