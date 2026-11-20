using System;
using UnityEngine;

// คลาสแม่ของตัวละครทุกตัว เหมือน Character ใน week3
public abstract class Character : MonoBehaviour
{
    [Header("Character")]
    public string characterName;
    [SerializeField] protected float maxHP = 100f; //เปลี่ยนเลือดสูงสุดให้แก้ใน Inspector
    [SerializeField] protected float moveSpeed = 5f; //เปลี่ยนความเร็วในการเดินให้แก้ใน inspector

    protected float currentHP; // เลือดที่เหลืออยู่ตอนนี้
    protected bool isAlive; // เช็กว่าตัวละครยังมีชีวิตอยู่ไหม

    // เอาไว้ให้สคริปต์อื่นอ่านค่าพวกนี้ไปใช้
    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsAlive => isAlive;
    // EXP ที่ได้ตอนฆ่า Enemy โดยคลาสลูกสามารถ override ได้
    public virtual int ExpReward => 0;

    // Event แจ้งเมื่อเสียชีวิต และเมื่อเลือดเปลี่ยน
    public event Action<Character> OnDied;
    public event Action<float, float> OnHPChanged;

    protected virtual void Start()
    {
        SetUP(); // ตั้งค่าเริ่มต้นตอนเริ่มเกม
    }

    public virtual void SetUP()
    {
        isAlive = true;
        currentHP = maxHP; // เริ่มเกมด้วยเลือดเต็ม
        OnHPChanged?.Invoke(currentHP, maxHP); // แจ้ง UI ให้แสดงเลือด
    }

    public virtual void TakeDamage(float damage)
    {
        // ถ้าตายแล้วหรือดาเมจไม่ถูกต้อง จะไม่รับดาเมจ
        if (!isAlive || damage <= 0f) return;

        currentHP -= damage; // หักเลือดตามดาเมจที่ได้รับ
        if (currentHP < 0f) currentHP = 0f; // ไม่ให้เลือดติดลบ

        Debug.Log(characterName + " HP : " + currentHP + "/" + maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP); // แจ้ง UI ให้อัปเดตหลอดเลือด
        CheckDead(); // ดูว่าเลือดหมดหรือยัง
    }

    // ให้ลูกเรียกได้ เพราะ event เรียกนอกคลาสไม่ได้
    protected void RaiseHPChanged()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public void Heal(float healPoint)
    {
        // ถ้าตายแล้วหรือค่าฟื้นเลือดไม่ถูกต้อง จะไม่ฟื้นเลือด
        if (!isAlive || healPoint <= 0f) return;

        currentHP += healPoint; // เพิ่มเลือด
        if (currentHP > maxHP) currentHP = maxHP; // เลือดห้ามเกินค่าสูงสุด
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    // เช็กว่าเลือดหมดหรือยัง ถ้าหมดให้เรียก Die()
    protected void CheckDead()
    {
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    // ทำงานเมื่อตัวละครตาย
    protected virtual void Die()
    {
        isAlive = false;
        Debug.Log(characterName + " is Dead");
        OnDied?.Invoke(this);  // แจ้งระบบอื่นว่าตัวละครนี้ตายแล้ว
    }
}
