using System.Collections.Generic;
using UnityEngine;

// ต่อยหมุน โดนรอบตัว
public class SpinPunchSkill : ActiveSkill
{
    public SpinPunchSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        List<Character> targets = player.FindTargetsInRadius(data.radius);
        for (int i = 0; i < targets.Count; i++)
        {
            player.DealDamage(targets[i], Value());
        }
        Debug.Log("SpinPunch hit " + targets.Count + " targets");
    }
}

// พุ่ง ชนแล้วโดน
public class DashStrikeSkill : ActiveSkill
{
    public DashStrikeSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        player.Dash(Value(), data.radius);
    }
}

// ฟันไฟแนวนอน โดนเป็นแถบข้างหน้า
public class OverPunchSkill : ActiveSkill
{
    public OverPunchSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        Transform t = player.transform;
        // หาวงใหญ่ก่อนแล้วค่อยเช็กทีละตัว
        float searchRadius = Mathf.Sqrt(data.radius * data.radius + data.hitWidth * data.hitWidth);
        List<Character> targets = player.FindTargetsInRadius(searchRadius);
        int hitCount = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 toTarget = targets[i].transform.position - t.position;
            toTarget.y = 0f;
            float forward = Vector3.Dot(toTarget, t.forward); // ไกลไปข้างหน้า
            float side = Vector3.Dot(toTarget, t.right); // เยื้องซ้ายขวา
            // ติดตัวมากๆ ก็ให้โดน
            if (forward >= -0.5f && forward <= data.radius && Mathf.Abs(side) <= data.hitWidth / 2f)
            {
                player.DealDamage(targets[i], Value());
                hitCount++;
            }
        }
        Debug.Log("OverPunch hit " + hitCount + " targets");
    }
}

// กระทืบ โดนรอบตัว
public class FinalExecutionSkill : ActiveSkill
{
    public FinalExecutionSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        List<Character> targets = player.FindTargetsInRadius(data.radius);
        for (int i = 0; i < targets.Count; i++)
        {
            player.DealDamage(targets[i], Value());
        }
        Debug.Log("FinalExecution hit " + targets.Count + " targets");
    }
}

// ยิ่งเลือดน้อยยิ่งแรง
public class MetalHeartSkill : PassiveSkill
{
    public MetalHeartSkill(SkillData data) : base(data) { }

    // ไม่ได้ใช้ คิดใน Tick แทน
    protected override void Apply(Player player, float amount) { }

    public override void Tick(Player player)
    {
        float missingHP = 1f - player.CurrentHP / player.MaxHP; // 0 = เลือดเต็ม 1 = หมด
        player.SetBonusDamage(missingHP * Value() / 100f);
    }
}

// ตีตัวที่เลือดน้อยสุด เอามาจาก UseFireStorm + Sort ใน week7
public class FireStormSkill : ActiveSkill
{
    public FireStormSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        Character[] enemies = player.FindTargetsInRadius(data.radius).ToArray();
        SortByRemainingHP(enemies);

        int count = data.targetCount;
        if (count > enemies.Length)
        {
            count = enemies.Length;
        }
        for (int i = 0; i < count; i++)
        {
            player.DealDamage(enemies[i], Value());
        }
        Debug.Log("FireStorm hit " + count + " lowest HP targets");
    }

    // selection sort แบบ SortEnemiesByRemainningEnergy1
    private static void SortByRemainingHP(Character[] enemies)
    {
        for (int i = 0; i < enemies.Length - 1; i++)
        {
            int minIndex = i;
            for (int j = i + 1; j < enemies.Length; j++)
            {
                if (enemies[j].CurrentHP < enemies[minIndex].CurrentHP)
                {
                    minIndex = j;
                }
            }
            Character temp = enemies[i];
            enemies[i] = enemies[minIndex];
            enemies[minIndex] = temp;
        }
    }
}

// เพิ่ม stat เป็น %
public class StatPassiveSkill : PassiveSkill
{
    public StatPassiveSkill(SkillData data) : base(data) { }

    protected override void Apply(Player player, float percent)
    {
        player.ApplyStatBoost(data.statType, percent / 100f, true);
    }
}
