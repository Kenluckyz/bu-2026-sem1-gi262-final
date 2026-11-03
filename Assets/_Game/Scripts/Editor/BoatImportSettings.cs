using UnityEditor;

// ตั้งค่า import ของโมเดล/ท่า Boat อัตโนมัติ: Humanoid ทุกไฟล์, ท่า Idle/Run วนลูป
public class BoatImportSettings : AssetPostprocessor
{
    private const string Folder = "Assets/_Game/Art/Boat/";

    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(Folder)) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Human;
        importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;

        bool isAnim = assetPath.Contains("/Anims/");
        importer.importAnimation = isAnim;
        importer.materialImportMode = isAnim ? ModelImporterMaterialImportMode.None : ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        importer.importCameras = false;
        importer.importLights = false;
    }

    private void OnPreprocessAnimation()
    {
        if (!assetPath.StartsWith(Folder + "Anims/")) return;

        ModelImporter importer = (ModelImporter)assetImporter;
        bool loop = assetPath.EndsWith("Idle.fbx") || assetPath.EndsWith("Run.fbx");
        ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++)
        {
            clips[i].loopTime = loop;
            // ล็อกการหมุน/ความสูงไว้ที่ตัว ไม่ให้ท่าพาตัวละครลอยหรือหมุนเอง
            clips[i].lockRootRotation = true;
            clips[i].lockRootHeightY = true;
            clips[i].keepOriginalPositionY = true;
            clips[i].lockRootPositionXZ = true;
        }
        importer.clipAnimations = clips;
    }
}
