using UnityEditor;

// ตั้งค่า import ท่าของบอสออค อัตโนมัติ
// - ท่าในแพ็ก PolyNext ตั้ง Loop ไว้ทุกท่า (ท่าตี/ท่าตายก็วน) -> ให้วนแค่ท่ายืน/เดิน/วิ่ง
// - ท่าที่ทำเพิ่มใน Blender (Orc_Jump) อยู่ใน _Game/Art/Enemies/Orc/ : Generic เหมือนของแพ็ก
public class OrcImportSettings : AssetPostprocessor
{
    public const string PackFolder = "Assets/PolyNext/Low Poly Orc Warrior/Animations/";
    public const string Folder = "Assets/_Game/Art/Enemies/Orc/";
    public const string Idle = PackFolder + "01_Orc_Warrior_Idle1.fbx";
    public const string Walk = PackFolder + "03_Orc_Warrior_Walk.fbx";
    public const string Slam = PackFolder + "07_Orc_Warrior_Attack1.fbx";
    public const string Sweep = PackFolder + "08_Orc_Warrior_Attack2.fbx";
    public const string Hit = PackFolder + "09_Orc_Warrior_Hit.fbx";
    public const string Death = PackFolder + "10_Orc_Warrior_Death.fbx";
    public const string Jump = Folder + "Orc_Jump.fbx";
    public const string Guard = PackFolder + "05_Orc_Warrior_Idle_Battle1.fbx"; // ตั้งโล่ = ท่าตั้งการ์ดของแพ็ก

    private static bool IsOrcAnim(string path)
    {
        return path.EndsWith(".fbx") && (path.StartsWith(PackFolder) || path.StartsWith(Folder));
    }

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".fbx")) return;
        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.avatarSetup = ModelImporterAvatarSetup.NoAvatar;
        // สำคัญ: ไฟล์จาก Blender มีโหนดบนสุดโหนดเดียว ("root") Unity จะยุบทิ้ง ทำให้ path ท่าเป็น "root.x/..."
        // แต่โมเดลออคเป็น "root/root.x/..." -> ท่าไม่ขยับเลย ต้องเก็บ hierarchy ไว้ให้ path ตรงกัน
        importer.preserveHierarchy = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.importCameras = false;
        importer.importLights = false;
    }

    private void OnPreprocessAnimation()
    {
        if (!IsOrcAnim(assetPath)) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        // วนลูป: ท่ายืน (Idle, Idle_Battle = ตั้งโล่), เดิน, วิ่ง
        bool loop = assetPath.Contains("Idle") || assetPath.Contains("_Walk") || assetPath.Contains("_Run");
        ModelImporterClipAnimation[] clips = importer.clipAnimations;
        // ไฟล์จาก Blender: ใช้ช่วงเฟรมเต็มของไฟล์ใหม่ทุกครั้ง
        // (ถ้าใช้ clipAnimations เดิม Unity จะจำช่วงเฟรมของไฟล์เก่าไว้ ท่าที่ยาวขึ้นจะโดนตัดท้ายทิ้ง)
        if (clips == null || clips.Length == 0 || assetPath.StartsWith(Folder)) clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = loop;
        }
        importer.clipAnimations = clips;
    }

    // เรียกจาก EnemySceneBuilder: import ท่าที่ใช้ใหม่ให้ตั้งค่าข้างบนทำงาน
    public static void EnsureImported()
    {
        string[] files = { Idle, Walk, Slam, Sweep, Hit, Death, Jump, Guard };
        for (int i = 0; i < files.Length; i++)
        {
            AssetDatabase.ImportAsset(files[i], ImportAssetOptions.ForceUpdate);
        }
    }
}
