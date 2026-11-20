using System;
using UnityEngine;

// เก็บค่าสถานะของผู้เล่น เช่น ดาเมจ เลเวล และ EXP
// ค่าจริงแก้ใน Inspector ของ Player > Stats (แก้เลขในโค้ดไม่มีผล)
[Serializable]
public class PlayerStats
{
    [SerializeField] private float damage = 10f; // ดาเมจต่อยปกติ (ในเกมตั้งไว้ 18 ที่ Inspector)
    [SerializeField] private int level = 1; // เลเวลเริ่มต้น
    [SerializeField] private int exp = 0; // EXP ที่ผู้เล่นมีอยู่ตอนนี้
    [Tooltip("EXP ที่ต้องใช้ต่อเลเวล: เลเวล 1 ต้องใช้ baseExpToLevel, เลเวล 2 ใช้ x2 ...")]
    [SerializeField] private int baseExpToLevel = 30; // EXP พื้นฐานที่ต้องใช้ในการเพิ่มเลเวล

    // เปิดให้สคริปต์อื่นเรียกดูค่าสถานะของผู้เล่นได้
    public float Damage => damage;
    public int Level => level;
    public int Exp => exp;
    public int ExpToNextLevel => baseExpToLevel * level; // EXP ที่ต้องใช้เพื่อขึ้นเลเวลถัดไป (30, 60, 90 ...)

    // แจ้งสคริปต์อื่นเมื่อผู้เล่นเลเวลอัปหรือ EXP เปลี่ยน
    public event Action<int> OnLevelUp; // ส่งค่าเลเวลใหม่ออกไป
    public event Action<int, int> OnExpChanged; // ส่ง EXP ปัจจุบันและ EXP ที่ต้องใช้ไปให้ UI

    // เพิ่ม EXP ให้ผู้เล่น
    public void AddExp(int amount)
    {
        if (amount <= 0) return; // ถ้า EXP ที่ได้รับเป็น 0 หรือติดลบ จะไม่เพิ่ม

        exp += amount;
        // เช็กว่า EXP ถึงเกณฑ์เลเวลถัดไปหรือยัง
        // ใช้ while เพื่อให้สามารถเลเวลอัปได้หลายครั้ง ถ้าได้รับ EXP จำนวนมาก
        while (exp >= ExpToNextLevel)
        {
            exp -= ExpToNextLevel; // หัก EXP ที่ใช้ขึ้นเลเวลออก
            level++; // เพิ่มเลเวลผู้เล่น
            Debug.Log("Level Up! Level " + level);
            OnLevelUp?.Invoke(level); // แจ้งระบบอื่นว่าเลเวลเพิ่มแล้ว
        }
        // อัปเดตแถบ EXP ให้แสดงค่าปัจจุบัน
        OnExpChanged?.Invoke(exp, ExpToNextLevel);
    }

    // เพิ่มดาเมจให้ผู้เล่นตามจำนวนที่กำหนด
    public void AddDamage(float amount)
    {
        damage += amount;
    }

    // ส่งค่า EXP ปัจจุบันไปให้ UI ตอนเริ่มต้น
    public void Refresh()
    {
        OnExpChanged?.Invoke(exp, ExpToNextLevel);
    }
}

