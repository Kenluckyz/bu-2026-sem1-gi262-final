using System.Collections.Generic;
using UnityEngine;

// สกิลต่อยหมุน โจมตีศัตรูที่อยู่รอบตัวผู้เล่น (ยังไม่ได้ใช้ในเกม)
public class SpinPunchSkill : ActiveSkill
{
    public SpinPunchSkill(SkillData data) : base(data) { }

    // หาศัตรูรอบตัวแล้วทำดาเมจทุกตัวที่อยู่ในระยะ
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

// สกิลพุ่งไปข้างหน้าและทำดาเมจศัตรูที่ชนระหว่างพุ่ง
// ดาเมจ/คูลดาวน์แก้ใน Dash.asset ส่วนความเร็ว/ระยะพุ่งแก้ dashSpeed, dashTime ที่ Player
public class DashStrikeSkill : ActiveSkill
{
    public DashStrikeSkill(SkillData data) : base(data) { }

    // ส่งค่าดาเมจและระยะโจมตีไปให้ระบบ Dash ใน Player จัดการ
    protected override void Activate(Player player)
    {
        player.Dash(Value(), data.radius);
    }
}

// สกิลโจมตีเป็นแนวกว้างด้านหน้าผู้เล่น
public class OverPunchSkill : ActiveSkill
{
    public OverPunchSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        Transform t = player.transform;

        // หาวงที่ใหญ่พอครอบมุมของกรอบสี่เหลี่ยมแล้วค่อยคัดทีละตัว
        float searchRadius = Mathf.Sqrt(data.radius * data.radius + data.hitWidth * data.hitWidth);
        List<Character> targets = player.FindTargetsInRadius(searchRadius);
        int hitCount = 0;
        for (int i = 0; i < targets.Count; i++)
        {
            Vector3 toTarget = targets[i].transform.position - t.position;
            toTarget.y = 0f;
            // เช็กว่าศัตรูอยู่ด้านหน้าและอยู่ในความกว้างของพื้นที่โจมตีหรือไม่
            float forward = Vector3.Dot(toTarget, t.forward); // ระยะตามแนวด้านหน้า
            float side = Vector3.Dot(toTarget, t.right); // ระยะเยื้องไปทางซ้ายหรือขวา
            // ศัตรูที่อยู่ในระยะด้านหน้าและความกว้างที่กำหนดจะได้รับดาเมจ
            // ถ้าต้องการปรับพื้นที่โจมตี ให้แก้ radius และ hitWidth ใน OverPunch.asset
            if (forward >= -0.5f && forward <= data.radius && Mathf.Abs(side) <= data.hitWidth / 2f)
            {
                player.DealDamage(targets[i], Value());
                hitCount++;
            }
        }
        Debug.Log("OverPunch hit " + hitCount + " targets");
    }
}

// สกิลกระทืบพื้นโจมตีศัตรูที่อยู่รอบตัวผู้เล่น
// วงกว้างแก้ radius ใน FinalExecution.asset
public class FinalExecutionSkill : ActiveSkill
{
    public FinalExecutionSkill(SkillData data) : base(data) { }

    // ค้นหาศัตรูในระยะแล้วทำดาเมจทุกตัว
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

// สกิลติดตัวยิ่งเลือดเหลือน้อยดาเมจยิ่งเพิ่มขึ้น
// % สูงสุดแก้ baseValue ใน MetalHeart.asset (50 = 50%)
public class MetalHeartSkill : PassiveSkill
{
    public MetalHeartSkill(SkillData data) : base(data) { }

    // ไม่ต้องเพิ่มค่าโดยตรงเพราะคำนวณโบนัสจากเลือดที่เหลือในTick()
    protected override void Apply(Player player, float amount) { }

    // ตรวจสอบเลือดที่หายไปแล้วคำนวณโบนัสความเสียหายใหม่ทุกเฟรม
    public override void Tick(Player player)
    {
        float missingHP = 1f - player.CurrentHP / player.MaxHP; // 0 คือเลือดเต็ม และ 1 คือเลือดหมด
        player.SetBonusDamage(missingHP * Value() / 100f);
    }
}

// สกิลโจมตีศัตรูที่มีเลือดเหลือน้อยที่สุดก่อน (ยังไม่ได้ใช้ในเกม)
public class FireStormSkill : ActiveSkill
{
    public FireStormSkill(SkillData data) : base(data) { }

    protected override void Activate(Player player)
    {
        // หาศัตรูในระยะ แล้วเรียงลำดับจากน้อยไปมาก
        Character[] enemies = player.FindTargetsInRadius(data.radius).ToArray();
        SortByRemainingHP(enemies);

        // ใช้ค่า targetCount กำหนดว่าจะโจมตีกี่ตัว
        // ถ้าศัตรูมีน้อยกว่าจำนวนที่ตั้งไว้ จะโจมตีเท่าที่มี
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

    // เรียงศัตรูตามเลือดที่เหลือ โดยใช้วิธี Selection Sort
    // ศัตรูที่เลือดน้อยจะถูกย้ายมาอยู่ด้านหน้าของ Array
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

// สกิลติดตัวที่เพิ่มค่าสถานะของผู้เล่นเป็นเปอร์เซ็นต์ (ยังไม่ได้ใช้ในเกม)
public class StatPassiveSkill : PassiveSkill
{
    public StatPassiveSkill(SkillData data) : base(data) { }

    // ส่งประเภทค่าสถานะและเปอร์เซ็นต์ที่ต้องการเพิ่มไปให้ Player
    // เลือกว่าจะเพิ่มเลือด ดาเมจ หรือความเร็วได้จาก statType ใน SkillData
    protected override void Apply(Player player, float percent)
    {
        player.ApplyStatBoost(data.statType, percent / 100f, true);
    }
}