using UnityEngine;

// คลาสหลักของสกิล ใช้เป็นต้นแบบให้สกิลแบบกดใช้และสกิลติดตัว
// คลาสนี้ไม่ได้ติดอยู่กับ GameObject ใน Unity
public abstract class Skill
{
    protected SkillData data; // เก็บข้อมูลของสกิล เช่น ชื่อ เลเวลสูงสุด และค่าพลัง
    protected int level; // เลเวลปัจจุบันของสกิล

    // ใช้เรียกดูข้อมูลสกิลจากคลาสอื่น
    public SkillData Data => data;
    public string Name => data.skillName;
    public int Level => level;
    public bool IsMaxLevel => level >= data.maxLevel;

    // ตั้งค่าข้อมูลสกิลและกำหนดให้เริ่มต้นที่เลเวล 1
    protected Skill(SkillData data)
    {
        this.data = data;
        level = 1;
    }

    // คำนวณค่าของสกิลตามเลเวลปัจจุบัน
    // ถ้าต้องการปรับค่าพลังเริ่มต้นหรือค่าที่เพิ่มต่อเลเวล ให้ไปแก้ใน SkillData
    public float Value()
    {
        return data.baseValue + data.valuePerLevel * (level - 1);
    }

    // เพิ่มเลเวลสกิล ถ้าเลเวลเต็มแล้วจะไม่สามารถเพิ่มได้อีก
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

    // ทำงานตอนผู้เล่นได้รับสกิลครั้งแรก
    // สกิลลูกสามารถเขียนเพิ่มได้ ถ้าต้องการให้มีผลตอนเก็บสกิล
    public virtual void OnAdded(Player player) { }

    // ทำงานทุกเฟรม ใช้กับสกิลที่ต้องตรวจสอบหรือทำงานต่อเนื่อง
    public virtual void Tick(Player player) { }
}

// คลาสสำหรับสกิลที่ผู้เล่นต้องกดใช้ และมีเวลาคูลดาวน์
public abstract class ActiveSkill : Skill
{
    private float lastUseTime = -999f; // เวลาที่ใช้สกิลครั้งล่าสุด

    protected ActiveSkill(SkillData data) : base(data) { }

    // เวลาคูลดาวน์ที่เหลืออยู่ และเช็กว่าสกิลพร้อมใช้หรือยัง
    public float CooldownRemaining => Mathf.Max(0f, data.cooldown - (Time.time - lastUseTime));
    public bool IsReady => CooldownRemaining <= 0f;

    // ตรวจสอบว่าสกิลพร้อมใช้หรือยัง ถ้าพร้อมจะเริ่มใช้สกิล
    public bool TryActivate(Player player)
    {
        if (!IsReady)
        {
            Debug.Log(Name + " cooldown " + CooldownRemaining.ToString("0.0") + "s");
            return false;
        }
        lastUseTime = Time.time;
        player.CastSkill(this); // ให้ Player เล่นแอนิเมชันก่อน แล้วค่อยทำงานตามจังหวะที่กำหนด
        return true;
    }

    // เรียกใช้ตอนถึงจังหวะที่กำหนดในแอนิเมชัน
    // ทำงานของสกิลและสร้างเอฟเฟกต์ออกมา
    public void Execute(Player player)
    {
        Activate(player);
        PlayVFX(player);
    }

    // สร้างเอฟเฟกต์ของสกิล โดยใช้ตำแหน่งและการหมุนของผู้เล่น
    private void PlayVFX(Player player)
    {
        Transform t = player.transform;

        // ปรับตำแหน่งเอฟเฟกต์ได้ใน SkillData
        // vfxForward = ระยะด้านหน้า, vfxSide = ระยะด้านข้าง, vfxHeight = ความสูง
        Vector3 position = t.position + t.forward * data.vfxForward + t.right * data.vfxSide + Vector3.up * data.vfxHeight;

        // ถ้ากำหนดกระดูกของตัวละครไว้ จะใช้ตำแหน่งจากกระดูกนั้นแทน
        Transform bone = player.GetBone(data.vfxBone);
        if (bone != null) position = bone.position + t.forward * data.vfxForward;

        // กำหนดทิศทางการหมุนของเอฟเฟกต์
        // ถ้าต้องการเปลี่ยนมุมเอฟเฟกต์ ให้ปรับ vfxRotation ใน SkillData
        Vector3 r = data.vfxRotation;
        Quaternion rotation = t.rotation * Quaternion.Euler(0f, r.y, 0f) * Quaternion.Euler(r.x, 0f, r.z);

        // เรียก VFXManager ให้สร้างเอฟเฟกต์ตามค่าที่ตั้งไว้ใน SkillData
        VFXManager.Play(data.vfxPrefab, position, rotation, data.vfxScale, data.vfxTime, data.vfxFollow ? t : null);
    }

    // ให้คลาสของสกิลแต่ละตัวเขียนวิธีทำงานของตัวเอง
    protected abstract void Activate(Player player);
}

// คลาสสำหรับสกิลติดตัว ไม่ต้องกดใช้เอง
public abstract class PassiveSkill : Skill
{
    protected PassiveSkill(SkillData data) : base(data) { }

    // เมื่อได้รับสกิลครั้งแรก ให้เพิ่มผลของสกิลทันที
    public override void OnAdded(Player player)
    {
        Apply(player, Value());
    }

    // เมื่อเพิ่มเลเวล ให้เพิ่มเฉพาะค่าที่ได้เพิ่มขึ้นจากเลเวลก่อนหน้า
    public override void LevelUp(Player player)
    {
        if (IsMaxLevel) return;
        float before = Value();
        base.LevelUp(player);

        // คำนวณเฉพาะส่วนต่าง เพื่อไม่ให้ผลของเลเวลเก่าถูกบวกซ้ำ
        Apply(player, Value() - before);
    }

    // แต่ละสกิลติดตัวจะกำหนดผลที่เพิ่มให้ผู้เล่นเอง
    protected abstract void Apply(Player player, float amount);
}
