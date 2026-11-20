using System.Collections.Generic;
using UnityEngine;

// จัดการการ์ดอัปเกรดตอนผู้เล่นเลเวลอัป ทั้งการสุ่มและนำการ์ดไปใช้
// อยู่ที่ UpgradeSystem ในฉาก Gameplay
public class UpgradeManager : MonoBehaviour
{
    // รายชื่อการ์ดอัปเกรดทั้งหมด ลากไฟล์ UpgradeData (Data/Upgrades) มาใส่ใน Inspector
    [SerializeField] private List<UpgradeData> allUpgrades = new List<UpgradeData>();

    // เก็บการ์ดอัปเกรดโดยใช้ Key (ชื่อไฟล์การ์ด) เป็นตัวค้นหา เพื่อป้องกันการเพิ่มการ์ดซ้ำ
    private Dictionary<string, UpgradeData> pool = new Dictionary<string, UpgradeData>();

    public int PoolCount => pool.Count; // จำนวนการ์ดทั้งหมดที่เก็บอยู่ในระบบ

    private void Awake()
    {
        SetUP();
    }

    // เริ่มต้นระบบอัปเกรดและรวบรวมการ์ดทั้งหมด
    public void SetUP()
    {
        BuildPool();
    }

    // นำการ์ดจาก allUpgrades มาเก็บไว้ใน pool (เหมือน AddItem ใน Inventory)
    public void BuildPool()
    {
        pool.Clear(); // ล้างข้อมูลเดิมก่อนสร้างรายการใหม่
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeData upgrade = allUpgrades[i];
            if (upgrade == null) continue; // ข้ามช่องที่ยังไม่ได้ใส่การ์ด
            // ถ้ามีการ์ดที่ใช้ Key ซ้ำกัน จะไม่เพิ่มใบที่ซ้ำเข้าระบบ
            if (pool.ContainsKey(upgrade.Key))
            {
                Debug.LogWarning("อัปเกรดซ้ำ: " + upgrade.Key);
                continue;
            }
            pool.Add(upgrade.Key, upgrade);
        }
        Debug.Log("Upgrade pool: " + pool.Count + " ตัว");
    }

    // ตรวจสอบว่าการ์ดใบนี้สามารถเลือกได้หรือไม่
    // การ์ดเพิ่มค่าสถานะเลือกได้ตลอด ส่วนการ์ดสกิลต้องปลดล็อกแล้วและเลเวลยังไม่เต็ม
    public bool CanPick(UpgradeData upgrade, Player player)
    {
        if (upgrade.type == UpgradeType.Stat) return true;
        if (upgrade.skill == null) return false; // ถ้าไม่มีข้อมูลสกิลจะเลือกไม่ได้
        if (!player.IsSkillAvailable(upgrade.skill)) return false; // สกิลยังไม่ปลดล็อกจะเลือกไม่ได้
        // เลือกสกิลได้ถ้าเลเวลปัจจุบันยังไม่ถึงเลเวลสูงสุด
        return player.GetSkillLevel(upgrade.skill.skillName) < upgrade.skill.maxLevel;
    }

    // สุ่มการ์ดอัปเกรดให้ผู้เล่น โดยไม่ให้ได้การ์ดใบเดิมซ้ำในรอบเดียวกัน
    public List<UpgradeData> GetOptions(Player player, int count)
    {
        List<UpgradeData> candidates = new List<UpgradeData>();
        // เลือกเฉพาะการ์ดที่ผู้เล่นสามารถเลือกได้
        foreach (UpgradeData upgrade in pool.Values)
        {
            if (CanPick(upgrade, player)) candidates.Add(upgrade);
        }

        List<UpgradeData> options = new List<UpgradeData>();
        // สุ่มการ์ดจากรายการที่เลือกได้ จนครบจำนวนที่ต้องการหรือไม่มีการ์ดเหลือ
        while (options.Count < count && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            options.Add(candidates[index]);
            candidates.RemoveAt(index); // เอาการ์ดที่สุ่มได้ออก เพื่อไม่ให้สุ่มซ้ำ
        }
        return options;
    }

    // นำการ์ดที่ผู้เล่นเลือกไปเพิ่มค่าสถานะหรือเพิ่มสกิล
    // ถ้าต้องการเพิ่มการ์ดใหม่ ให้สร้างไฟล์ UpgradeData แล้วนำมาใส่ใน allUpgrades
    public void ApplyUpgrade(UpgradeData upgrade, Player player)
    {
        if (upgrade == null || player == null) return; // ตรวจสอบว่ามีข้อมูลการ์ดและผู้เล่นก่อน
        switch (upgrade.type)
        {
            case UpgradeType.Stat:
                // ถ้าเป็นการ์ดเพิ่มค่าสถานะ จะส่งค่าไปให้ Player จัดการ
                player.ApplyStatBoost(upgrade.statType, upgrade.value, upgrade.isPercent);
                break;
            case UpgradeType.Skill:
                // ถ้าเป็นการ์ดสกิล จะเพิ่มสกิลหรือเพิ่มเลเวลสกิลที่มีอยู่แล้ว
                player.AddSkill(upgrade.skill);
                break;
        }
        Debug.Log("เลือกอัปเกรด " + upgrade.DisplayName);
    }
}