using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ศัตรูพื้นฐาน (ตีใกล้) — สืบทอดจาก Character (Inheritance ตาม Diagram)
// ศัตรูแบบอื่นสืบทอดต่อแล้ว override เฉพาะส่วนที่ต่าง (Polymorphism) เช่น
//   โครงกระดูก override OnAttackHit() ให้ยิงธนู, สไลม์ override Die() ให้แตกตัว, Boss override Attack() ให้สุ่มสกิล
[RequireComponent(typeof(CharacterController))]
public class Enemy : Character
{
    [Header("Enemy")]
    [SerializeField] protected EnemyData data;
    [SerializeField] protected Animator animator; // Animator ของโมเดล (ลูกของ Enemy)
    [SerializeField] protected float turnSpeed = 600f; // องศาต่อวินาที
    [SerializeField] protected float gravity = -20f;
    [Tooltip("ตีโดนถ้าผู้เล่นอยู่ในระยะ attackRange + ค่านี้ (เผื่อผู้เล่นถอยนิดหน่อยระหว่างง้าง)")]
    [SerializeField] protected float hitTolerance = 0.4f;
    [Tooltip("ผู้เล่นต้องอยู่ในมุมนี้ด้านหน้าถึงจะโดน (องศา)")]
    [SerializeField] protected float hitAngle = 75f;

    [Header("Surround - ล้อมผู้เล่น")]
    [Tooltip("เดินไปยืนตามช่องรอบตัวผู้เล่นที่ EnemyManager แจกให้ (ไม่กองอยู่หน้าผู้เล่นตัวเดียว)")]
    public bool surroundPlayer = true;
    [Tooltip("ยืนห่างผู้เล่นกี่ส่วนของ attackRange (0.8 = 80% ของระยะตี)")]
    [Range(0.5f, 1f)] public float standRatio = 0.8f;
    [Tooltip("ห่างจากช่องของตัวเองไม่เกินกี่เมตร (วัดตามแนวโค้งรอบผู้เล่น) ถึงจะยืนตีได้")]
    public float slotTolerance = 1f;
    [Tooltip("แรงดันออกจากศัตรูตัวอื่นที่ยืนชิดกัน (กันซ้อนกัน)")]
    public float separationWeight = 1.5f;

    [Header("Dungeon")]
    [Tooltip("ไล่ผู้เล่นตลอด ไม่สน detectRange (ศัตรูในดันเจี้ยนที่ Room สั่งเกิด จะเปิดอันนี้)")]
    public bool huntPlayer;

    protected Player target;
    protected EnemyManager manager;    // EnemyManager ที่สร้างตัวนี้ (สไลม์ใช้เกิดลูก)
    protected CharacterController controller;
    protected bool isAttacking;
    protected bool isStunned;
    protected float lastAttackTime = -99f;
    private float verticalVelocity;
    private Coroutine attackRoutine;
    private Coroutine stunRoutine;
    private Renderer[] renderers;
    private MaterialPropertyBlock flashBlock;
    private float surroundAngle;   // มุมของช่องรอบผู้เล่น (องศา, 0 = ทิศ +Z ของโลก)
    private bool hasSlot;
    private float offSlotTimer;    // อยู่ในระยะตีแต่ยังไปไม่ถึงช่องนานแค่ไหนแล้ว (ติดกำแพง/ติดเพื่อน)

    public EnemyData Data => data;
    public Player Target => target;
    // รัศมีตัวจริงในโลก (รัศมี CharacterController x สเกล) — ใช้กันศัตรูยืนซ้อนกัน
    public float BodyRadius => controller != null ? controller.radius * transform.localScale.x : 0.5f;

    // EnemyManager แจกช่องรอบตัวผู้เล่นให้ (เรียกซ้ำทุกครึ่งวินาที)
    public void SetSurroundAngle(float angle)
    {
        surroundAngle = angle;
        hasSlot = true;
    }

    // EnemyManager เรียกตอน Register — ศัตรูจะได้สั่ง EnemyManager ต่อได้ (เช่น สไลม์แตกตัว)
    public void SetManager(EnemyManager enemyManager)
    {
        manager = enemyManager;
    }
    public override int ExpReward => data != null ? data.expDrop : 0;

    // EnemyManager เรียกหลัง Instantiate (ก่อน Start) — ส่งข้อมูลกับเป้าหมายมาให้
    public virtual void Init(EnemyData enemyData, Player player)
    {
        data = enemyData;
        target = player;
        ApplyData();
    }

