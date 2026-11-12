using System.Collections.Generic;
using UnityEngine;

// Active: ต่อยหมุนรอบตัว โดนทุกตัวในวงรอบผู้เล่น
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
        Debug.Log($"SpinPunch hit {targets.Count} targets");
    }
}

// Active: พุ่งโจมตี — พุ่งไปข้างหน้า ศัตรูที่พุ่งผ่านโดนดาเมจ (ตัวละ 1 ครั้ง)
public class DashStrikeSkill : ActiveSkill
{
    public DashStrikeSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        player.Dash(Value(), data.radius);
    }
}

// Active: ปล่อยหมัดไฟแนวนอน โดนศัตรูทุกตัวในแถบสี่เหลี่ยมข้างหน้า ตั้งแต่ชิดตัวจนสุดระยะ radius
public class OverPunchSkill : ActiveSkill
{
    public OverPunchSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        Transform t = player.transform;
        // หาวงกว้างพอครอบมุมแถบก่อน แล้วค่อยคัดด้วยระยะหน้า/ข้าง
        float searchRadius = Mathf.Sqrt(data.radius * data.radius + data.hitWidth * data.hitWidth);
        List<Character> targets = player.FindTargetsInRadius(searchRadius);
        int hitCount = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 toTarget = targets[i].transform.position - t.position;
            toTarget.y = 0f;
            float forward = Vector3.Dot(toTarget, t.forward); // ไกลไปข้างหน้าเท่าไร
            float side = Vector3.Dot(toTarget, t.right);      // เยื้องซ้ายขวาเท่าไร
            // -0.5 = ศัตรูที่ชิดตัวมาก (ทับตัวนิด ๆ) ก็โดน
            if (forward >= -0.5f && forward <= data.radius && Mathf.Abs(side) <= data.hitWidth / 2f)
            {
                player.DealDamage(targets[i], Value());
                hitCount++;
            }
        }
        Debug.Log($"OverPunch hit {hitCount} targets");
    }
}

// Active: กระทืบเท้า ดาเมจวงกว้างรอบตัว
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
        Debug.Log($"FinalExecution hit {targets.Count} targets");
    }
}

// Passive: Metal Heart ยิ่งเลือดน้อยยิ่งตีแรง — Value() = % ดาเมจที่เพิ่มตอนเลือดใกล้หมด
public class MetalHeartSkill : PassiveSkill
{
    public MetalHeartSkill(SkillData data) : base(data) { }

    // ไม่ได้บวกค่าทีเดียว แต่คิดใหม่ทุกเฟรมตามเลือดตอนนั้น (ใน Tick)
    protected override void Apply(Player player, float amount) { }

    public override void Tick(Player player)
    {
        float missingHP = 1f - player.CurrentHP / player.MaxHP; // 0 = เลือดเต็ม, 1 = เลือดหมด
        player.SetBonusDamage(missingHP * Value() / 100f);
    }
}

// Active: FireStorm ตีศัตรูที่เลือดน้อยสุด N ตัว — ต่อยอดจาก OOPPlayer.UseFireStorm + UtilitySortEnemies (week07)
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
        Debug.Log($"FireStorm hit {count} lowest HP targets");
    }

    // Selection Sort น้อยไปมาก แบบ SortEnemiesByRemainningEnergy1
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
            var temp = enemies[i];
            enemies[i] = enemies[minIndex];
            enemies[minIndex] = temp;
        }
    }
}

// Passive: เพิ่มสถานะเป็น % (HP / Damage / Speed)
public class StatPassiveSkill : PassiveSkill
{
    public StatPassiveSkill(SkillData data) : base(data) { }

    protected override void Apply(Player player, float percent)
    {
        player.ApplyStatBoost(data.statType, percent / 100f, true);
    }
}
