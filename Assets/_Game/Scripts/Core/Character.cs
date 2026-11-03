using System;
using UnityEngine;

// คลาสแม่ของตัวละครทุกตัว (Player, Enemy, Boss) — แนวเดียวกับ Character ใน workshop week03
public abstract class Character : MonoBehaviour
{
    [Header("Character")]
    public string characterName;
    [SerializeField] protected float maxHP = 100f;
    [SerializeField] protected float moveSpeed = 5f;

    protected float currentHP;
    protected bool isAlive;

    public float MaxHP => maxHP;
    public float CurrentHP => currentHP;
    public bool IsAlive => isAlive;
    // EXP ที่ผู้เล่นได้เมื่อฆ่าตัวนี้ (Enemy override ให้ดึงจาก EnemyData)
    public virtual int ExpReward => 0;

    // คนอื่นมาฟังได้ เช่น EnemyManager นับศัตรูที่ตาย, GameManager รู้ว่าผู้เล่นตาย
    public event Action<Character> OnDied;
    // ส่ง (เลือดตอนนี้, เลือดเต็ม) ให้ UI แถบเลือด
    public event Action<float, float> OnHPChanged;

    protected virtual void Start()
    {
        SetUP();
    }

    public virtual void SetUP()
    {
        isAlive = true;
        currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    public virtual void TakeDamage(float damage)
    {
        if (!isAlive || damage <= 0f) return;

        currentHP -= damage;
        if (currentHP < 0f) currentHP = 0f;
        Debug.Log(characterName + " HP : " + currentHP + "/" + maxHP);
        OnHPChanged?.Invoke(currentHP, maxHP);
        CheckDead();
    }

    public void Heal(float healPoint)
    {
        if (!isAlive || healPoint <= 0f) return;

        currentHP += healPoint;
        if (currentHP > maxHP) currentHP = maxHP;
        OnHPChanged?.Invoke(currentHP, maxHP);
    }

    protected void CheckDead()
    {
        if (currentHP <= 0f)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        isAlive = false;
        Debug.Log(characterName + " is Dead");
        OnDied?.Invoke(this);
    }
}
