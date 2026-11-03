using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ตัวละครที่ผู้เล่นคุม — สืบทอดจาก Character (Inheritance)
[RequireComponent(typeof(CharacterController))]
public class Player : Character
{
    [Header("Player")]
    [SerializeField] private float turnSpeed = 720f; // องศาต่อวินาที
    [SerializeField] private float gravity = -20f;

    [Header("Model")]
    [SerializeField] private Animator animator; // Animator ของโมเดล Boat (ลูกของ Player)

    [Header("Stats")]
    [SerializeField] private PlayerStats stats = new PlayerStats();
    public PlayerStats Stats => stats;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;   // ระยะฟันข้างหน้า
    [SerializeField] private float attackRadius = 1.2f;  // ความกว้างของวงฟัน
    [SerializeField] private float windUpTime = 0.15f;   // ใช้ตอนไม่มีโมเดล (แคปซูล)

    [Header("Combo 1-2-3")]
    [SerializeField] private float comboResetTime = 0.5f;                      // เว้นนานกว่านี้ คอมโบเริ่มท่า 1 ใหม่
    [SerializeField] private float[] comboDamage = { 1f, 1f, 1.5f };          // ท่า 3 แรงกว่า
    // จังหวะเป็นสัดส่วนของความยาวท่า: ง้าง -> หมัดโดน (hitPoint) -> ค้าง -> ต่อท่าถัดไปได้ (chainPoint)
    [SerializeField] private float[] hitPoint = { 0.35f, 0.4f, 0.45f };
    [SerializeField] private float[] chainPoint = { 0.65f, 0.65f, 0.85f };    // ท่า 3 ค้างนานกว่า
    [SerializeField] private float clickBufferTime = 0.6f;                     // คลิกที่เก่าเกินนี้ทิ้ง
    // คิวคลิกที่รอต่อย (Queue แบบ autoMoveQueue ใน ActionHistoryManager week09) เก็บเวลาที่คลิก
    private Queue<float> comboQueue = new Queue<float>();
    private int comboStep;          // 0 = ท่า 1, 1 = ท่า 2, 2 = ท่า 3
    private float lastComboEndTime = -99f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction attackAction;
    private float verticalVelocity;
    private bool isAttacking;

    [Header("Skills")]
    [SerializeField] private List<SkillData> startingSkills = new List<SkillData>(); // สกิลที่มีตั้งแต่เริ่ม (ไว้ทดสอบ)
    // ชื่อสกิล -> สกิล (Dictionary แบบ Inventory ใน week04)
    private Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
    // ช่องสกิลเรียงตามลำดับที่ได้ (ช่อง 1, 2, 3 = กดปุ่ม 1, 2, 3)
    private List<Skill> skillSlots = new List<Skill>();
    public IReadOnlyList<Skill> SkillSlots => skillSlots;
    public IEnumerable<Skill> Skills => skills.Values;
    public event System.Action OnSkillsChanged;

    [Header("Hurt")]
    [SerializeField] private float invincibleTime = 0.6f; // หลังโดนตี อมตะสักพัก ไม่ให้โดนรัวจนตายทันที
    private float lastHurtTime = -99f;

    public override void TakeDamage(float damage)
    {
        if (Time.time - lastHurtTime < invincibleTime) return;
        lastHurtTime = Time.time;
        base.TakeDamage(damage);
        if (isAlive && animator != null) animator.SetTrigger("Hit");
    }

    public override void SetUP()
    {
        base.SetUP();
        controller = GetComponent<CharacterController>();
        // ดึง action จาก InputSystem_Actions แบบเดียวกับ OOPPlayer ในคลาส
        moveAction = InputSystem.actions.FindAction("Move");
        if (moveAction == null)
        {
            Debug.LogError("ไม่พบ Input Action ชื่อ Move");
        }
        attackAction = InputSystem.actions.FindAction("Attack"); // คลิกซ้าย
        stats.Refresh();

        for (int i = 0; i < startingSkills.Count; i++)
        {
            AddSkill(startingSkills[i]);
        }
    }

