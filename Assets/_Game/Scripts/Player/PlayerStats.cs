using System;
using UnityEngine;

// ค่าสถานะของผู้เล่น: Damage, EXP, Level — Player เป็นเจ้าของ (Composition)
[Serializable]
public class PlayerStats
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private int level = 1;
    [SerializeField] private int exp = 0;
    [Tooltip("EXP ที่ต้องใช้ต่อเลเวล: เลเวล 1 ต้องใช้ baseExpToLevel, เลเวล 2 ใช้ x2 ...")]
    [SerializeField] private int baseExpToLevel = 30;

    public float Damage => damage;
    public int Level => level;
    public int Exp => exp;
    public int ExpToNextLevel => baseExpToLevel * level;

    public event Action<int> OnLevelUp;          // ส่งเลเวลใหม่
    public event Action<int, int> OnExpChanged;  // (exp ตอนนี้, exp ที่ต้องใช้)

    public void AddExp(int amount)
    {
        if (amount <= 0) return;

        exp += amount;
        // while: ได้ EXP เยอะ ๆ ทีเดียวอาจขึ้นหลายเลเวล
        while (exp >= ExpToNextLevel)
        {
            exp -= ExpToNextLevel;
            level++;
            Debug.Log("Level Up! Level " + level);
            OnLevelUp?.Invoke(level);
        }
        OnExpChanged?.Invoke(exp, ExpToNextLevel);
    }

    public void AddDamage(float amount)
    {
        damage += amount;
    }

    // ให้ UI ได้ค่าเริ่มต้นตอนเปิดเกม
    public void Refresh()
    {
        OnExpChanged?.Invoke(exp, ExpToNextLevel);
    }
}
