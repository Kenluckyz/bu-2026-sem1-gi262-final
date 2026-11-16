using UnityEngine;

// คลาสแม่ของสกิล ไม่ใช่ MonoBehaviour
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

    // เลเวล 1 = baseValue แล้วบวกเพิ่มทีละเลเวล
    public float Value()
    {
        return data.baseValue + data.valuePerLevel * (level - 1);
    }

    public virtual void LevelUp(Player player)
    {
        if (IsMaxLevel)
        {
            Debug.Log("Skill " + Name + " is already max level.");
            return;
        }
        level++;
        Debug.Log("Skill " + Name + " level up -> " + level);
    }

    // ตอนได้สกิล
    public virtual void OnAdded(Player player) { }

    // ทุกเฟรม
    public virtual void Tick(Player player) { }
}

// สกิลกดใช้ มีคูลดาวน์
public abstract class ActiveSkill : Skill
{
    private float lastUseTime = -999f;

    protected ActiveSkill(SkillData data) : base(data) { }

    public float CooldownRemaining => Mathf.Max(0f, data.cooldown - (Time.time - lastUseTime));
    public bool IsReady => CooldownRemaining <= 0f;

    // ใช้ได้คืน true
    public bool TryActivate(Player player)
    {
        if (!IsReady)
        {
            Debug.Log(Name + " cooldown " + CooldownRemaining.ToString("0.0") + "s");
            return false;
        }
        lastUseTime = Time.time;
        player.CastSkill(this); // ให้ player เล่นท่าก่อน ถึงจังหวะแล้วค่อยเรียก Execute
        return true;
    }

    // ตอนหมัดโดน ดาเมจ + เอฟเฟกต์
    public void Execute(Player player)
    {
        Activate(player);
        PlayVFX(player);
    }

    // เอฟเฟกต์เขียนไว้ที่เดียว ลูกไม่ต้องเขียนซ้ำ
    private void PlayVFX(Player player)
    {
        Transform t = player.transform;
        Vector3 position = t.position + t.forward * data.vfxForward + t.right * data.vfxSide + Vector3.up * data.vfxHeight;
        // ออกจากมือ/เท้า
        Transform bone = player.GetBone(data.vfxBone);
        if (bone != null) position = bone.position + t.forward * data.vfxForward;
        // หมุนตามตัวผู้เล่น
        Vector3 r = data.vfxRotation;
        Quaternion rotation = t.rotation * Quaternion.Euler(0f, r.y, 0f) * Quaternion.Euler(r.x, 0f, r.z);
        VFXManager.Play(data.vfxPrefab, position, rotation, data.vfxScale, data.vfxTime, data.vfxFollow ? t : null);
    }

    // ลูกเขียนเอง
    protected abstract void Activate(Player player);
}

// สกิลติดตัว
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
        // บวกแค่ส่วนที่เพิ่ม ไม่งั้นบวกซ้ำ
        Apply(player, Value() - before);
    }

    protected abstract void Apply(Player player, float amount);
}
