using System.Collections.Generic;
using UnityEngine;

// สุ่มตัวเลือกอัปเกรดตอนเลเวลอัป แล้วใช้กับผู้เล่น (ตาม class diagram: pool, GetOptions, ApplyUpgrade)
public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private List<UpgradeData> allUpgrades = new List<UpgradeData>(); // ลากไฟล์ .asset ใส่ใน Inspector
    // ชื่อ -> อัปเกรด (Dictionary แบบ Inventory ใน week04)
    private Dictionary<string, UpgradeData> pool = new Dictionary<string, UpgradeData>();

    public int PoolCount => pool.Count;

    private void Awake()
    {
        SetUP();
    }

    public void SetUP()
    {
        BuildPool();
    }

    // ใส่อัปเกรดทุกตัวลง Dictionary (ตัวซ้ำไม่ใส่ เหมือน AddItem เช็ก ContainsKey ก่อน)
    public void BuildPool()
    {
        pool.Clear();
        for (int i = 0; i < allUpgrades.Count; i++)
        {
            UpgradeData upgrade = allUpgrades[i];
            if (upgrade == null) continue;
            if (pool.ContainsKey(upgrade.Key))
            {
                Debug.LogWarning("อัปเกรดซ้ำ: " + upgrade.Key);
                continue;
            }
            pool.Add(upgrade.Key, upgrade);
        }
        Debug.Log("Upgrade pool: " + pool.Count + " ตัว");
    }

    // เลือกได้ไหม: Stat ได้เสมอ, Skill ต้องปลดล็อกแล้ว (Skill Tree) และยังไม่เต็มเลเวล
    public bool CanPick(UpgradeData upgrade, Player player)
    {
        if (upgrade.type == UpgradeType.Stat) return true;
        if (upgrade.skill == null) return false;
        if (!player.IsSkillAvailable(upgrade.skill)) return false;
        return player.GetSkillLevel(upgrade.skill.skillName) < upgrade.skill.maxLevel;
    }

    // สุ่มตัวเลือกไม่ซ้ำกัน count ใบ จากตัวที่เลือกได้
    public List<UpgradeData> GetOptions(Player player, int count)
    {
        List<UpgradeData> candidates = new List<UpgradeData>();
        foreach (UpgradeData upgrade in pool.Values)
        {
            if (CanPick(upgrade, player)) candidates.Add(upgrade);
        }

        List<UpgradeData> options = new List<UpgradeData>();
        while (options.Count < count && candidates.Count > 0)
        {
            int index = Random.Range(0, candidates.Count);
            options.Add(candidates[index]);
            candidates.RemoveAt(index); // หยิบแล้วเอาออก จะได้ไม่ซ้ำ
        }
        return options;
    }

    public void ApplyUpgrade(UpgradeData upgrade, Player player)
    {
        if (upgrade == null || player == null) return;
        switch (upgrade.type)
        {
            case UpgradeType.Stat:
                player.ApplyStatBoost(upgrade.statType, upgrade.value, upgrade.isPercent);
                break;
            case UpgradeType.Skill:
                player.AddSkill(upgrade.skill); // มีแล้ว = อัปเลเวล, ยังไม่มี = ได้สกิลใหม่ (อยู่ใน AddSkill)
                break;
        }
        Debug.Log("เลือกอัปเกรด " + upgrade.DisplayName);
    }
}
