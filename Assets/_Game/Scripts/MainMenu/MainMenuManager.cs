
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MainMenuManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("Drag Gameplay Scene Here")]
    [SerializeField] private SceneAsset gameplayScene;
#endif

    [SerializeField, HideInInspector]
    private string gameplayScenePath;

    [Header("Loading UI (Optional)")]
    [SerializeField] private GameObject loadingPanel;

    private bool isLoading = false;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (gameplayScene != null)
        {
            gameplayScenePath =
                AssetDatabase.GetAssetPath(gameplayScene);
        }
        else
        {
            gameplayScenePath = "";
        }
    }
#endif

    private void Awake()
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(false);
    }

    public void Play()
    {
        if (isLoading) return;

        int sceneIndex =
            SceneUtility.GetBuildIndexByScenePath(
                gameplayScenePath
            );

        if (sceneIndex < 0)
        {
            Debug.LogError(
                "Gameplay Scene not found in Build Profiles!"
            );
            return;
        }

        StartCoroutine(LoadGame(sceneIndex));
    }

    private IEnumerator LoadGame(int sceneIndex)
    {
        isLoading = true;

        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        yield return null;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneIndex);

        while (!operation.isDone)
        {
            yield return null;
        }
    }

    public void Exit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
