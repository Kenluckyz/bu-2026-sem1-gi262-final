using UnityEditor;
using UnityEngine;

// ท่าที่ทำใน Blender (Skeleton_Throw/Death/Hit) มีคีย์ของ "Group" ติดมาด้วย
// Unity มองโหนดบนสุดของ FBX (Group) เป็นตัวโมเดลเอง = SkeletonModel
// คีย์ scale = 1 ของ Group เลยไปทับ scale 0.846 ที่ EnemySceneBuilder ย่อไว้ -> ตัวขยายตอนเล่นท่าเหล่านี้
// แก้: ตอน import ลบคีย์ของตัวโมเดล (path "") ทิ้ง ให้ท่าขยับแค่กระดูก เหมือนท่าเดิมของ Asset Store
public class SkeletonAnimImportSettings : AssetPostprocessor
{
    public const string Folder = "Assets/_Game/Art/Enemies/Skeleton/";
    private static readonly string[] Clips = { "Skeleton_Throw.fbx", "Skeleton_Death.fbx", "Skeleton_Hit.fbx" };

    private void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        if (!assetPath.StartsWith(Folder)) return;

        int removed = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            // 1. path "" = ตัวโมเดลเอง (ไม่ใช่กระดูก) -> ลบทิ้ง
            if (binding.path == "" && binding.type == typeof(Transform))
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
                removed++;
            }
        }
        if (removed > 0) Debug.Log(clip.name + ": ลบคีย์ของตัวโมเดล " + removed + " เส้น (กันตัวขยาย)");
    }

    // เรียกจาก EnemySceneBuilder: import ท่าใหม่ให้กฎข้างบนทำงาน
    public static void EnsureImported()
    {
        for (int i = 0; i < Clips.Length; i++)
        {
            AssetDatabase.ImportAsset(Folder + Clips[i], ImportAssetOptions.ForceUpdate);
        }
    }
}
