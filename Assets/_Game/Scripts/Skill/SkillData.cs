using System.Collections.Generic;
using UnityEngine;

public enum SkillKind { Active, Passive }
public enum SkillEffect { SpinPunch, FireStorm, StatBoost }
public enum StatType { MaxHP, Damage, MoveSpeed }

// ข้อมูลสกิล 1 ตัว เก็บเป็นไฟล์ .asset — เพื่อนเพิ่มสกิลใหม่ได้โดยสร้างไฟล์ ไม่ต้องแก้โค้ด
[CreateAssetMenu(fileName = "SkillData", menuName = "BoatVsBoss/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName = "Skill";
    [TextArea] public string description;
    public Sprite icon;
    public SkillKind kind;
    public SkillEffect effect;

    [Header("Level")]
    public int maxLevel = 5;
    public float baseValue = 10f;      // ค่าที่เลเวล 1 (ดาเมจ หรือ % สถานะ)
    public float valuePerLevel = 5f;   // เพิ่มต่อเลเวล

    [Header("Active")]
    public float cooldown = 3f;
    public float radius = 3f;
    public int targetCount = 3;

    [Header("Passive (StatBoost)")]
    public StatType statType;

    [Header("Skill Tree (แบบ Skill.nextSkills ใน week08)")]
    public bool isStartAvailable = true;           // เหมือน attack.isAvailable = true
    public List<SkillData> nextSkills = new List<SkillData>();

    // สร้างตัวสกิลจริงตาม effect (Polymorphism: ได้คลาสลูกต่างกัน แต่ใช้ผ่าน Skill เหมือนกัน)
    public Skill CreateSkill()
    {
        switch (effect)
        {
            case SkillEffect.SpinPunch: return new SpinPunchSkill(this);
            case SkillEffect.FireStorm: return new FireStormSkill(this);
            default: return new StatPassiveSkill(this);
        }
    }
}
