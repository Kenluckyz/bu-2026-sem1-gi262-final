using System.Collections.Generic;
using UnityEngine;

// ประเภทของสกิล แบ่งเป็นสกิลกดใช้กับสกิลติดตัว
public enum SkillKind { Active, Passive }

// เลือกว่าสกิลนี้จะมีรูปแบบการทำงานแบบไหน
// ถ้าสร้างสกิลรูปแบบใหม่ ต้องเพิ่มชื่อไว้ตรงนี้ด้วย
public enum SkillEffect { SpinPunch, FireStorm, StatBoost, DashStrike, OverPunch, FinalExecution, MetalHeart }

// ประเภทค่าสถานะที่สกิลสามารถเพิ่มให้ผู้เล่นได้
public enum StatType { MaxHP, Damage, MoveSpeed }

// เก็บข้อมูลของสกิลแต่ละตัว สร้างไฟล์ข้อมูลสกิลใหม่ผ่าน Unity ได้
// ตัวเลขข้างล่างเป็นแค่ค่าเริ่มต้น ค่าจริงแก้ในไฟล์ของสกิลนั้น (Data/Skills/Dash.asset ฯลฯ)
[CreateAssetMenu(fileName = "SkillData", menuName = "BoatVsBoss/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName = "Skill"; // ชื่อสกิล ใช้เป็น key ใน Dictionary ห้ามซ้ำกัน
    [TextArea] public string description; // คำอธิบายสกิล
    public Sprite icon; // รูปไอคอนของสกิล
    public SkillKind kind; // ใช้โชว์บนการ์ดว่าเป็นดาเมจหรือโบนัส % (สกิลจะกดหรือติดตัวดูที่ effect)
    public SkillEffect effect; // เลือกรูปแบบการทำงานของสกิล

    [Header("Level")]
    public int maxLevel = 5; // เลเวลสูงสุดของสกิล
    public float baseValue = 10f; // ค่าพลังเริ่มต้นของสกิลตอนเลเวล 1
    public float valuePerLevel = 5f; // ค่าพลังที่เพิ่มขึ้นเมื่ออัปสกิลแต่ละเลเวล

    [Header("Active")]
    public float cooldown = 3f; // เวลาที่ต้องรอก่อนใช้สกิลซ้ำ เป็นวินาที
    public float radius = 3f; // ระยะ/รัศมีของสกิล
    public int targetCount = 3; // จำนวนศัตรูสูงสุดที่สกิลเลือกโจมตี (ใช้กับ FireStorm)
    public float coneAngle = 90f; // มุมโจมตีรูปกรวย 
    public float hitWidth = 2.4f; // ความกว้างของพื้นที่โจมตีสำหรับสกิล OverPunch
    public string animState; // ชื่อ Animation State ที่ต้องการให้เล่นใน Animator
    [Range(0f, 1f)] public float hitPoint = 0f; // จังหวะที่สกิลปล่อยดาเมจ เช่น 0.4 คือ 40% ของแอนิเมชัน
    [Range(0f, 1f)] public float recoverPoint = 0.8f; // จังหวะที่ผู้เล่นสามารถกลับมาเดินได้ระหว่างแอนิเมชัน

    [Header("VFX (Game Mellow)")]
    public GameObject vfxPrefab; // เลือกเอฟเฟกต์ที่จะใช้กับสกิล
    public float vfxScale = 1f; // ขนาดของเอฟเฟกต์
    public float vfxTime = 0.6f; // ระยะเวลาที่เอฟเฟกต์แสดง หน่วยเป็นวินาที
    public float vfxForward = 0f; // เลื่อนเอฟเฟกต์ไปด้านหน้าของตัวละคร
    public float vfxSide = 0f; // เลื่อนเอฟเฟกต์ไปด้านข้าง ค่าติดลบคือเลื่อนไปทางซ้าย
    public float vfxHeight = 0f; // ปรับความสูงของเอฟเฟกต์

    // เลือกว่าจะให้เอฟเฟกต์ออกจากกระดูกส่วนไหนของตัวละคร
    // LastBone หมายถึงไม่ได้กำหนดกระดูกให้เอฟเฟกต์
    public HumanBodyBones vfxBone = HumanBodyBones.LastBone;

    // ปรับองศาการหมุนของเอฟเฟกต์ ถ้าเอฟเฟกต์หันผิดทางให้ลองปรับตรงนี้
    public Vector3 vfxRotation = new Vector3(0f, -90f, 0f);
    public bool vfxFollow; // เปิดใช้งานถ้าต้องการให้เอฟเฟกต์เคลื่อนที่ตามตัวผู้เล่น

    [Header("Passive (StatBoost)")]
    public StatType statType; // เลือกว่าสกิลติดตัวจะเพิ่มเลือด ดาเมจ หรือความเร็ว

    [Header("Skill Tree (แบบ Skill.nextSkills ใน week08)")]
    public bool isStartAvailable = true; // กำหนดว่าสกิลนี้สามารถเลือกได้ตั้งแต่เริ่มเกมหรือไม่
    public List<SkillData> nextSkills = new List<SkillData>(); // รายชื่อสกิลที่สามารถปลดล็อกต่อจากสกิลนี้

    // สร้างคลาสสกิลให้ตรงกับ effect ที่เลือกไว้ด้านบน
    // ถ้าเพิ่มรูปแบบสกิลใหม่ ต้องเพิ่ม case ของสกิลนั้นตรงนี้ด้วย
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

