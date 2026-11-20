using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Player คือตัวละครที่ผู้เล่นควบคุม และสืบทอดระบบเลือดกับการตายจาก Character
[RequireComponent(typeof(CharacterController))]
public class Player : Character
{
    [Header("Player")]
    [SerializeField] private float turnSpeed = 720f; // ความเร็วในการหันหน้าเข้าหาทิศที่เดิน
    [SerializeField] private float gravity = -20f; // แรงโน้มถ่วงที่ใช้ดึงตัวละครลงพื้น

    [Header("Model")]
    [SerializeField] private Animator animator; // Animator ที่ใช้ควบคุมท่าเดิน โจมตี และใช้สกิล

    [Header("Stats")]
    [SerializeField] private PlayerStats stats = new PlayerStats(); // เก็บข้อมูลเลเวล EXP และพลังโจมตีของผู้เล่น
    public PlayerStats Stats => stats;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f; // ระยะตี
    [SerializeField] private float attackRadius = 1.2f; // วงตี
    [SerializeField] private float windUpTime = 0.15f; // ไว้ใช้ตอนยังไม่มีโมเดล

    [Header("Combo 1-2-3")]
    [SerializeField] private float comboResetTime = 0.5f; // เว้นนานเกินนี้กลับไปท่า 1
    [SerializeField] private float[] comboDamage = { 1f, 1f, 1.5f }; // ท่า 3 แรงสุด
    // hitPoint = ตอนหมัดโดน, chainPoint = ตอนต่อท่าถัดไปได้ (เป็นสัดส่วนของท่า)
    [SerializeField] private float[] hitPoint = { 0.35f, 0.4f, 0.45f };
    [SerializeField] private float[] chainPoint = { 0.65f, 0.65f, 0.85f };
    [SerializeField] private float clickBufferTime = 0.6f; // เวลาที่เก็บคลิกโจมตีไว้รอต่อคอมโบ
    [SerializeField] private float attackMoveMultiplier = 0.9f; // ต่อยไปเดินไปได้ ช้าลงนิดเดียว
    // เก็บเวลาที่กดโจมตีไว้ เพื่อให้กดต่อเนื่องแล้วเล่นคอมโบตามลำดับ
    private Queue<float> comboQueue = new Queue<float>();
    private int comboStep; // 0,1,2 = ท่า 1,2,3
    private float lastComboEndTime = -99f;
    private Coroutine comboRoutine; // เก็บไว้ stop ตอนกดสกิล

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction attackAction;
    private float verticalVelocity;
    private bool isAttacking;

    [Header("Skills")]
    [SerializeField] private List<SkillData> startingSkills = new List<SkillData>(); // สกิลตอนเริ่ม เรียงตามปุ่ม 1 2 3
    // เก็บสกิล ใช้ Dictionary แบบ Inventory
    private Dictionary<string, Skill> skills = new Dictionary<string, Skill>();
    // ช่องสกิลปุ่ม 1 2 3 เรียงตามลำดับที่ได้สกิล
    private List<Skill> skillSlots = new List<Skill>();
    public IReadOnlyList<Skill> SkillSlots => skillSlots;
    public IEnumerable<Skill> Skills => skills.Values;
    public event System.Action OnSkillsChanged;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 12f; // ความเร็วในการพุ่ง
    [SerializeField] private float dashTime = 0.35f; // เวลาพุ่ง
    private bool isDashing; // ดูว่าตอนนี้ Player พุ่งยุมั้ย

    [Header("Dodge (Shift)")]
    [SerializeField] private float dodgeCooldownTime = 4f; // เวลาคูลดาวน์ของ Shift เพื่อหลบ
    private float lastDodgeTime = -99f;
    public float DodgeCooldown => dodgeCooldownTime;
    public float DodgeCooldownRemaining => Mathf.Max(0f, dodgeCooldownTime - (Time.time - lastDodgeTime));
    [SerializeField] private Color afterImageColor = new Color(0.45f, 0.8f, 1f, 0.3f); // สีเงา
    [SerializeField] private float afterImageInterval = 0.08f; // เว้นกี่วิทิ้งเงา 1 อัน
    [SerializeField] private float afterImageLife = 0.25f; // เงาหายในกี่วิ
    private bool isCasting; // เช็กว่ากำลังอยู่ระหว่างใช้สกิลมั้ย

