using System;
using UnityEngine;

// คลาสแม่ของตัวละครทุกตัว เหมือน Character ใน week3
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
    // EXP ที่ได้ตอนฆ่า Enemy ไป override
    public virtual int ExpReward => 0;

    // ตายแล้วบอกคนอื่น
    public event Action<Character> OnDied;
    // เลือดเปลี่ยนแล้วบอก UI
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

    // ให้ลูกเรียกได้ เพราะ event เรียกนอกคลาสไม่ได้
    protected void RaiseHPChanged()
    {
        OnHPChanged?.Invoke(currentHP, maxHP);
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
