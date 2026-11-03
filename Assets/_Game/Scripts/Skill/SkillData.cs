using System.Collections.Generic;
using UnityEngine;

public enum SkillKind { Active, Passive }
public enum SkillEffect { SpinPunch, FireStorm, StatBoost, DashStrike, OverPunch, FinalExecution, MetalHeart }
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
    public float coneAngle = 90f;      // ความกว้างกรวย (องศา) ใช้กับ OverPunch
    public string animState;           // ชื่อท่าใน Animator (เว้นว่าง = ท่าสกิลทั่วไป)
    [Range(0f, 1f)] public float hitPoint = 0f;   // ดาเมจ+เอฟเฟกต์ออกตอนท่าเล่นไปกี่ส่วน (0.4 = 40% ของท่า) แบบ hitPoint ของคอมโบ
    [Range(0f, 1f)] public float recoverPoint = 0.8f; // ยืนนิ่งจนท่าเล่นถึงตรงนี้ แล้วค่อยขยับได้

    [Header("VFX (Game Mellow)")]
    public GameObject vfxPrefab;       // prefab เอฟเฟกต์ 3D ในโฟลเดอร์ VFX (มี NFEffect)
    public float vfxScale = 1f;
    public float vfxTime = 0.6f;       // เล่นจบภายในกี่วินาที
    public float vfxForward = 0f;      // เลื่อนไปข้างหน้าผู้เล่นกี่เมตร
    public float vfxSide = 0f;         // เลื่อนไปทางขวาผู้เล่นกี่เมตร (ติดลบ = ซ้าย)
    public float vfxHeight = 0f;       // สูงจากพื้นกี่เมตร (ใช้ตอนไม่ได้เลือกกระดูก)
    // ให้เอฟเฟกต์ออกจากกระดูกนี้ในท่า เช่น มือขวาที่ต่อย (LastBone = ไม่ใช้ ใช้ตำแหน่งตัวแทน)
    public HumanBodyBones vfxBone = HumanBodyBones.LastBone;
    // หมุนเพิ่ม (องศา): y = หมุนซ้ายขวา (เอฟเฟกต์จาก Lab พุ่งไปแกน X เลยใช้ -90), x = วางนอน/ตั้ง
    public Vector3 vfxRotation = new Vector3(0f, -90f, 0f);
    public bool vfxFollow;             // ติดตามตัวผู้เล่น (เช่น Dash)

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
            case SkillEffect.DashStrike: return new DashStrikeSkill(this);
            case SkillEffect.OverPunch: return new OverPunchSkill(this);
            case SkillEffect.FinalExecution: return new FinalExecutionSkill(this);
            case SkillEffect.MetalHeart: return new MetalHeartSkill(this);
            default: return new StatPassiveSkill(this);
        }
    }
}
