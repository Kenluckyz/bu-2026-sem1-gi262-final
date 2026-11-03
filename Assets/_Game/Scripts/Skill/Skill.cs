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

    // คืน true ถ้าใช้สำเร็จ (Player จะเล่นท่าสกิล)
    public bool TryActivate(Player player)
    {
        if (!IsReady)
        {
            Debug.Log($"{Name} cooldown {CooldownRemaining:0.0}s");
            return false;
        }
        lastUseTime = Time.time;
        player.CastSkill(this); // Player เล่นท่า แล้วเรียก Execute ตอนถึงจังหวะ hitPoint
        return true;
    }

    // จังหวะที่หมัด/เท้าลงจริง: ดาเมจ + เอฟเฟกต์
    public void Execute(Player player)
    {
        Activate(player);
        PlayVFX(player);
    }

    // ทุก ActiveSkill เล่นเอฟเฟกต์ของตัวเองจาก SkillData (ไม่ต้องเขียนซ้ำในคลาสลูก)
    private void PlayVFX(Player player)
    {
        Transform t = player.transform;
        Vector3 position = t.position + t.forward * data.vfxForward + t.right * data.vfxSide + Vector3.up * data.vfxHeight;
        // ออกจากมือ/เท้าที่ใช้ในท่าจริง ณ จังหวะ hitPoint
        Transform bone = player.GetBone(data.vfxBone);
        if (bone != null) position = bone.position + t.forward * data.vfxForward;
        // หันตามผู้เล่น -> หมุนซ้ายขวา (y) -> วางนอน/ตั้ง (x)
        Vector3 r = data.vfxRotation;
        Quaternion rotation = t.rotation * Quaternion.Euler(0f, r.y, 0f) * Quaternion.Euler(r.x, 0f, r.z);
        VFXManager.Play(data.vfxPrefab, position, rotation, data.vfxScale, data.vfxTime, data.vfxFollow ? t : null);
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
