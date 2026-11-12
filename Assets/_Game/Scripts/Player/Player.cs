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
    [SerializeField] private float attackMoveMultiplier = 0.9f;                // ระหว่างต่อยช้าลงแค่ 10%
    // คิวคลิกที่รอต่อย (Queue แบบ autoMoveQueue ใน ActionHistoryManager week09) เก็บเวลาที่คลิก
    private Queue<float> comboQueue = new Queue<float>();
    private int comboStep;          // 0 = ท่า 1, 1 = ท่า 2, 2 = ท่า 3
    private float lastComboEndTime = -99f;
    private Coroutine comboRoutine; // เก็บไว้หยุดคอมโบตอนกดสกิลกลางท่า

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

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f;     // เร็วกว่าเดินปกติมาก
    [SerializeField] private float dashTime = 0.35f;    // พุ่งนานแค่ไหน (วินาที) — คูลดาวน์อยู่ใน SkillData
    private bool isDashing;

    [Header("Dodge (Shift)")]
    [SerializeField] private float dodgeCooldown = 0.8f; // กลิ้งหลบด้วย Shift: ไม่มีดาเมจ แค่หลบ (คนละอย่างกับสกิล Dash)
    private float lastDodgeTime = -99f;
    [SerializeField] private Color afterImageColor = new Color(0.45f, 0.8f, 1f, 0.55f); // สีเงาร่างตอนหลบ
    [SerializeField] private float afterImageInterval = 0.05f; // ทิ้งเงาทุกกี่วินาที
    [SerializeField] private float afterImageLife = 0.35f;     // เงาจางหมดในกี่วินาที
    private bool isCasting; // กำลังเล่นท่าสกิล 2/3

    [Header("Hurt")]
    [SerializeField] private float invincibleTime = 0.6f; // หลังโดนตี อมตะสักพัก ไม่ให้โดนรัวจนตายทันที
    private float lastHurtTime = -99f;

    public override void TakeDamage(float damage)
    {
        if (isDashing) return; // ระหว่างพุ่งหลบ ไม่โดนดาเมจ
        if (Time.time - lastHurtTime < invincibleTime) return;
        lastHurtTime = Time.time;
        base.TakeDamage(damage);
        // ไม่เล่นท่าโดนตี (Hit) — Boat ไม่ชะงักเวลาโดนศัตรูตี เดิน/ต่อย/ใช้สกิลต่อได้เลย
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

        UpdateSkillInput();
        if (isDashing) return; // ระหว่างพุ่ง Coroutine คุมการเคลื่อนที่เอง
        if (isCasting)
        {
            Move(Vector2.zero); // ระหว่างท่าสกิลยืนนิ่ง ท่ากับตัวจะได้ไปด้วยกัน
            return;
        }

        // ระหว่างต่อยยังเดินได้แต่ช้าลง จะได้ถอยหนีศัตรูได้ ไม่ยืนนิ่งให้โดนรุม
        Vector2 input = moveAction.ReadValue<Vector2>();
        Move(isAttacking ? input * attackMoveMultiplier : input);

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
                comboRoutine = StartCoroutine(ComboAttack());
            }
        }
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
            // เลเยอร์ 1 = UpperBody ต่อยแค่ตัวบน ขายังเดินได้
            animator.CrossFadeInFixedTime(stateName, 0.05f, 1, 0f);
            yield return null; // รอ 1 เฟรมให้ Animator เริ่มท่า
            AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(1);
            if (!info.IsName(stateName)) info = animator.GetCurrentAnimatorStateInfo(1);
            if (info.length > 0.05f) length = info.length;
        }

        yield return new WaitForSeconds(length * hitPoint[step]);
        Attack(comboDamage[step]);
        yield return new WaitForSeconds(length * (chainPoint[step] - hitPoint[step]));

        comboStep = (step + 1) % 3; // 1 -> 2 -> 3 -> 1
        lastComboEndTime = Time.time;
        isAttacking = false;
    }

    // พุ่งโจมตี (Dash ใน class diagram) — DashStrikeSkill เรียก, คูลดาวน์อยู่ที่ ActiveSkill
    public void Dash(float damage, float hitRadius)
    {
        if (isDashing) return;
        if (isAttacking)
        {
            // กดพุ่งกลางคอมโบ = ยกเลิกท่าต่อย (Coroutine ที่รันอยู่มีแค่ ComboAttack)
            StopAllCoroutines();
            isAttacking = false;
            if (animator != null) animator.CrossFadeInFixedTime("Empty", 0.05f, 1, 0f); // หยุดท่าต่อยตัวบน
        }
        StartCoroutine(DashRoutine(damage, hitRadius));
    }

    // Shift = พุ่งหลบ ใช้ท่าและการเคลื่อนที่เดียวกับ Dash แต่ไม่ทำดาเมจ ไม่ต้องมีสกิล อมตะระหว่างพุ่ง
    public void Dodge()
    {
        if (isDashing || isCasting) return;
        if (Time.time - lastDodgeTime < dodgeCooldown) return;
        lastDodgeTime = Time.time;
        Debug.Log("Dodge (Shift)");
        Dash(0f, 0f);
    }

    private IEnumerator DashRoutine(float damage, float hitRadius)
    {
        isDashing = true;
        comboQueue.Clear(); // คลิกที่ค้างไว้ก่อนพุ่ง ไม่ต้องต่อย
        List<Character> alreadyHit = new List<Character>(); // ตัวไหนโดนแล้วไม่โดนซ้ำ

        // พุ่งไปทางที่กด WASD อยู่ ถ้าไม่กดเลยพุ่งไปข้างหน้า
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction);
        if (animator != null)
        {
            animator.ResetTrigger("Skill"); // สกิลนี้ใช้ท่า Dash ไม่ใช่ท่าสกิลทั่วไป
            animator.CrossFadeInFixedTime("Dash", 0.05f, 0, 0f);
        }

        float timer = 0f;
        float afterImageTimer = 0f;
        while (timer < dashTime)
        {
            controller.Move(direction * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;

            // หลบด้วย Shift (ดาเมจ 0): ทิ้งเงาร่างไว้ข้างหลัง ไม่ทำดาเมจ
            if (damage <= 0f)
            {
                afterImageTimer -= Time.deltaTime;
                if (afterImageTimer <= 0f)
                {
                    afterImageTimer = afterImageInterval;
                    DashAfterImage.Spawn(animator != null ? animator.transform : transform, afterImageColor, afterImageLife);
                }
                yield return null;
                continue;
            }
            // ศัตรูที่พุ่งผ่าน โดนดาเมจ
            List<Character> targets = FindTargetsInRadius(hitRadius);
            for (int i = 0; i < targets.Count; i++)
            {
                if (alreadyHit.Contains(targets[i])) continue;
                alreadyHit.Add(targets[i]);
                DealDamage(targets[i], damage);
            }
            yield return null; // รอเฟรมถัดไป
        }
        isDashing = false;
        // ตัวหยุดพุ่งแล้ว ตัดท่าพุ่งกลับไปท่าปกติทันที ท่าจะได้ไม่ก้าวต่อทั้งที่ตัวหยุด
        if (animator != null) animator.CrossFadeInFixedTime("Idle", 0.1f, 0);
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

    // ดาเมจเพิ่มชั่วคราว (0.3 = แรงขึ้น 30%) — MetalHeart ตั้งค่าทุกเฟรม
    private float bonusDamage;
    public void SetBonusDamage(float value)
    {
        bonusDamage = value;
    }

    // ทุกการโจมตี (ตีปกติ/สกิล) ผ่านตรงนี้ ตีตายแล้วได้ EXP
    public void DealDamage(Character target, float amount)
    {
        target.TakeDamage(amount * (1f + bonusDamage));
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
            VFXManager.Prewarm(skillData.vfxPrefab, transform.position); // เตรียมเอฟเฟกต์ไว้ก่อน กดครั้งแรกไม่กระตุก
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
        if (index >= skillSlots.Count || isCasting) return;
        if (skillSlots[index] is ActiveSkill active)
        {
            active.TryActivate(this);
        }
    }

    // ตำแหน่งกระดูกในท่า (มือ/เท้า) ให้เอฟเฟกต์ออกจากตรงนั้น — Humanoid ของ Mixamo หากระดูกให้เอง
    public Transform GetBone(HumanBodyBones bone)
    {
        if (animator == null || bone == HumanBodyBones.LastBone || !animator.isHuman) return null;
        return animator.GetBoneTransform(bone);
    }

    // เล่นท่าสกิล แล้วปล่อยดาเมจตรงจังหวะท่า — หลักเดียวกับ ComboAttack (ง้าง -> โดน -> คืนท่า)
    public void CastSkill(ActiveSkill skill)
    {
        StartCoroutine(CastRoutine(skill));
    }

    private IEnumerator CastRoutine(ActiveSkill skill)
    {
        SkillData data = skill.Data;
        string state = data.animState;
        if (animator == null || string.IsNullOrEmpty(state) || state == "Dash")
        {
            skill.Execute(this); // Dash/ไม่มีท่า: ออกทันที (Dash คุมท่าเองใน DashRoutine)
            yield break;
        }

        // กดกลางคอมโบ = ยกเลิกท่าต่อยตัวบน
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        isAttacking = false;
        comboQueue.Clear();
        animator.CrossFadeInFixedTime("Empty", 0.05f, 1, 0f);

        isCasting = true;
        animator.CrossFadeInFixedTime(state, 0.05f, 0, 0f);
        yield return null; // รอ 1 เฟรมให้ Animator เริ่มท่า แล้วอ่านความยาวท่าจริง
        AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(0);
        if (!info.IsName(state)) info = animator.GetCurrentAnimatorStateInfo(0);
        float length = info.length > 0.05f ? info.length : 0.5f; // length หารด้วย speed ของ state แล้ว

        yield return new WaitForSeconds(length * data.hitPoint);
        skill.Execute(this);
        yield return new WaitForSeconds(length * Mathf.Max(0f, data.recoverPoint - data.hitPoint));
        isCasting = false;
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
        if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame) Dodge();
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