    [Header("Level Up")]
    [SerializeField] private float hpPerLevel = 25f; // จำนวนเลือดสูงสุดที่จะเพิ่มเมื่อเลเวลอัป

    [Header("Hurt")]
    [SerializeField] private float invincibleTime = 0.6f; // โดนแล้วอมตะแป๊บนึง กันโดนรัว
    private float lastHurtTime = -99f;

    public override void TakeDamage(float damage)
    {
        if (isDashing) return; // พุ่งอยู่ไม่โดน
        if (Time.time - lastHurtTime < invincibleTime) return;// ถ้ายังอยู่ในช่วงอมตะ จะไม่รับดาเมจซ้ำ
        lastHurtTime = Time.time;
        base.TakeDamage(damage);
        // ไม่เล่นท่าโดนตี (Hit) — Boat ไม่ชะงักเวลาโดนศัตรูตี เดิน/ต่อย/ใช้สกิลต่อได้เลย
    }

    // เตรียมค่าต่าง ๆ ของ Player ตอนเริ่มเกม
    public override void SetUP()
    {
        base.SetUP(); // เรียกการตั้งค่าเริ่มต้นจาก Character เช่น เติมเลือดและกำหนดสถานะมีชีวิต
        controller = GetComponent<CharacterController>();
        // เอา Input Action สำหรับปุ่มเดินและปุ่มโจมตี
        moveAction = InputSystem.actions.FindAction("Move");
        if (moveAction == null)
        {
            Debug.LogError("ไม่พบ Input Action ชื่อ Move");
        }
        attackAction = InputSystem.actions.FindAction("Attack");
        stats.Refresh(); // อัปเดตข้อมูลเริ่มต้นของเลเวลและ EXP
        // เลเวลอัปแล้วเลือดเพิ่ม (ถอดก่อนใส่ กันโดนเรียก 2 รอบ)
        stats.OnLevelUp -= HandleLevelUp;
        stats.OnLevelUp += HandleLevelUp;

        // เพิ่มสกิลเริ่มต้นให้ Player ตามรายการใน Inspector
        for (int i = 0; i < startingSkills.Count; i++)
        {
            AddSkill(startingSkills[i]);
        }
    }

    // ตรวจการควบคุมและอัปเดตการทำงานของ Player ทุกเฟรม
    private void Update()
    {
        if (!isAlive || moveAction == null) return;

        UpdateSkillInput(); // ตรวจปุ่มใช้สกิลและปุ่มหลบ
        if (isDashing) return; // ระหว่างพุ่ง ให้ DashRoutine ควบคุมการเคลื่อนที่แทน
        if (isCasting)
        {
            Move(Vector2.zero); // ใช้สกิลอยู่ยืนนิ่ง จะกดเดินไม่ได้
            return;
        }

        // ต่อยอยู่ก็ยังเดินได้ หนีได้
        Vector2 input = moveAction.ReadValue<Vector2>();
        Move(isAttacking ? input * attackMoveMultiplier : input);

        // คลิกแล้วเข้าคิว เก็บสูงสุด 2
        if (attackAction != null && attackAction.triggered && comboQueue.Count < 2)
        {
            comboQueue.Enqueue(Time.time);
        }
        // ต่อยจบแล้วเอาอันต่อไปออกจากคิว
        if (!isAttacking && comboQueue.Count > 0)
        {
            float clickTime = comboQueue.Dequeue();
            if (Time.time - clickTime <= clickBufferTime)
            {
                comboRoutine = StartCoroutine(ComboAttack());
            }
        }
    }