    public override void SetUP()
    {
        controller = GetComponent<CharacterController>();
        renderers = GetComponentsInChildren<Renderer>();
        // วางในซีนเองโดยไม่ผ่าน EnemyManager ก็ยังหาผู้เล่นเจอ
        if (target == null) target = FindAnyObjectByType<Player>();
        ApplyData();
        base.SetUP(); // ตั้ง currentHP = maxHP (maxHP มาจาก EnemyData แล้ว)
        controller.enabled = true;
        isAttacking = false;
        isStunned = false;
    }

    // คัดค่าจาก EnemyData มาใส่ตัวแปรของ Character
    protected virtual void ApplyData()
    {
        if (data == null)
        {
            Debug.LogWarning(name + ": ยังไม่ได้ใส่ EnemyData");
            return;
        }
        characterName = data.enemyName;
        maxHP = data.maxHP;
        moveSpeed = data.moveSpeed;
        transform.localScale = Vector3.one * data.modelScale;
        if (animator != null) animator.SetFloat("AttackSpeed", data.attackAnimSpeed);
    }

    protected virtual void Update()
    {
        if (!isAlive) return;

        // ไม่มีเป้าหมาย / ผู้เล่นตาย / กำลังตี / ชะงัก = ยืนเฉย ๆ
        if (data == null || target == null || !target.IsAlive || isAttacking || isStunned)
        {
            Move(Vector3.zero);
            return;
        }

        Vector3 toTarget = FlatDirectionToTarget();
        float distance = toTarget.magnitude;
        // ล้อม: ต้องอยู่ใกล้ช่องของตัวเองก่อนถึงจะยืนตี (ไม่งั้นทุกตัวจะหยุดตีอยู่ด้านหน้าผู้เล่นหมด)
        bool atSlot = !UsesSlot() || Mathf.Abs(SlotArcError()) <= slotTolerance;
        // ไปช่องไม่ได้นานเกิน 2 วินาที (เช่นติดกำแพง) ก็ตีจากตรงนั้นเลย
        if (distance <= data.attackRange && !atSlot) offSlotTimer += Time.deltaTime;
        else offSlotTimer = 0f;
        if (offSlotTimer > 2f) atSlot = true;

        if (distance <= data.attackRange && atSlot)
        {
            // ถึงระยะแล้ว หยุดเดิน (ดันออกจากเพื่อนที่ชิดเกินเท่านั้น) หันหน้าเข้าหา พอหันตรงแล้วคูลดาวน์ครบก็ตี
            Move(Vector3.ClampMagnitude(Separation() * separationWeight, 1f));
            FaceDirection(toTarget);
            bool cooldownReady = Time.time - lastAttackTime >= data.attackCooldown;
            if (cooldownReady && Vector3.Angle(transform.forward, toTarget) < 30f)
            {
                Attack();
            }
        }
        else if (distance <= data.detectRange || huntPlayer)
        {
            ChasePlayer();
        }
        else
        {
            Move(Vector3.zero);
        }
    }

    // เดินเข้าหาผู้เล่น (ChasePlayer ใน Diagram) — ล้อม: เดินอ้อมไปยังช่องของตัวเอง
    public virtual void ChasePlayer()
    {
        // 1. ทิศหลัก: ไปช่องรอบผู้เล่น (ไม่มีช่อง = ตรงเข้าหาผู้เล่น)
        Vector3 direction = UsesSlot() ? SurroundDirection() : FlatDirectionToTarget().normalized;
        // 2. บวกแรงดันออกจากเพื่อนที่เดินชิดกัน
        direction += Separation() * separationWeight;
        // 3. ยาวเกิน 1 = เดินเต็มความเร็ว / ใกล้ถึงช่อง = ค่อย ๆ ช้าลงเอง
        direction = Vector3.ClampMagnitude(direction, 1f);
        FaceDirection(direction);
        Move(direction);
    }

    // เริ่มท่าตี (Attack ใน Diagram) — ดาเมจจริงออกใน OnAttackHit() ตรงจังหวะท่า
    public virtual void Attack()
    {
        if (isAttacking || !isAlive) return;
        attackRoutine = StartCoroutine(AttackRoutine());
    }

