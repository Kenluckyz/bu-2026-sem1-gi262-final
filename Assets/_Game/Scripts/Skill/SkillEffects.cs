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