    // เล่นคอมโบหนึ่งท่า และกำหนดจังหวะที่ดาเมจจะออก
    private IEnumerator ComboAttack()
    {
        if (Time.time - lastComboEndTime > comboResetTime)
        {
            comboStep = 0; // ไม่ได้กดนาน เริ่มท่า 1 ใหม่
        }
        int step = comboStep;
        isAttacking = true;

        // ใช้ความยาว Animation เป็นเวลาของท่าโจมตี
        float length = windUpTime / hitPoint[step];
        if (animator != null)
        {
            // เลือก Animation ตามลำดับคอมโบ เช่น Combo1, Combo2 และ Combo3
            string stateName = "Combo" + (step + 1);
            // layer 1 = ตัวบน ขาเดินได้
            animator.CrossFadeInFixedTime(stateName, 0.05f, 1, 0f);
            yield return null; // รอให้ Animator เริ่มเปลี่ยนท่าก่อนอ่านข้อมูล Animation
            AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(1);
            if (!info.IsName(stateName)) info = animator.GetCurrentAnimatorStateInfo(1);
            if (info.length > 0.05f) length = info.length;
        }

        yield return new WaitForSeconds(length * hitPoint[step]);
        Attack(comboDamage[step]);
        yield return new WaitForSeconds(length * (chainPoint[step] - hitPoint[step]));

        comboStep = (step + 1) % 3;
        lastComboEndTime = Time.time;
        isAttacking = false;
    }

    // เริ่มการพุ่ง โดยรับค่าดาเมจและรัศมีโจมตีจากสกิลที่เรียกใช้
    public void Dash(float damage, float hitRadius)
    {
        if (isDashing) return; // ถ้ากำลังพุ่งอยู่ จะไม่เริ่มการพุ่งซ้ำ
        if (isAttacking)
        {
            // กดพุ่งตอนต่อยอยู่ ยกเลิกต่อย
            StopAllCoroutines();
            isAttacking = false;
            if (animator != null) animator.CrossFadeInFixedTime("Empty", 0.05f, 1, 0f);
        }
        StartCoroutine(DashRoutine(damage, hitRadius));
    }

    // Shift หลบ ใช้ท่าเดียวกับ Dash แต่ไม่มีดาเมจ
    public void Dodge()
    {
        if (isDashing || isCasting) return;
        if (Time.time - lastDodgeTime < dodgeCooldownTime) return;
        lastDodgeTime = Time.time;
        Dash(0f, 0f);
    }

    private IEnumerator DashRoutine(float damage, float hitRadius)
    {
        isDashing = true;
        comboQueue.Clear();
        List<Character> alreadyHit = new List<Character>(); // กันโดนซ้ำ

        // พุ่งตามปุ่มที่กด ไม่กดก็พุ่งไปข้างหน้า
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
        direction.Normalize();
        transform.rotation = Quaternion.LookRotation(direction);
        if (animator != null)
        {
            animator.ResetTrigger("Skill");
            animator.CrossFadeInFixedTime("Dash", 0.05f, 0, 0f);
        }

        float timer = 0f;
        float afterImageTimer = afterImageInterval; // เงาแรกไม่ให้ทับตัว
        while (timer < dashTime)
        {
            controller.Move(direction * dashSpeed * Time.deltaTime);
            timer += Time.deltaTime;

            // หลบ = ทิ้งเงาอย่างเดียว
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
            // ตัวที่พุ่งผ่านโดนดาเมจ
            List<Character> targets = FindTargetsInRadius(hitRadius);
            for (int i = 0; i < targets.Count; i++)
            {
                if (alreadyHit.Contains(targets[i])) continue;
                alreadyHit.Add(targets[i]);
                DealDamage(targets[i], damage);
            }
            yield return null;
        }
        isDashing = false;
        // พุ่งเสร็จกลับท่า idle เลย ไม่งั้นขายังก้าวอยู่
        if (animator != null) animator.CrossFadeInFixedTime("Idle", 0.1f, 0);
    }

