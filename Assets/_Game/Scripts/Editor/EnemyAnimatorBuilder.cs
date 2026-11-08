using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// สร้าง Animator Controller ของศัตรูจากไฟล์ FBX — ใช้ซ้ำได้กับศัตรูแบบอื่น (ส่งชื่อท่าในไฟล์มา)
// State: Move (Blend Tree ยืน<->วิ่ง ตาม Speed), Attack1..N, Hit (Enemy.cs สั่ง CrossFade), Dead (Any State เมื่อ Dead = true)
public static class EnemyAnimatorBuilder
{
    // แบบที่ 1: ทุกท่าอยู่ในไฟล์ FBX เดียว (ก็อบลิน) — ส่งชื่อท่ามา
    // เรียกแบบที่ 2 ต่อ เพื่อให้ logic สร้าง Animator อยู่ที่เดียว (แบบ Heal(hp) เรียก Heal(hp, bonus))
    public static AnimatorController Build(string controllerPath, string fbxPath,
        string idleClip, string runClip, string[] attackClips, string hitClip, string deadClip)
    {
        Dictionary<string, AnimationClip> clips = LoadClips(fbxPath);
        AnimationClip[] attacks = new AnimationClip[attackClips.Length];
        for (int i = 0; i < attackClips.Length; i++)
        {
            attacks[i] = GetClip(clips, attackClips[i]);
        }
        return Build(controllerPath, GetClip(clips, idleClip), GetClip(clips, runClip), attacks,
            GetClip(clips, hitClip), GetClip(clips, deadClip));
    }

    // แบบที่ 2: ส่งท่ามาเป็น AnimationClip ตรง ๆ — ท่ามาจากหลายไฟล์ได้ (โครงกระดูก: IDLE.fbx, Run.fbx, Skeleton_Throw.fbx ...)
    public static AnimatorController Build(string controllerPath, AnimationClip idleClip, AnimationClip runClip,
        AnimationClip[] attackClips, AnimationClip hitClip, AnimationClip deadClip)
    {
        // ห้ามลบไฟล์แล้วสร้างใหม่: GUID จะเปลี่ยน แล้ว Prefab ที่อ้างตัวเก่าจะกลายเป็น Missing (ท่าหายหมด)
        // มีอยู่แล้ว -> ล้างข้างในแล้วสร้างใหม่ในไฟล์เดิม
        AnimatorController ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if (ac == null) ac = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        else ClearController(ac, controllerPath);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Dead", AnimatorControllerParameterType.Bool);
        ac.AddParameter(new AnimatorControllerParameter
        {
            name = "AttackSpeed",
            type = AnimatorControllerParameterType.Float,
            defaultFloat = 1f
        });

        AnimatorStateMachine sm = ac.layers[0].stateMachine;

        // ยืน/วิ่งเป็น Blend Tree เดียว — Enemy.Move() ส่ง Speed 0..1
        AnimatorState move = ac.CreateBlendTreeInController("Move", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idleClip, 0f);
        tree.AddChild(runClip, 1f);
        sm.defaultState = move;

        // ท่าตี (ชื่อ State = Attack1, Attack2, ... ตรงกับ EnemyData.attackAnims) ความเร็วตาม AttackSpeed
        for (int i = 0; i < attackClips.Length; i++)
        {
            AnimatorState attack = AddState(sm, "Attack" + (i + 1), attackClips[i], new Vector3(550, -60 + i * 70));
            attack.speedParameter = "AttackSpeed";
            attack.speedParameterActive = true;
            BackToMove(attack, move); // กันค้าง ปกติ Enemy.cs CrossFade กลับเอง
        }

        AnimatorState hit = AddState(sm, "Hit", hitClip, new Vector3(550, 200));
        hit.speed = 1.5f; // โดนตีสะดุ้งเร็ว ๆ
        BackToMove(hit, move);

        AnimatorState dead = AddState(sm, "Dead", deadClip, new Vector3(550, 300));
        var toDead = sm.AddAnyStateTransition(dead);
        toDead.hasExitTime = false;
        toDead.duration = 0.1f;
        toDead.canTransitionToSelf = false;
        toDead.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

        EditorUtility.SetDirty(ac);
        AssetDatabase.SaveAssets();
        Debug.Log("Enemy Animator built: " + controllerPath);
        return ac;
    }

    // ล้าง State / Transition / Parameter / Blend Tree เดิมทิ้ง แต่เก็บตัวไฟล์ (GUID) ไว้
    private static void ClearController(AnimatorController ac, string controllerPath)
    {
        while (ac.layers.Length > 1) ac.RemoveLayer(ac.layers.Length - 1);
        AnimatorStateMachine sm = ac.layers[0].stateMachine;
        foreach (AnimatorStateTransition t in sm.anyStateTransitions) sm.RemoveAnyStateTransition(t);
        foreach (AnimatorTransition t in sm.entryTransitions) sm.RemoveEntryTransition(t);
        foreach (ChildAnimatorState s in sm.states) sm.RemoveState(s.state);
        ac.parameters = new AnimatorControllerParameter[0];

        // Blend Tree เป็น sub-asset ไม่ถูกลบตามตอน RemoveState
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(controllerPath))
        {
            if (o is BlendTree) Object.DestroyImmediate(o, true);
        }
    }

    private static void BackToMove(AnimatorState from, AnimatorState move)
    {
        var t = from.AddTransition(move);
        t.hasExitTime = true;
        t.exitTime = 0.95f;
        t.duration = 0.1f;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string stateName, Motion clip, Vector3 pos)
    {
        AnimatorState state = sm.AddState(stateName, pos);
        state.motion = clip;
        return state;
    }

    private static AnimationClip GetClip(Dictionary<string, AnimationClip> clips, string clipName)
    {
        if (clips.TryGetValue(clipName.ToLower(), out AnimationClip clip)) return clip;
        Debug.LogWarning("ไม่พบท่า " + clipName + " ในไฟล์ FBX (มี: " + string.Join(", ", clips.Keys) + ")");
        return null;
    }

    // ท่าแรกในไฟล์ FBX (ไฟล์ที่มีท่าเดียว เช่น Skeleton_Throw.fbx)
    public static AnimationClip LoadFirstClip(string fbxPath)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (o is AnimationClip clip && !clip.name.StartsWith("__preview")) return clip;
        }
        Debug.LogWarning("ไม่พบท่าในไฟล์ " + fbxPath);
        return null;
    }

    // ท่าทั้งหมดในไฟล์ FBX: ชื่อ (ตัวเล็ก) -> clip
    private static Dictionary<string, AnimationClip> LoadClips(string fbxPath)
    {
        var result = new Dictionary<string, AnimationClip>();
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
        {
            if (o is AnimationClip clip && !clip.name.StartsWith("__preview"))
            {
                result[clip.name.ToLower()] = clip;
            }
        }
        return result;
    }
}
