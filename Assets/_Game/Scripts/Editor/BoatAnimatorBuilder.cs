using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// สร้าง Animator Controller ของ Boat: Idle <-> Run ตาม Speed, Combo1-3 (Player สั่ง CrossFade) / Skill / Hit จาก Any State, Death ค้าง
public static class BoatAnimatorBuilder
{
    public const string ControllerPath = "Assets/_Game/Art/Boat/Boat.controller";
    private const string AnimFolder = "Assets/_Game/Art/Boat/Anims/";

    [MenuItem("BoatVsBoss/Build Boat Animator")]
    public static AnimatorController Build()
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        AnimatorController ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Skill", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
        ac.AddParameter("Dead", AnimatorControllerParameterType.Bool);

        AnimatorStateMachine sm = ac.layers[0].stateMachine;
        AnimatorState idle = AddState(sm, "Idle", "Idle", new Vector3(300, 0));
        AnimatorState run = AddState(sm, "Run", "Run", new Vector3(300, 120));
        // คอมโบ 1-2-3 อยู่เลเยอร์ UpperBody: ตัวบนต่อย ขายังเดิน/วิ่งตามเลเยอร์ล่าง
        ac.AddLayer(new AnimatorControllerLayer
        {
            name = "UpperBody",
            defaultWeight = 1f,
            avatarMask = BuildUpperBodyMask(),
            stateMachine = new AnimatorStateMachine { name = "UpperBody" }
        });
        AnimatorStateMachine upper = ac.layers[1].stateMachine;
        AssetDatabase.AddObjectToAsset(upper, ac);
        AnimatorState empty = upper.AddState("Empty", new Vector3(300, 0)); // ไม่มีท่า = ใช้ท่าของเลเยอร์ล่าง
        upper.defaultState = empty;
        // จ๊อบ -> ฮุก -> อัปเปอร์คัต
        AnimatorState combo1 = AddState(upper, "Combo1", "Punch", new Vector3(600, -100));
        AnimatorState combo2 = AddState(upper, "Combo2", "HookPunch", new Vector3(600, 0));
        AnimatorState combo3 = AddState(upper, "Combo3", "Uppercut", new Vector3(600, 100));
        AnimatorState skill = AddState(sm, "Skill", "HookPunch", new Vector3(600, 20));
        AnimatorState hit = AddState(sm, "Hit", "Hit", new Vector3(600, 120));
        AnimatorState death = AddState(sm, "Death", "Death", new Vector3(600, 220));
        // ท่าพุ่ง (Boxing Dodge Advance) — Player.cs สั่ง CrossFade ตอนกด Space
        AnimatorState dash = AddState(sm, "Dash", "Dash", new Vector3(600, 320));
        dash.speed = 2.5f; // ใช้ช่วงก้าวพุ่งต้นท่า Player ตัดกลับ Idle ตอนพุ่งจบ
        // ท่าสกิลของ Boat — Player.cs สั่ง CrossFade ตามชื่อใน SkillData.animState
        AnimatorState overPunch = AddState(sm, "OverPunch", "OverPunch", new Vector3(900, -100)); // Cross Punch
        AnimatorState stomp = AddState(sm, "Stomp", "Stomp", new Vector3(900, 0));               // Stomp
        overPunch.speed = 1.5f; // ท่าเดิมช้า เร่งให้หมัดหนักแต่ไม่อืด
        stomp.speed = 1.3f;
        sm.defaultState = idle;

        var toRun = idle.AddTransition(run);
        toRun.hasExitTime = false; toRun.duration = 0.1f;
        toRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = run.AddTransition(idle);
        toIdle.hasExitTime = false; toIdle.duration = 0.1f;
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        // ท่าคอมโบ Player.cs สั่งเล่นตรง ๆ ด้วย CrossFade (ไม่ใช้ Trigger) เร่งให้หมัดไวขึ้น
        foreach (AnimatorState c in new[] { combo1, combo2, combo3 })
        {
            c.speed = 1.4f;
        }
        AnyTo(sm, skill, "Skill", 0.05f);
        AnyTo(sm, hit, "Hit", 0.05f);
        var toDeath = sm.AddAnyStateTransition(death);
        toDeath.hasExitTime = false; toDeath.duration = 0.1f; toDeath.canTransitionToSelf = false;
        toDeath.AddCondition(AnimatorConditionMode.If, 0f, "Dead");

        // เล่นจบแล้วกลับไป Idle
        foreach (AnimatorState s in new[] { skill, hit, dash, overPunch, stomp })
        {
            var back = s.AddTransition(idle);
            back.hasExitTime = true; back.exitTime = 0.85f; back.duration = 0.1f;
        }
        // คอมโบต่อยจบ กลับไป Empty ให้ตัวบนกลับไปเล่นตามเลเยอร์ล่าง
        foreach (AnimatorState c in new[] { combo1, combo2, combo3 })
        {
            var back = c.AddTransition(empty);
            back.hasExitTime = true; back.exitTime = 0.85f; back.duration = 0.1f;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Boat Animator built");
        return ac;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string stateName, string clipFile, Vector3 pos)
    {
        AnimatorState state = sm.AddState(stateName, pos);
        state.motion = LoadClip(clipFile);
        if (state.motion == null) Debug.LogWarning("ไม่พบท่า " + clipFile);
        return state;
    }

    // มาสก์ตัวบน: เปิดลำตัว หัว แขน มือ / ปิดขากับ root
    private static AvatarMask BuildUpperBodyMask()
    {
        const string maskPath = "Assets/_Game/Art/Boat/UpperBody.mask";
        AssetDatabase.DeleteAsset(maskPath);
        AvatarMask mask = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
            bool on = part == AvatarMaskBodyPart.Body || part == AvatarMaskBodyPart.Head
                || part == AvatarMaskBodyPart.LeftArm || part == AvatarMaskBodyPart.RightArm
                || part == AvatarMaskBodyPart.LeftFingers || part == AvatarMaskBodyPart.RightFingers;
            mask.SetHumanoidBodyPartActive(part, on);
        }
        AssetDatabase.CreateAsset(mask, maskPath);
        return mask;
    }

    private static void AnyTo(AnimatorStateMachine sm, AnimatorState target, string trigger, float duration)
    {
        var t = sm.AddAnyStateTransition(target);
        t.hasExitTime = false; t.duration = duration; t.canTransitionToSelf = false;
        t.AddCondition(AnimatorConditionMode.If, 0f, trigger);
        t.AddCondition(AnimatorConditionMode.IfNot, 0f, "Dead");
    }

    private static AnimationClip LoadClip(string fileName)
    {
        foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(AnimFolder + fileName + ".fbx"))
        {
            if (o is AnimationClip clip && !clip.name.StartsWith("__preview")) return clip;
        }
        return null;
    }
}
