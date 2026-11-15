using System.Collections.Generic;
using UnityEngine;

// สุ่มการ์ดตอนเลเวลอัป แล้วเอาไปใช้กับผู้เล่น
public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private List<UpgradeData> allUpgrades = new List<UpgradeData>(); // ลากการ์ดใส่
    // Dictionary แบบ Inventory
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

    // ใส่ทุกใบลง Dictionary ซ้ำไม่ใส่ (ContainsKey แบบ AddItem)
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

    // Stat เลือกได้ตลอด Skill ต้องปลดล็อกแล้วและยังไม่ตัน
    public bool CanPick(UpgradeData upgrade, Player player)
    {
        if (upgrade.type == UpgradeType.Stat) return true;
        if (upgrade.skill == null) return false;
        if (!player.IsSkillAvailable(upgrade.skill)) return false;
        return player.GetSkillLevel(upgrade.skill.skillName) < upgrade.skill.maxLevel;
    }

    // สุ่มไม่ซ้ำ
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
            candidates.RemoveAt(index); // หยิบแล้วเอาออก
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
                player.AddSkill(upgrade.skill); // AddSkill จัดการเองว่าได้ใหม่หรืออัปเลเวล
                break;
        }
        Debug.Log("เลือกอัปเกรด " + upgrade.DisplayName);
    }
}
