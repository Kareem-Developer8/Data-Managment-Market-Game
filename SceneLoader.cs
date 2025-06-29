using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 1) // Game scene
        {
            StartCoroutine(LoadGameData());
        }
    }

    IEnumerator LoadGameData()
    {
        // Load boxes first
        FindFirstObjectByType<BoxManager>()?.LoadSavedBoxes();
        yield return new WaitForEndOfFrame();

        // Wait for PlaceholderManager initialization
        while (PlaceholderManager.Instance == null)
        {
            yield return null;
        }

        // Load placeholders
        var placeholderData = DataManager.Instance.LoadPlaceholders();
        PlaceholderManager.Instance.LoadSavedPlaceholders(placeholderData);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}