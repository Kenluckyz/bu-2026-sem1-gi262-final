using UnityEngine;

public enum UpgradeType { Stat, Skill }

// ตัวเลือกอัปเกรด 1 ใบ เก็บเป็นไฟล์ .asset (ScriptableObject แบบ SkillData / ItemData ในคลาส)
// Stat = เพิ่มค่าสถานะ, Skill = ได้สกิลใหม่หรืออัปเลเวลสกิล
[CreateAssetMenu(fileName = "UpgradeData", menuName = "BoatVsBoss/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName;           // เว้นว่างได้ถ้าเป็น Skill จะใช้ชื่อสกิลแทน
    [TextArea] public string description;
    public Sprite icon;                  // เว้นว่างได้ถ้าเป็น Skill จะใช้ไอคอนสกิลแทน
    public UpgradeType type;

    [Header("Stat")]
    public StatType statType;
    public float value = 10f;
    public bool isPercent;               // true = value เป็นสัดส่วน (0.1 = 10%)

    [Header("Skill")]
    public SkillData skill;

    // ใช้เป็น key ใน Dictionary ของ UpgradeManager
    public string Key => name;

    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrEmpty(upgradeName)) return upgradeName;
            if (type == UpgradeType.Skill && skill != null) return skill.skillName;
            return name;
        }
    }

    public Sprite DisplayIcon
    {
        get
        {
            if (icon != null) return icon;
            if (type == UpgradeType.Skill && skill != null) return skill.icon;
            return null;
        }
    }
}
