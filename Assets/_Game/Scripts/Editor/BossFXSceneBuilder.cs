// เมนูของส่วนบอส (Keerati)
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เมนูใส่เอฟเฟกต์บอส + cutscene บอสเกิด ลงซีน Gameplay (เพิ่ม object ใหม่ ไม่แก้ของเพื่อน)
public static class BossFXSceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Gameplay.unity";
    private const string ObjectName = "BossFX";
    private const string VFXFolder = "Assets/_Game/VFX/Boss/";

    [MenuItem("BoatVsBoss/Boss/Add Boss VFX + Intro Cutscene to Gameplay Scene")]
    public static void AddToGameplayScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("กด Stop ก่อน แล้วค่อยกดเมนูนี้");
            return;
        }
        GameObject crater = LoadVFX("CraterBlast");
        GameObject spear = LoadVFX("SpearRing");
        GameObject summon = LoadVFX("CursedTalismans");
        if (crater == null || spear == null || summon == null)
        {
            Debug.LogError("ยังไม่มี prefab เอฟเฟกต์บอส รอ Unity import โฟลเดอร์ VFX/Boss เสร็จ (NightForgeImporter สร้าง all.prefab ให้) แล้วกดใหม่");
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
        SerializedObject vfx = new SerializedObject(obj.AddComponent<BossSkillVFX>());
        vfx.FindProperty("craterBlast").objectReferenceValue = crater;
        vfx.FindProperty("spearRing").objectReferenceValue = spear;
        vfx.ApplyModifiedPropertiesWithoutUndo();
        SerializedObject intro = new SerializedObject(obj.AddComponent<BossIntroCutscene>());
        intro.FindProperty("summonVFX").objectReferenceValue = summon;
        intro.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = obj;
        Debug.Log("ใส่ BossFX (เอฟเฟกต์ท่าบอส + cutscene ตอนบอสเกิด) ลงซีน Gameplay แล้ว");
    }

    private static GameObject LoadVFX(string folder)
    {
        return AssetDatabase.LoadAssetAtPath<GameObject>(VFXFolder + folder + "/all.prefab");
    }
}
