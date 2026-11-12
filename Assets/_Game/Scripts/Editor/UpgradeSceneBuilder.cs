using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เมนูของส่วน Upgrade: สร้างไฟล์ UpgradeData ตัวอย่าง แล้วใส่ UpgradeSystem ลงซีน Gameplay
// เพิ่ม object ใหม่อย่างเดียว ไม่แก้สคริปต์หรือ object ของเพื่อน
public static class UpgradeSceneBuilder
{
    private const string UpgradeFolder = "Assets/_Game/Data/Upgrades";
    private const string SkillFolder = "Assets/_Game/Data/Skills";
    private const string ScenePath = "Assets/_Game/Scenes/Gameplay.unity";
    private const string ObjectName = "UpgradeSystem";

    [MenuItem("BoatVsBoss/Upgrade/1. Create Upgrade Data")]
    public static void CreateUpgradeDataMenu()
    {
        List<UpgradeData> list = CreateUpgradeData();
        Debug.Log("สร้าง/อัปเดต UpgradeData " + list.Count + " ไฟล์ ที่ " + UpgradeFolder);
    }

    [MenuItem("BoatVsBoss/Upgrade/2. Add Upgrade System to Gameplay Scene")]
    public static void AddToGameplayScene()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("กด Stop ก่อน แล้วค่อยกดเมนูนี้");
            return;
        }
        List<UpgradeData> upgrades = CreateUpgradeData();

        // เปิดซีน Gameplay (ถามเซฟซีนเดิมก่อน)
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // กดซ้ำได้: ลบตัวเก่าของเราแล้วสร้างใหม่
        GameObject old = GameObject.Find(ObjectName);
        if (old != null) Object.DestroyImmediate(old);

        Player player = Object.FindAnyObjectByType<Player>();
        if (player == null) Debug.LogWarning("ไม่พบ Player ในซีน — UpgradeUI จะหาเองตอนเริ่มเกม");

        GameObject obj = new GameObject(ObjectName);
        UpgradeManager manager = obj.AddComponent<UpgradeManager>();
        UpgradeUI ui = obj.AddComponent<UpgradeUI>();

        SerializedObject so = new SerializedObject(manager);
        SerializedProperty list = so.FindProperty("allUpgrades");
        list.arraySize = upgrades.Count;
        for (int i = 0; i < upgrades.Count; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject uiSo = new SerializedObject(ui);
        uiSo.FindProperty("player").objectReferenceValue = player;
        uiSo.FindProperty("upgradeManager").objectReferenceValue = manager;
        uiSo.FindProperty("iconBackground").objectReferenceValue = LoadSprite("circle_slot");
        uiSo.FindProperty("iconRing").objectReferenceValue = LoadSprite("skill_slot_ring");
        uiSo.FindProperty("bannerCloth").objectReferenceValue = LoadSprite("upgrade_banner_cloth");
        uiSo.FindProperty("bannerTrim").objectReferenceValue = LoadSprite("upgrade_banner_trim");
        uiSo.FindProperty("bannerGlow").objectReferenceValue = LoadSprite("upgrade_banner_glow");
        uiSo.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = obj;
        Debug.Log("ใส่ UpgradeSystem ลงซีน Gameplay แล้ว (" + upgrades.Count + " อัปเกรด) กด Play แล้วฆ่าศัตรูจนเลเวลอัปได้เลย");
    }

    // ไฟล์ตัวอย่าง: สกิลของ Boat 3 ตัว (มีไฟล์อยู่แล้วจะอัปเดตค่า ไม่สร้างซ้ำ)
    private static List<UpgradeData> CreateUpgradeData()
    {
        if (!AssetDatabase.IsValidFolder(UpgradeFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Game/Data", "Upgrades");
        }

        List<UpgradeData> result = new List<UpgradeData>();
        AddSkill(result, "Dash", new Color(0.95f, 0.62f, 0.08f), "พุ่งไปข้างหน้า ทำดาเมจศัตรูที่พุ่งผ่าน และหลบการโจมตีระหว่างพุ่ง");
        AddSkill(result, "OverPunch", new Color(1f, 0.45f, 0.1f), "ต่อยแรงเป็นรูปกรวยข้างหน้า โดนศัตรูหลายตัว");
        AddSkill(result, "FinalExecution", new Color(0.85f, 0.12f, 0.15f), "ท่าไม้ตายดาเมจสูง ปิดเกมศัตรูที่อยู่ใกล้");
        // อัปแค่สกิล 3 ตัว (ช่องสกิลปุ่ม 1/2/3) — อยากได้ Stat เพิ่มภายหลังใช้ AddStat(...) ได้
        AssetDatabase.SaveAssets();
        return result;
    }

    private static void AddSkill(List<UpgradeData> result, string skillFile, Color color, string description)
    {
        SkillData skill = AssetDatabase.LoadAssetAtPath<SkillData>(SkillFolder + "/" + skillFile + ".asset");
        if (skill == null)
        {
            Debug.LogWarning("ไม่พบ SkillData " + skillFile + " (กด BoatVsBoss/Build Test_Player Scene เพื่อสร้างก่อน)");
            return;
        }
        UpgradeData data = LoadOrCreate("Skill_" + skillFile);
        data.type = UpgradeType.Skill;
        data.skill = skill;
        data.upgradeName = "";   // ใช้ชื่อสกิล
        data.icon = null;        // ใช้ไอคอนสกิล (icon_*.png)
        data.description = description;
        data.cardColor = color;
        EditorUtility.SetDirty(data);
        result.Add(data);
    }

    private static void AddStat(List<UpgradeData> result, string file, string displayName, StatType stat, float value, bool isPercent, string description)
    {
        UpgradeData data = LoadOrCreate(file);
        data.type = UpgradeType.Stat;
        data.upgradeName = displayName;
        data.statType = stat;
        data.value = value;
        data.isPercent = isPercent;
        data.skill = null;
        data.description = description;
        EditorUtility.SetDirty(data);
        result.Add(data);
    }

    private static UpgradeData LoadOrCreate(string file)
    {
        string path = UpgradeFolder + "/" + file + ".asset";
        UpgradeData data = AssetDatabase.LoadAssetAtPath<UpgradeData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<UpgradeData>();
            AssetDatabase.CreateAsset(data, path);
        }
        return data;
    }

    // โหลดรูปจาก Art/UI และตั้งให้เป็น Sprite ถ้ายังไม่ได้ตั้ง (แบบเดียวกับ SceneBuilder)
    private static Sprite LoadSprite(string fileName)
    {
        string path = "Assets/_Game/Art/UI/" + fileName + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("ไม่พบรูป " + path);
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