    // เรียกโจมตีด้วยดาเมจปกติ 1 เท่า
    public void Attack()
    {
        Attack(1f);
    }

    // หาศัตรูข้างหน้าแล้ว TakeDamage
    public void Attack(float damageMultiplier)
    {
        Vector3 center = transform.position + transform.forward * attackRange;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius); // ตรวจ Collider ทุกตัวที่อยู่ในรัศมีโจมตี
        for (int i = 0; i < hits.Length; i++)
        {
            Character target = hits[i].GetComponent<Character>();
            if (target != null && target != this && target.IsAlive) // โจมตีเฉพาะ Character ตัวอื่นที่ยังมีชีวิตอยู่
            {
                DealDamage(target, stats.Damage * damageMultiplier);
            }
        }
    }

    // MetalHeart ตั้งค่าทุกเฟรม
    private float bonusDamage;
    public void SetBonusDamage(float value)
    {
        bonusDamage = value;
    }

    // ดาเมจทุกอย่างผ่านตรงนี้ ฆ่าได้ได้ EXP
    public void DealDamage(Character target, float amount)
    {
        target.TakeDamage(amount * (1f + bonusDamage));
        if (!target.IsAlive)
        {
            stats.AddExp(target.ExpReward);  // ถ้าเป้าหมายตาย จะได้รับ EXP ตามค่ารางวัลของเป้าหมาย
        }
    }

    // หาศัตรูรอบตัว ไว้ใช้กับสกิล
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
    // เพิ่มสกิลให้ Player ถ้ามีสกิลเดิมอยู่แล้วจะเพิ่มเลเวลแทน
    public void AddSkill(SkillData skillData)
    {
        if (skillData == null) return;

        // ถ้ามีสกิลนี้อยู่แล้ว ให้เพิ่มเลเวลสกิลเดิม
        if (skills.ContainsKey(skillData.skillName))
        {
            skills[skillData.skillName].LevelUp(this);
        }
        else
        {
            // ถ้ายังไม่มี ให้สร้างสกิลใหม่และเก็บไว้ในรายการสกิลของ Player
            Skill newSkill = skillData.CreateSkill();
            skills.Add(skillData.skillName, newSkill);
            newSkill.OnAdded(this);
            skillSlots.Add(newSkill);// เพิ่มสกิลลงช่องที่ใช้กดปุ่ม 1, 2 หรือ 3
            VFXManager.Prewarm(skillData.vfxPrefab, transform.position); // เตรียม VFX ไว้ล่วงหน้า เพื่อลดอาการกระตุกตอนใช้สกิล
            Debug.Log("Got new skill " + skillData.skillName);
        }
        OnSkillsChanged?.Invoke();// แจ้ง UI ให้อัปเดตช่องสกิล
    }

    // ตรวจว่า Player มีสกิลตามชื่อนี้หรือไม่
    public bool HasSkill(string skillName)
    {
        return skills.ContainsKey(skillName);
    }

    // คืนค่าเลเวลของสกิล ถ้ายังไม่มีสกิลนี้จะคืนค่า 0
    public int GetSkillLevel(string skillName)
    {
        if (skills.ContainsKey(skillName))
        {
            return skills[skillName].Level;
        }
        return 0;
    }

    // ดูว่าสกิลนี้เลือกได้มั้ย ดูว่าเป็นสกิลเริ่มต้นหรือเป็นสกิลต่อยอดจากสกิลที่มีอยู่
    public bool IsSkillAvailable(SkillData skillData)
    {
        if (skillData.isStartAvailable) return true;
        foreach (Skill owned in skills.Values)
        {
            if (owned.Data.nextSkills.Contains(skillData)) return true;
        }
        return false;
    }

    // เลเวลอัป: เลือดสูงสุด + เลือดตอนนี้ เพิ่ม hpPerLevel
    private void HandleLevelUp(int newLevel)
    {
        ApplyStatBoost(StatType.MaxHP, hpPerLevel, false);
        Debug.Log("Level " + newLevel + " : เลือดสูงสุด +" + hpPerLevel + " = " + maxHP);
    }

    // เพิ่ม stat ใช้ตอนเลือกการ์ด
    // stat ใหม่ เพิ่มใน StatType แล้วมาเพิ่ม case นี้
    public void ApplyStatBoost(StatType type, float value, bool isPercent)
    {
        switch (type)
        {
            case StatType.MaxHP:
                float addHP = isPercent ? maxHP * value : value; // คำนวณจำนวนเลือดที่จะเพิ่ม ลือกได้ว่าเป็นเปอร์เซ็นต์หรือค่าคงที่
                maxHP += addHP;
                currentHP += addHP; // เพิ่มเลือดปัจจุบันตามไปด้วย ให้เลือดที่เพิ่มมาใช้งานได้ทันที
                RaiseHPChanged();
                break;
            case StatType.Damage:
                stats.AddDamage(isPercent ? stats.Damage * value : value);
                break;
            case StatType.MoveSpeed:
                moveSpeed += isPercent ? moveSpeed * value : value;
                break;
        }
        Debug.Log("Stat boost " + type + " +" + value + (isPercent ? " (x%)" : ""));
    }

    // ใช้สกิล ช่องนี้จะใช้ได้เฉพาะActiveSkill
    private void UseSkillSlot(int index)
    {
        if (index >= skillSlots.Count || isCasting) return;
        if (skillSlots[index] is ActiveSkill active)
        {
            active.TryActivate(this); // ส่งคำสั่งไปให้สกิลตรวจสอบคูลดาวน์และเริ่มทำงาน
        }
    }

    // หาตำแหน่งมือ/เท้า ไว้ปล่อยเอฟเฟกต์
    public Transform GetBone(HumanBodyBones bone)
    {
        if (animator == null || bone == HumanBodyBones.LastBone || !animator.isHuman) return null;
        return animator.GetBoneTransform(bone);
    }

    // เล่นท่าสกิล รอถึงจังหวะค่อยปล่อยดาเมจ (หลักเดียวกับคอมโบ)
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
            skill.Execute(this); // ไม่มีท่าก็ปล่อยเลย
            yield break;
        }

        // ยกเลิกต่อยตัวบน
        if (comboRoutine != null) StopCoroutine(comboRoutine);
        isAttacking = false;
        comboQueue.Clear();
        animator.CrossFadeInFixedTime("Empty", 0.05f, 1, 0f);

        isCasting = true;
        animator.CrossFadeInFixedTime(state, 0.05f, 0, 0f);
        yield return null; // รอ 1 เฟรมแล้วค่อยอ่านความยาวท่า
        AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(0);
        if (!info.IsName(state)) info = animator.GetCurrentAnimatorStateInfo(0);
        float length = info.length > 0.05f ? info.length : 0.5f;

        yield return new WaitForSeconds(length * data.hitPoint);
        skill.Execute(this);
        yield return new WaitForSeconds(length * Mathf.Max(0f, data.recoverPoint - data.hitPoint));
        isCasting = false;
    }

    private void UpdateSkillInput()
    {
        foreach (Skill skill in skills.Values)
        {
            skill.Tick(this);
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        // ปุ่ม 1 2 3
        if (keyboard.digit1Key.wasPressedThisFrame) UseSkillSlot(0);
        if (keyboard.digit2Key.wasPressedThisFrame) UseSkillSlot(1);
        if (keyboard.digit3Key.wasPressedThisFrame) UseSkillSlot(2);
        if (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame) Dodge();
    }
    #endregion

    // วาดวงตีไว้ดูใน Scene
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, attackRadius);
    }

    private void Move(Vector2 input)
    {
        // input เป็น x,y แต่เดินบน x,z
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // แรงโน้มถ่วง
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
        // เล่นท่าตาย ไม่มีโมเดลก็ล้มแคปซูล
        if (animator != null) animator.SetBool("Dead", true);
        else transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
    }
}
