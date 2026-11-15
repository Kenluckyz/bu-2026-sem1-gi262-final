using UnityEngine;

public enum UpgradeType { Stat, Skill }

// การ์ดอัปเกรด 1 ใบ เก็บเป็นไฟล์เหมือน SkillData
// Stat = เพิ่มค่า, Skill = ได้สกิล/อัปสกิล
[CreateAssetMenu(fileName = "UpgradeData", menuName = "BoatVsBoss/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    public string upgradeName; // ว่างได้ ถ้าเป็นสกิลใช้ชื่อสกิล
    [TextArea] public string description;
    public Sprite icon; // ว่างได้ ใช้ไอคอนสกิล
    public UpgradeType type;
    public Color cardColor = new Color(0.3f, 0.55f, 1f); // สีธง

    [Header("Stat")]
    public StatType statType;
    public float value = 10f;
    public bool isPercent; // 0.1 = 10%

    [Header("Skill")]
    public SkillData skill;

    // key ของ Dictionary
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
