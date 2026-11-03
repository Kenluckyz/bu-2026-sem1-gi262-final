using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// เมนู BoatVsBoss > Build Test_Player Scene : สร้างพื้น ผู้เล่น กล้อง ให้พร้อมกด Play
public static class SceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Player.unity";

    [MenuItem("BoatVsBoss/Build Test_Player Scene")]
    public static void BuildTestPlayerScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DestroyIfExists("Ground");
        DestroyIfExists("Player");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        Player playerScript = player.AddComponent<Player>();
        playerScript.characterName = "Hero";

        for (int i = 0; i < 3; i++)
        {
            string dummyName = "Dummy_" + i;
            DestroyIfExists(dummyName);
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummy.name = dummyName;
            dummy.transform.position = new Vector3(-3f + i * 3f, 0.75f, 4f);
            dummy.transform.localScale = new Vector3(1f, 1.5f, 1f);
            dummy.AddComponent<TrainingDummy>().characterName = dummyName;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Test_Player scene built");
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) Object.DestroyImmediate(existing);
    }
}
