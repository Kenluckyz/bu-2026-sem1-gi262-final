using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เมนูใส่ช่องคูลดาวน์ Shift หลบ ลงซีน Gameplay (เพิ่ม object ใหม่ ไม่แก้ของเพื่อน)
public static class DodgeUISceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Gameplay.unity";
    private const string ObjectName = "DodgeCooldownUI";

    [MenuItem("BoatVsBoss/Player/Add Dodge Cooldown UI to Gameplay Scene")]
    public static void AddToGameplayScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("กด Stop ก่อน แล้วค่อยกดเมนูนี้");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject old = GameObject.Find(ObjectName);
        if (old != null) Object.DestroyImmediate(old); // กดซ้ำได้

        GameObject obj = new GameObject(ObjectName);
        DodgeCooldownUI ui = obj.AddComponent<DodgeCooldownUI>();
        SerializedObject so = new SerializedObject(ui);
        so.FindProperty("player").objectReferenceValue = Object.FindAnyObjectByType<Player>();
        so.FindProperty("slotBackground").objectReferenceValue = LoadSprite("circle_slot");
        so.FindProperty("slotRing").objectReferenceValue = LoadSprite("skill_slot_ring");
        so.FindProperty("maskSprite").objectReferenceValue = LoadSprite("circle_white");
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = obj;
        Debug.Log("ใส่ช่องคูลดาวน์ Shift ลงซีน Gameplay แล้ว");
    }

    private static Sprite LoadSprite(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/" + fileName + ".png");
    }
}