    private void Update()
    {
        if (!isAlive || moveAction == null) return;

        // ระหว่างฟันยืนนิ่ง จะได้ฟันตรงทิศที่หันอยู่
        Vector2 input = isAttacking ? Vector2.zero : moveAction.ReadValue<Vector2>();
        Move(input);

        // 1. คลิกแล้วเข้าคิว (จำไว้สูงสุด 2 ครั้ง กดรัวระหว่างต่อยก็ไม่หาย)
        if (attackAction != null && attackAction.triggered && comboQueue.Count < 2)
        {
            comboQueue.Enqueue(Time.time);
        }
        // 2. ต่อยเสร็จแล้ว ดึงคลิกถัดไปออกจากคิว
        if (!isAttacking && comboQueue.Count > 0)
        {
            float clickTime = comboQueue.Dequeue();
            if (Time.time - clickTime <= clickBufferTime)
            {
                StartCoroutine(ComboAttack());
            }
        }
        UpdateSkillInput();
    }

    // ต่อย 1 ท่าในคอมโบ: ง้างช้า -> ต่อยเร็ว (โดนตรงนี้) -> ค้าง -> คืนท่า
    private IEnumerator ComboAttack()
    {
        if (Time.time - lastComboEndTime > comboResetTime)
        {
            comboStep = 0; // เว้นนาน เริ่มคอมโบใหม่
        }
        int step = comboStep;
        isAttacking = true;

        // ความยาวท่า (วินาที) — ไม่มีโมเดลใช้ค่าประมาณ
        float length = windUpTime / hitPoint[step];
        if (animator != null)
        {
            // สั่งเล่นท่าตรง ๆ ตั้งแต่ต้น ไม่ผ่าน Trigger จะได้ไม่รีเซ็ตกลางท่าเวลาคลิกรัว
            string stateName = "Combo" + (step + 1);
            animator.CrossFadeInFixedTime(stateName, 0.05f, 0, 0f);
            yield return null; // รอ 1 เฟรมให้ Animator เริ่มท่า
            AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(0);
            if (!info.IsName(stateName)) info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0.05f) length = info.length;
        }

        yield return new WaitForSeconds(length * hitPoint[step]);
        Attack(comboDamage[step]);
        yield return new WaitForSeconds(length * (chainPoint[step] - hitPoint[step]));