    // ง้าง -> ฟันโดน (hitPoint) -> คืนท่า — หลักเดียวกับ ComboAttack ของ Player
    protected virtual IEnumerator AttackRoutine()
    {
        isAttacking = true;
        float length = 0.8f; // ไม่มีโมเดลใช้ค่าประมาณ

        if (animator != null && data.attackAnims != null && data.attackAnims.Length > 0)
        {
            string state = data.attackAnims[Random.Range(0, data.attackAnims.Length)];
            animator.CrossFadeInFixedTime(state, 0.05f, 0, 0f);
            yield return null; // รอ 1 เฟรมให้ Animator เริ่มท่า แล้วอ่านความยาวท่าจริง
            AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(0);
            if (!info.IsName(state)) info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0.05f) length = info.length; // length หารด้วย speed ของ state แล้ว
        }

        yield return new WaitForSeconds(length * data.hitPoint);
        OnAttackHit();
        yield return new WaitForSeconds(length * (1f - data.hitPoint) * 0.9f);

        isAttacking = false;
        attackRoutine = null;
        lastAttackTime = Time.time; // คูลดาวน์นับจากตีจบ
        ReturnToMove();
    }

    // จังหวะดาเมจออก — ตีใกล้: เช็กระยะ+มุมหน้า / ตีไกล (โครงกระดูก): override ให้ยิงกระสุนแทน
    protected virtual void OnAttackHit()
    {
        if (target == null || !target.IsAlive) return;

        Vector3 toTarget = FlatDirectionToTarget();
        bool inRange = toTarget.magnitude <= data.attackRange + hitTolerance;
        bool inFront = Vector3.Angle(transform.forward, toTarget) <= hitAngle;
        if (inRange && inFront)
        {
            target.TakeDamage(data.damage);
        }
    }

    public override void TakeDamage(float damage)
    {
        if (!isAlive) return;

        base.TakeDamage(damage);
        if (!isAlive) return; // ตายแล้ว Die() จัดการต่อ

        StartCoroutine(FlashHit());
        if (data != null && data.canBeStunned)
        {
            // ยกเลิกท่าตีที่ง้างอยู่ แล้วเล่นท่าโดนตี
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            isAttacking = false;
            if (stunRoutine != null) StopCoroutine(stunRoutine);
            stunRoutine = StartCoroutine(StunRoutine());
        }
    }

    private IEnumerator StunRoutine()
    {
        isStunned = true;
        if (animator != null) animator.CrossFadeInFixedTime("Hit", 0.05f, 0, 0f);
        yield return new WaitForSeconds(data.stunTime);
        isStunned = false;
        stunRoutine = null;
        ReturnToMove();
    }

    // กลับไปท่าเดิน/ยืน (Blend Tree "Move") ทันที ไม่ต้องรอท่าตี/ท่าโดนตีเล่นจนจบ ตัวจะได้ไม่ไถลตอนเริ่มเดิน
    protected void ReturnToMove()
    {
        if (animator != null && isAlive) animator.CrossFadeInFixedTime("Move", 0.15f, 0);
    }

    // กะพริบแดงตอนโดนตี (ใช้ MaterialPropertyBlock ไม่ต้องสร้าง Material ใหม่)
    private IEnumerator FlashHit()
    {
        return FlashHit(new Color(1f, 0.35f, 0.35f));
    }

    // Overload: กะพริบสีอื่น (บอสตั้งโล่กันได้ = สีฟ้า)
    protected IEnumerator FlashHit(Color color)
    {
        SetFlash(true, color);
        yield return new WaitForSeconds(0.1f);
        SetFlash(false, color);
    }

    private void SetFlash(bool on, Color color)
    {
        if (renderers == null) return;
        if (flashBlock == null) flashBlock = new MaterialPropertyBlock();
        flashBlock.SetColor("_BaseColor", color);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            renderers[i].SetPropertyBlock(on ? flashBlock : null);
        }
    }

    protected override void Die()
    {
        StopAllCoroutines();
        SetFlash(false, Color.white);
        isAttacking = false;
        isStunned = false;

        base.Die(); // แจ้ง OnDied -> EnemyManager นับศัตรูที่เหลือ
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("Dead", true);
        }
        controller.enabled = false; // ผู้เล่นเดินผ่านศพได้ และตีศพไม่โดน
        Destroy(gameObject, data != null ? data.corpseTime : 3f);
    }

    #region Surround
    protected bool UsesSlot()
    {
        return surroundPlayer && hasSlot && target != null;
    }

    // ยืนห่างผู้เล่นเท่าไร: 80% ของระยะตี แต่ไม่ชิดจนตัวชนผู้เล่น และไม่เกินระยะตี
    protected float StandDistance()
    {
        float touch = BodyRadius + 0.3f + 0.25f; // รัศมีเรา + รัศมี Boat (0.3) + เผื่อช่องว่าง
        float stand = Mathf.Max(data.attackRange * standRatio, touch);
        return Mathf.Min(stand, data.attackRange * 0.95f);
    }

    // ห่างจากช่องกี่เมตรตามแนวโค้ง (+ = ต้องเดินวนตามเข็ม, - = ทวนเข็ม)
    protected float SlotArcError()
    {
        Vector3 fromPlayer = -FlatDirectionToTarget();
        float myAngle = Mathf.Atan2(fromPlayer.x, fromPlayer.z) * Mathf.Rad2Deg;
        float delta = Mathf.DeltaAngle(myAngle, surroundAngle);
        return delta * Mathf.Deg2Rad * fromPlayer.magnitude;
    }

    // ทิศเดินไปช่อง: แยกเป็น 2 ส่วน
    //   ส่วนรัศมี  : เข้า/ออกจากผู้เล่นให้ได้ระยะ StandDistance
    //   ส่วนสัมผัส : เดินวนรอบผู้เล่นไปยังมุมของช่อง (ไม่เดินทะลุตัวผู้เล่น)
    protected Vector3 SurroundDirection()
    {
        Vector3 fromPlayer = -FlatDirectionToTarget();
        float distance = fromPlayer.magnitude;
        if (distance < 0.01f) return transform.forward;

        // 1. ทิศออกจากผู้เล่น / ทิศวนรอบผู้เล่น (ตั้งฉากกัน)
        Vector3 radial = fromPlayer / distance;
        Vector3 tangent = new Vector3(radial.z, 0f, -radial.x);

        // 2. ต้องขยับเท่าไรในแต่ละทิศ (หน่วยเมตร)
        float radialError = distance - StandDistance();
        float arcError = Mathf.Clamp(SlotArcError(), -2f, 2f); // จำกัดไว้ ตอนอยู่ไกลจะได้เดินเข้าหาก่อน ไม่วิ่งวนรอบ

        // 3. รวมกัน
        return -radial * radialError + tangent * arcError;
    }

    // ดันออกจากศัตรูตัวอื่นที่ยืนชิดกันเกินไป (วนลูปเช็กทุกตัวใน List ของ EnemyManager)
    protected Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        if (manager == null) return push;

        IReadOnlyList<Enemy> others = manager.AliveEnemies;
        for (int i = 0; i < others.Count; i++)
        {
            Enemy other = others[i];
            if (other == null || other == this) continue;

            // 1. ระยะที่ควรห่างกัน = รัศมีเรา + รัศมีเพื่อน + ช่องว่างนิดหน่อย
            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float minDistance = BodyRadius + other.BodyRadius + 0.2f;
            float d = away.magnitude;
            if (d >= minDistance) continue;

            // 2. ยิ่งชิดยิ่งดันแรง (ทับกันพอดีให้ดันไปทางขวา)
            if (d < 0.01f) away = transform.right;
            push += away.normalized * (minDistance - d) / minDistance;
        }
        return push;
    }
    #endregion

    #region Movement
    protected Vector3 FlatDirectionToTarget()
    {
        if (target == null) return Vector3.zero;
        Vector3 d = target.transform.position - transform.position;
        d.y = 0f;
        return d;
    }

    protected void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion wanted = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, turnSpeed * Time.deltaTime);
    }

    // เดินตามทิศ (ขนาด 0-1) พร้อมแรงโน้มถ่วง — แบบเดียวกับ Player.Move
    protected void Move(Vector3 direction)
    {
        if (controller == null || !controller.enabled) return;

        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        if (animator != null) animator.SetFloat("Speed", direction.magnitude, 0.05f, Time.deltaTime);

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }
    #endregion

    // วาดระยะมองเห็น/ระยะตีในหน้าต่าง Scene ไว้ปรับค่า
    private void OnDrawGizmosSelected()
    {
        if (data == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
        // ช่องรอบผู้เล่นที่ได้รับแจก
        if (UsesSlot() && Application.isPlaying)
        {
            Vector3 slot = target.transform.position + Quaternion.Euler(0f, surroundAngle, 0f) * Vector3.forward * StandDistance();
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, slot);
            Gizmos.DrawWireSphere(slot, 0.2f);
        }
    }
}
