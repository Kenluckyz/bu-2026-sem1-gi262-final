using UnityEngine;

// คลาสแม่ของสกิล — คลาสธรรมดา (ไม่ใช่ MonoBehaviour) แบบ Skill ใน week08
public abstract class Skill
{
    protected SkillData data;
    protected int level;

    public SkillData Data => data;
    public string Name => data.skillName;
    public int Level => level;
    public bool IsMaxLevel => level >= data.maxLevel;

    protected Skill(SkillData data)
    {
        this.data = data;
        level = 1;
    }

    // ค่าตามเลเวล: เลเวล 1 = baseValue, เลเวลถัดไป + valuePerLevel
    public float Value()
    {
        return data.baseValue + data.valuePerLevel * (level - 1);
    }

    public virtual void LevelUp(Player player)
    {
        if (IsMaxLevel)
        {
            Debug.Log($"Skill {Name} is already max level.");
            return;
        }
        level++;
        Debug.Log($"Skill {Name} level up -> {level}");
    }

    // เรียกครั้งแรกตอนได้สกิล
    public virtual void OnAdded(Player player) { }

    // เรียกทุกเฟรมจาก Player
    public virtual void Tick(Player player) { }
}

// สกิลกดใช้ มี Cooldown
public abstract class ActiveSkill : Skill
{
    private float lastUseTime = -999f;

    protected ActiveSkill(SkillData data) : base(data) { }

    public float CooldownRemaining => Mathf.Max(0f, data.cooldown - (Time.time - lastUseTime));
    public bool IsReady => CooldownRemaining <= 0f;

    public void TryActivate(Player player)
    {
        if (!IsReady)
        {
            Debug.Log($"{Name} cooldown {CooldownRemaining:0.0}s");
            return;
        }
        lastUseTime = Time.time;
        Activate(player);
    }

    // แต่ละสกิลเขียนผลเอง
    protected abstract void Activate(Player player);
}

// สกิลติดตัว ทำงานเองตอนได้ และตอนเลเวลอัป
public abstract class PassiveSkill : Skill
{
    protected PassiveSkill(SkillData data) : base(data) { }

    public override void OnAdded(Player player)
    {
        Apply(player, Value());
    }

    public override void LevelUp(Player player)
    {
        if (IsMaxLevel) return;
        float before = Value();
        base.LevelUp(player);
        // ใส่เฉพาะส่วนที่เพิ่ม จะได้ไม่บวกทบซ้ำ
        Apply(player, Value() - before);
    }

    protected abstract void Apply(Player player, float amount);
}
