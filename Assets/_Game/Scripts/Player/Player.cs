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

    [Header("Stats")]
    [SerializeField] private PlayerStats stats = new PlayerStats();
    public PlayerStats Stats => stats;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;   // ระยะฟันข้างหน้า
    [SerializeField] private float attackRadius = 1.2f;  // ความกว้างของวงฟัน
    // จังหวะฟัน: ง้างช้า -> ฟันเร็ว (โดนตรงนี้) -> ค้าง -> คืนท่า
    [SerializeField] private float windUpTime = 0.15f;
    [SerializeField] private float recoverTime = 0.25f;

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

        if (attackAction != null && attackAction.triggered && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
        UpdateSkillInput();
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        yield return new WaitForSeconds(windUpTime);
        Attack();
        yield return new WaitForSeconds(recoverTime);
        isAttacking = false;
    }

    // หา Character ทุกตัวในวงหน้าผู้เล่น แล้วสั่ง TakeDamage (Polymorphism: ศัตรูแต่ละแบบรับดาเมจต่างกันได้)
    public void Attack()
    {
        Vector3 center = transform.position + transform.forward * attackRange;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Character target = hits[i].GetComponent<Character>();
            if (target != null && target != this && target.IsAlive)
            {
                DealDamage(target, stats.Damage);
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
        if (skillSlots[index] is ActiveSkill active)
        {
            active.TryActivate(this);
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

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    protected override void Die()
    {
        base.Die();
        // ล้มลงให้เห็นว่าตาย ภายหลัง GameManager จะฟัง OnDied แล้วเปิดหน้า Game Over
        transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
    }
}
