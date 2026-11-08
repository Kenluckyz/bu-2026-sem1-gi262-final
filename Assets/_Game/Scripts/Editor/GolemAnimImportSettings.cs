using UnityEditor;
using UnityEngine;

// ตั้งค่า import ท่าของโกเลม (ทำใน Blender) อัตโนมัติ
// - Humanoid ใช้ Avatar ของโมเดลโกเลมเอง (Copy From Other) กระดูกชื่อตรงกันทุกชิ้น
// - Idle / Walk วนลูป
// - ล็อกการหมุน/ความสูง/ตำแหน่งไว้ที่ตัว ไม่ให้ท่าพาตัวละครเลื่อนเอง (แบบเดียวกับ BoatImportSettings)
public class GolemAnimImportSettings : AssetPostprocessor
{
    public const string Folder = "Assets/_Game/Art/Enemies/Golem/";
    public const string GolemModel = "Assets/Kevin Iglesias/Characters/Humanoid Giant/Models/GiantModel01 - Golem.fbx";
    public static readonly string[] Clips = { "Golem_Idle", "Golem_Walk", "Golem_Slam", "Golem_Throw", "Golem_Hit", "Golem_Death" };

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".fbx")) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        Avatar golemAvatar = LoadGolemAvatar();
        if (golemAvatar != null)
        {
            importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
            importer.sourceAvatar = golemAvatar;
        }
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importCameras = false;
        importer.importLights = false;
    }

    private void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".fbx")) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        bool loop = assetPath.EndsWith("Golem_Idle.fbx") || assetPath.EndsWith("Golem_Walk.fbx");
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = loop;
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].keepOriginalPositionY = true;
            clips[i].lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
    }

    private static Avatar LoadGolemAvatar()
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(GolemModel))
        {
            if (o is Avatar avatar) return avatar;
        }
        Debug.LogWarning("ไม่พบ Avatar ของโกเลม: " + GolemModel);
        return null;
    }

    // เรียกจาก EnemySceneBuilder: import ท่าทั้งหมดใหม่ให้ตั้งค่าข้างบนทำงาน
    public static void EnsureImported()
    {
        for (int i = 0; i < Clips.Length; i++)
        {
            AssetDatabase.ImportAsset(Folder + Clips[i] + ".fbx", ImportAssetOptions.ForceUpdate);
        }
    }
}
