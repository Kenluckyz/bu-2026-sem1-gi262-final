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
        // คอมโบ 1-2-3: จ๊อบ -> ฮุก -> อัปเปอร์คัต
        AnimatorState combo1 = AddState(sm, "Combo1", "Punch", new Vector3(600, -200));
        AnimatorState combo2 = AddState(sm, "Combo2", "HookPunch", new Vector3(600, -140));
        AnimatorState combo3 = AddState(sm, "Combo3", "Uppercut", new Vector3(600, -80));
        AnimatorState skill = AddState(sm, "Skill", "HookPunch", new Vector3(600, 20));
        AnimatorState hit = AddState(sm, "Hit", "Hit", new Vector3(600, 120));
        AnimatorState death = AddState(sm, "Death", "Death", new Vector3(600, 220));
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
        foreach (AnimatorState s in new[] { combo1, combo2, combo3, skill, hit })
        {
            var back = s.AddTransition(idle);
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
