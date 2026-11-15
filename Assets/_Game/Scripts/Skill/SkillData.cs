using System.Collections.Generic;
using UnityEngine;

public enum SkillKind { Active, Passive }
public enum SkillEffect { SpinPunch, FireStorm, StatBoost, DashStrike, OverPunch, FinalExecution, MetalHeart }
public enum StatType { MaxHP, Damage, MoveSpeed }

// ข้อมูลสกิล เก็บเป็นไฟล์ เพิ่มสกิลไม่ต้องแก้โค้ด
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
    public float baseValue = 10f; // ค่าเลเวล 1
    public float valuePerLevel = 5f; // เพิ่มต่อเลเวล

    [Header("Active")]
    public float cooldown = 3f;
    public float radius = 3f;
    public int targetCount = 3;
    public float coneAngle = 90f; // มุมกรวย (ไม่ได้ใช้แล้ว)
    public float hitWidth = 2.4f; // ความกว้างแถบของ OverPunch
    public string animState; // ชื่อท่าใน Animator
    [Range(0f, 1f)] public float hitPoint = 0f; // ปล่อยดาเมจตอนท่าไปกี่ส่วน 0.4 = 40%
    [Range(0f, 1f)] public float recoverPoint = 0.8f; // ยืนนิ่งถึงตรงนี้แล้วค่อยเดิน

    [Header("VFX (Game Mellow)")]
    public GameObject vfxPrefab; // เอฟเฟกต์
    public float vfxScale = 1f;
    public float vfxTime = 0.6f; // เล่นกี่วิ
    public float vfxForward = 0f; // เลื่อนหน้า
    public float vfxSide = 0f; // เลื่อนขวา (ลบ = ซ้าย)
    public float vfxHeight = 0f; // ความสูง
    // ให้ออกจากกระดูกไหน LastBone = ไม่ใช้
    public HumanBodyBones vfxBone = HumanBodyBones.LastBone;
    // หมุนเพิ่ม เอฟเฟกต์จาก Lab หันแกน X เลยต้อง -90
    public Vector3 vfxRotation = new Vector3(0f, -90f, 0f);
    public bool vfxFollow; // ตามตัวผู้เล่น

    [Header("Passive (StatBoost)")]
    public StatType statType;

    [Header("Skill Tree (แบบ Skill.nextSkills ใน week08)")]
    public bool isStartAvailable = true; // เหมือน isAvailable ใน week8
    public List<SkillData> nextSkills = new List<SkillData>();

    // สร้างสกิลตาม effect
    public Skill CreateSkill()
    {
        switch (effect)
        {
            case SkillEffect.SpinPunch: return new SpinPunchSkill(this);
            case SkillEffect.FireStorm: return new FireStormSkill(this);
            case SkillEffect.DashStrike: return new DashStrikeSkill(this);
            case SkillEffect.OverPunch: return new OverPunchSkill(this);
            case SkillEffect.FinalExecution: return new FinalExecutionSkill(this);
            case SkillEffect.MetalHeart: return new MetalHeartSkill(this);
            default: return new StatPassiveSkill(this);
        }
    }
}