        comboStep = (step + 1) % 3; // 1 -> 2 -> 3 -> 1
        lastComboEndTime = Time.time;
        isAttacking = false;
    }

    public void Attack()
    {
        Attack(1f);
    }

    // หา Character ทุกตัวในวงหน้าผู้เล่น แล้วสั่ง TakeDamage (Polymorphism: ศัตรูแต่ละแบบรับดาเมจต่างกันได้)
    public void Attack(float damageMultiplier)
    {
        Vector3 center = transform.position + transform.forward * attackRange;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Character target = hits[i].GetComponent<Character>();
            if (target != null && target != this && target.IsAlive)
            {
                DealDamage(target, stats.Damage * damageMultiplier);
            }
        }
    }

    // ทุกการโจมตี (ตีปกติ/สกิล) ผ่านตรงนี้ ตีตายแล้วได้ EXP
    public void DealDamage(Character target, float amount)
    {
        target.TakeDamage(amount);
        if (!target.IsAlive)
        {
            stats.AddExp(target.ExpReward);
        }
    }

    // หาเป้าหมายทุกตัวในวงรอบผู้เล่น (ใช้กับสกิล)
    public List<Character> FindTargetsInRadius(float radius)
    {
        List<Character> result = new List<Character>();
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        for (int i = 0; i < hits.Length; i++)
        {
            Character c = hits[i].GetComponent<Character>();
            if (c != null && c != this && c.IsAlive && !result.Contains(c))
            {
                result.Add(c);
            }
        }
        return result;
    }

    #region Skill
    // ได้สกิลหรืออัปเลเวล — หลักเดียวกับ Inventory.AddItem (week04)
    public void AddSkill(SkillData skillData)
    {
        if (skillData == null) return;

        // 1. ตรวจสอบว่ามีสกิลนี้แล้วหรือยัง
        if (skills.ContainsKey(skillData.skillName))
        {
            skills[skillData.skillName].LevelUp(this);
        }
        else
        {
            // ถ้ายังไม่มี ให้สร้างสกิลใหม่แล้วเพิ่มเข้า Dictionary
            Skill newSkill = skillData.CreateSkill();
            skills.Add(skillData.skillName, newSkill);
            newSkill.OnAdded(this);
            skillSlots.Add(newSkill);
            Debug.Log("Got new skill " + skillData.skillName);
        }
        OnSkillsChanged?.Invoke();
    }

    public bool HasSkill(string skillName)
    {
        return skills.ContainsKey(skillName);
    }

    public int GetSkillLevel(string skillName)
    {
        if (skills.ContainsKey(skillName))
        {
            return skills[skillName].Level;
        }
        return 0;
    }

    // Skill Tree: เลือกได้ถ้าเป็นสกิลเริ่มต้น หรือสกิลที่มีอยู่มี nextSkills ชี้มาหา (แบบ Unlock() ใน week08)
    public bool IsSkillAvailable(SkillData skillData)
    {
        if (skillData.isStartAvailable) return true;
        foreach (var owned in skills.Values)
        {
            if (owned.Data.nextSkills.Contains(skillData)) return true;
        }
        return false;
    }

    public void PrintSkills()
    {
        Debug.Log("--- Skills ---");
        foreach (var entry in skills)
        {
            Debug.Log(entry.Key + " Lv." + entry.Value.Level);
        }
    }

    // เพิ่มค่าสถานะ — UpgradeManager เรียกตอนเลือก Stat Boost, PassiveSkill ก็เรียก
    public void ApplyStatBoost(StatType type, float value, bool isPercent)
    {
        switch (type)
        {
            case StatType.MaxHP:
                float addHP = isPercent ? maxHP * value : value;
                maxHP += addHP;
                currentHP += addHP;
                RaiseHPChanged();
                break;
            case StatType.Damage:
                stats.AddDamage(isPercent ? stats.Damage * value : value);
                break;
            case StatType.MoveSpeed:
                moveSpeed += isPercent ? moveSpeed * value : value;
                break;
        }
        Debug.Log($"Stat boost {type} +{value}{(isPercent ? " (x%)" : "")}");
    }

    // Passive ทำงานเองอยู่แล้ว กดไม่มีผล
    private void UseSkillSlot(int index)
    {
        if (index >= skillSlots.Count) return;
        if (skillSlots[index] is ActiveSkill active && active.TryActivate(this))
        {
            if (animator != null) animator.SetTrigger("Skill");
        }
    }

    private void UpdateSkillInput()
    {
        foreach (var skill in skills.Values)
        {
            skill.Tick(this);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        // ปุ่ม 1 / 2 / 3 = ช่องสกิล 1 / 2 / 3 (อ่านคีย์แบบ keyboard.qKey ใน OOPPlayer)
        if (keyboard.digit1Key.wasPressedThisFrame) UseSkillSlot(0);
        if (keyboard.digit2Key.wasPressedThisFrame) UseSkillSlot(1);
        if (keyboard.digit3Key.wasPressedThisFrame) UseSkillSlot(2);
        if (keyboard.pKey.wasPressedThisFrame) PrintSkills();
    }
    #endregion

    // วาดวงฟันในหน้าต่าง Scene ไว้ปรับระยะ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, attackRadius);
    }

    private void Move(Vector2 input)
    {
        // WASD เป็น 2D (x,y) แต่เกม 3D เดินบนพื้น (x,z)
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // แรงโน้มถ่วง ให้ติดพื้นเสมอ
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        if (animator != null) animator.SetFloat("Speed", direction.magnitude);

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    protected override void Die()
    {
        base.Die();
        // เล่นท่าตาย (ถ้ายังไม่มีโมเดลก็ล้มแคปซูลแทน) ภายหลัง GameManager จะฟัง OnDied แล้วเปิดหน้า Game Over
        if (animator != null) animator.SetBool("Dead", true);
        else transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
    }
}
