using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

// หน้าเมนูหลัก ปุ่ม Play = โหลดซีน Gameplay / ปุ่ม Exit = ออกเกม
// ปุ่มบนหน้าจอเรียก Play() กับ Exit() ผ่าน OnClick ใน Inspector
public class MainMenuManager : MonoBehaviour
{
#if UNITY_EDITOR
    [Header("ลากซีน Gameplay มาใส่")]
    [SerializeField] private SceneAsset gameplayScene;
#endif

    // path ของซีน (เก็บเป็นข้อความ ตอน Build เกมจริงไม่มี SceneAsset)
    [SerializeField, HideInInspector] private string gameplayScenePath;

    [Header("หน้า Loading (ไม่ใส่ก็ได้)")]
    [SerializeField] private GameObject loadingPanel;

    private bool isLoading;

#if UNITY_EDITOR
    // ลากซีนใส่ใน Inspector แล้วจำ path ไว้ให้เอง
    private void OnValidate()
    {
        gameplayScenePath = gameplayScene != null ? AssetDatabase.GetAssetPath(gameplayScene) : "";
    }
#endif

    private void Awake()
    {
        if (loadingPanel != null) loadingPanel.SetActive(false);
    }

    public void Play()
    {
        if (isLoading) return;

        // 1. หาลำดับซีนใน Build Settings
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(gameplayScenePath);
        if (sceneIndex < 0)
        {
            Debug.LogError("ไม่พบซีน Gameplay ใน Build Settings");
            return;
        }

        // 2. โหลดซีนเบื้องหลัง
        StartCoroutine(LoadGame(sceneIndex));
    }

    private IEnumerator LoadGame(int sceneIndex)
    {
        isLoading = true;
        if (loadingPanel != null) loadingPanel.SetActive(true);
        yield return null; // รอ 1 เฟรมให้หน้า Loading ขึ้นก่อน

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneIndex);
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
