using UnityEditor;

// ตั้งค่า import ของ goblin.fbx อัตโนมัติ: ท่า idle / walk / run วนลูป (ค่าเดิมจาก Asset Store ไม่วน ตัวจะค้างท่าสุดท้าย)
public class GoblinImportSettings : AssetPostprocessor
{
    public const string FbxPath = "Assets/Goblins -characters with animations/Goblins -characters with animations/models/goblin.fbx";

    private void OnPreprocessAnimation()
    {
        if (assetPath != FbxPath) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            string n = clips[i].name.ToLower();
            clips[i].loopTime = n.Contains("idle") || n.Contains("walk") || n.Contains("run");
        }
        importer.clipAnimations = clips;
    }

    // เรียกจาก EnemySceneBuilder: ถ้ายังไม่เคยตั้งค่าลูป ให้ import ใหม่
    public static void EnsureImported()
    {
        ModelImporter importer = AssetImporter.GetAtPath(FbxPath) as ModelImporter;
        if (importer == null) return;
        bool hasLoop = false;
        foreach (ModelImporterClipAnimation c in importer.clipAnimations)
        {
            if (c.loopTime) hasLoop = true;
        }
        if (!hasLoop) AssetDatabase.ImportAsset(FbxPath, ImportAssetOptions.ForceUpdate);
    }
}
