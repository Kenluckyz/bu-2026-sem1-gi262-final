using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// บอสออค — สืบทอดจาก Enemy แล้ว override Attack() ให้สุ่มสกิล (Boss ใน Diagram)
// สกิล 1 กระโดดจาม (Attack3) : ผู้เล่นอยู่ไกล -> ย่อตัว กระโดดไปจามขวานลงจุดที่ผู้เล่นยืน
// สกิล 2 ตั้งโล่    (Attack4 = ท่า Idle_Battle1 ของแพ็ก ยกโล่บังหน้า) : ตีด้านหน้าไม่เข้า ต้องอ้อมไปตีด้านหลัง (บอสหันตามช้า ๆ)
// สกิล 3 เหวี่ยงขวาน (Attack2) : AoE รูปพัดด้านหน้า
// สกิล 4 สับพื้น    (Attack1) : AoE วงกลมรอบตัว
// ทุกท่าโจมตีมีพื้นแดง (AttackIndicator) ขึ้นก่อน — สีแดงเข้มขยายเต็มวงเมื่อไร = โดนตอนนั้น
public class OrcBoss : Enemy
{
    private const int SkillSlam = 0;
    private const int SkillSweep = 1;

    [Header("Boss - ท่า (Builder ใส่ให้ ใช้คำนวณจังหวะโดน)")]
    public AnimationClip slamClip;
    public AnimationClip sweepClip;
    public AnimationClip jumpClip;
    public Material indicatorMaterial;     // Material ของพื้นแดง (Sprites/Default)
    [Tooltip("ยืนง้างให้ผู้เล่นเห็นพื้นแดงก่อนเริ่มท่ากี่วินาที (มากขึ้น = หลบง่ายขึ้น)")]
    public float telegraphHold = 0.25f;

    [Header("สกิล 1 กระโดดจาม (Combo 2 จังหวะ)")]
    [Tooltip("ครั้งที่ 1: แรงกระแทกตอนตัวลงพื้น (วงรอบจุดที่บอสลง)")]
    public float jumpLandDamage = 18f;
    public float jumpLandRadius = 2.6f;
    [Tooltip("ครั้งที่ 2: หัวขวานจามลงพื้น (วงตรงหัวขวาน = ตรงที่ผู้เล่นยืนตอนบอสกระโดด)")]
    public float jumpDamage = 32f;
    public float jumpRadius = 3.2f;
    public float jumpMinRange = 5f;        // ผู้เล่นอยู่ไกลกว่านี้ถึงจะกระโดด
    public float jumpMaxRange = 16f;
    public float jumpCooldown = 8f;
    public float jumpHeight = 3.5f;
    public float jumpAnimSpeed = 1f;
    [Range(0f, 1f)] public float takeoffPoint = 0.252f;  // จังหวะเท้าพ้นพื้น (สัดส่วนของท่า)
    [Range(0f, 1f)] public float landPoint = 0.427f;     // จังหวะเท้ากระแทกพื้น (โดนครั้งที่ 1)
    [Range(0f, 1f)] public float jumpHitPoint = 0.631f;  // จังหวะหัวขวานกระแทกพื้น (โดนครั้งที่ 2)

    [Header("สกิล 2 ตั้งโล่")]
    public float guardTime = 3f;
    public float guardCooldown = 10f;
    [Tooltip("มุมด้านหน้าที่โล่กันได้ (องศา) นอกมุมนี้ = ตีเข้า")]
    public float guardAngle = 140f;
    [Tooltip("ระหว่างตั้งโล่หันตามผู้เล่นได้กี่องศาต่อวินาที (ช้า ๆ ผู้เล่นจะได้อ้อมไปข้างหลังทัน)")]
    public float guardTurnSpeed = 70f;
    [Range(0f, 1f)] public float guardChance = 0.3f;

    [Header("สกิล 3 เหวี่ยงขวาน (รูปพัดด้านหน้า)")]
    public float sweepDamage = 22f;
    public float sweepRadius = 5.5f;
    public float sweepAngle = 150f;
    public float sweepCooldown = 3.5f;
    [Range(0f, 1f)] public float sweepHitPoint = 0.6f;

    [Header("สกิล 4 สับพื้น (วงกลมรอบตัว)")]
    public float slamDamage = 28f;
    public float slamRadius = 5f;
    public float slamForward = 0.8f;       // กลางวงเลื่อนไปหน้าตัวนิดหน่อย (ขวานลงหน้าตัว)
    public float slamCooldown = 5f;
    [Range(0f, 1f)] public float slamHitPoint = 0.72f;   // ขวานถึงพื้นเฟรม 22/30

    private float lastJumpTime = -99f;
    private float lastGuardTime = -99f;
    private float lastSweepTime = -99f;
    private float lastSlamTime = -99f;
    private bool isGuarding;
    private bool inAir;
    private float groundY;
    // พื้นแดงที่กำลังแสดงอยู่ (ตายกลางท่าต้องลบทิ้งให้หมด)
    private List<AttackIndicator> indicators = new List<AttackIndicator>();
    private MeshFilter axeMesh;        // ขวานในมือ (หาจากชื่อ "Axe")
    private Transform axeHand;         // มือขวา (hand.r) ที่ถือขวาน
    private Vector3 jumpAxeOffset;     // หัวขวานอยู่ตรงไหนเทียบกับตัวบอส ตอนจามโดน (วัดจากท่าจริงตอนเกิด)

    public bool IsGuarding => isGuarding;

    public override void SetUP()
    {
        base.SetUP();
        surroundPlayer = false; // บอสไม่ต้องแย่งช่องล้อมกับลูกน้อง
        isGuarding = false;
        inAir = false;
        lastJumpTime = Time.time - jumpCooldown; // เกิดมาแล้วกระโดดเปิดตัวได้เลยถ้าผู้เล่นอยู่ไกล
        FindAxe();
        MeasureJumpAxeOffset();
        Debug.Log("SetUP " + characterName + " (กระโดดจาม / ตั้งโล่ / เหวี่ยงขวาน / สับพื้น)");
    }

    protected override void Update()
    {
        if (!isAlive) return;

        // 1. ตั้งโล่: ยืนกับที่ หันตามผู้เล่นช้า ๆ
        if (isGuarding)
        {
            Move(Vector3.zero);
            if (target != null)
            {
                Vector3 toTarget = FlatDirectionToTarget();
                if (toTarget.sqrMagnitude > 0.0001f)
                {
                    Quaternion wanted = Quaternion.LookRotation(toTarget);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, guardTurnSpeed * Time.deltaTime);
                }
            }
            return;
        }

        // 2. กำลังทำท่า: Coroutine ของสกิลคุมเอง
        if (isAttacking)
        {
            Move(Vector3.zero);
            return;
        }

        if (data == null || target == null || !target.IsAlive || isStunned)
        {
            base.Update();
            return;
        }

        // 3. ผู้เล่นอยู่ไกล + คูลดาวน์ครบ -> กระโดดจาม
        float distance = FlatDirectionToTarget().magnitude;
        bool jumpReady = Time.time - lastJumpTime >= jumpCooldown;
        if (jumpReady && distance >= jumpMinRange && distance <= jumpMaxRange)
        {
            StartSkill(JumpRoutine());
            return;
        }

        // 4. ปกติ: เดินเข้าหา ถึงระยะแล้ว Enemy เรียก Attack() ให้
        base.Update();
    }

    // สุ่มสกิลระยะประชิด (override จาก Enemy)
    public override void Attack()
    {
        if (isAttacking || !isAlive) return;

        // 1. รวมสกิลโจมตีที่คูลดาวน์ครบลง List
        List<int> ready = new List<int>();
        if (Time.time - lastSlamTime >= slamCooldown) ready.Add(SkillSlam);
        if (Time.time - lastSweepTime >= sweepCooldown) ready.Add(SkillSweep);
        bool guardReady = Time.time - lastGuardTime >= guardCooldown;

        // 2. ตั้งโล่: สุ่มตามโอกาส (หรือไม่มีท่าโจมตีพร้อมเลย)
        if (guardReady && (ready.Count == 0 || Random.value < guardChance))
        {
            StartSkill(GuardRoutine());
            return;
        }
        if (ready.Count == 0) return; // รอคูลดาวน์

        // 3. สุ่มท่าโจมตี
        int skill = ready[Random.Range(0, ready.Count)];
        if (skill == SkillSlam) StartSkill(SlamRoutine());
        else StartSkill(SweepRoutine());
    }

    #region Skills
    // สกิล 4: ยกขวานง้าง -> สับพื้น โดนทุกอย่างในวงกลมรอบตัว
    private IEnumerator SlamRoutine()
    {
        lastSlamTime = Time.time;
        float length = ClipLength(slamClip, data.attackAnimSpeed);
        float hitTime = length * slamHitPoint;

        // 1. พื้นแดงวงกลม
        Vector3 center = transform.position + transform.forward * slamForward;
        AttackIndicator indicator = ShowIndicator(center, transform.forward, slamRadius, 360f, telegraphHold + hitTime);

        // 2. ยืนง้างให้เห็นก่อน แล้วเล่นท่า
        yield return new WaitForSeconds(telegraphHold);
        PlayState("Attack1", data.attackAnimSpeed);
        yield return new WaitForSeconds(hitTime);

        // 3. ขวานถึงพื้น
        HideIndicator(indicator);
        if (TargetInCircle(center, slamRadius)) HitTarget(slamDamage, "สับพื้น");

        yield return new WaitForSeconds(length * (1f - slamHitPoint) * 0.9f);
        EndSkill();
    }

    // สกิล 3: บิดตัวง้าง -> เหวี่ยงขวานกวาด โดนเฉพาะในรูปพัดด้านหน้า (ทิศล็อกตั้งแต่ตอนเริ่ม)
    private IEnumerator SweepRoutine()
    {
        lastSweepTime = Time.time;
        float length = ClipLength(sweepClip, data.attackAnimSpeed);
        float hitTime = length * sweepHitPoint;

        // 1. พื้นแดงรูปพัด
        Vector3 center = transform.position;
        Vector3 forward = transform.forward;
        AttackIndicator indicator = ShowIndicator(center, forward, sweepRadius, sweepAngle, telegraphHold + hitTime);

        // 2. ง้าง แล้วเหวี่ยง
        yield return new WaitForSeconds(telegraphHold);
        PlayState("Attack2", data.attackAnimSpeed);
        yield return new WaitForSeconds(hitTime);

        // 3. ขวานกวาดผ่าน
        HideIndicator(indicator);
        if (TargetInSector(center, forward, sweepRadius, sweepAngle)) HitTarget(sweepDamage, "เหวี่ยงขวาน");

        yield return new WaitForSeconds(length * (1f - sweepHitPoint) * 0.9f);
        EndSkill();
    }

    // สกิล 1: กระโดดจาม Combo 2 จังหวะ
    //   ตอนย่อตัว: วงแดงวงแรก = จุดที่บอสจะลง (ตามผู้เล่นจนกว่าจะกระโดด)
    //   ตัวลงพื้น = โดนครั้งที่ 1 -> แล้ววงที่สองค่อยขึ้น = จุดที่หัวขวานจะจาม -> จามลงพื้น = โดนครั้งที่ 2
    private IEnumerator JumpRoutine()
    {
        lastJumpTime = Time.time;
        float length = ClipLength(jumpClip, jumpAnimSpeed);
        float crouchTime = length * takeoffPoint;
        float airTime = length * (landPoint - takeoffPoint);
        float chopDelay = length * (jumpHitPoint - landPoint); // ลงพื้น -> ขวานถึงพื้น

        // 1. วงแรก: จุดที่บอสจะลง (สีเข้มเต็มวงตอนตัวกระแทกพื้น)
        Vector3 spot = JumpSpot();
        Vector3 land = LandPointFor(spot);
        AttackIndicator landZone = ShowIndicator(land, Vector3.forward, jumpLandRadius, 360f, telegraphHold + crouchTime + airTime);

        // 2. ง้าง + ย่อตัว (วงยังตามผู้เล่นอยู่)
        float timer = 0f;
        bool started = false;
        while (timer < telegraphHold + crouchTime)
        {
            if (!started && timer >= telegraphHold)
            {
                PlayState("Attack3", jumpAnimSpeed);
                started = true;
            }
            spot = JumpSpot();
            land = LandPointFor(spot);
            if (landZone != null) landZone.MoveTo(new Vector3(land.x, GroundY(), land.z));
            FaceDirection(spot - transform.position);
            timer += Time.deltaTime;
            yield return null;
        }
        if (!started) PlayState("Attack3", jumpAnimSpeed);

        // 3. ล็อกจุด หันไปหาจุดนั้นแล้วกระโดด (ลงห่างจากผู้เล่นพอดีระยะหัวขวาน)
        Vector3 start = transform.position;
        Vector3 toSpot = spot - start;
        toSpot.y = 0f;
        if (toSpot.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toSpot);
        land = LandPointFor(spot);
        land.y = start.y;
        groundY = start.y;
        inAir = true;
        controller.enabled = false; // ย้ายตำแหน่งเองตามเส้นโค้ง ไม่ให้ CharacterController ขวาง
        timer = 0f;
        while (timer < airTime)
        {
            float t = timer / airTime;
            transform.position = Vector3.Lerp(start, land, t) + Vector3.up * (4f * jumpHeight * t * (1f - t));
            timer += Time.deltaTime;
            yield return null;
        }
        transform.position = land;
        controller.enabled = true;
        inAir = false;

        // 4. ครั้งที่ 1: ตัวกระแทกพื้น โดนรอบจุดที่ลง
        HideIndicator(landZone);
        if (TargetInCircle(GroundPosition(), jumpLandRadius)) HitTarget(jumpLandDamage, "กระโดดกระแทกพื้น");

        // 5. วงที่สองขึ้นหลังลงพื้น: จุดที่หัวขวานจะจาม (สีเข้มเต็มวงตอนขวานถึงพื้น)
        AttackIndicator chopZone = ShowIndicator(spot, Vector3.forward, jumpRadius, 360f, chopDelay);

        // 6. ครั้งที่ 2: ยกขวานจามลงพื้น โดนตรงหัวขวานจริง
        yield return new WaitForSeconds(chopDelay);
        HideIndicator(chopZone);
        Vector3 hitCenter = AxeHeadPosition();
        hitCenter.y = GroundY();
        RememberJumpAxeOffset();
        Vector3 miss = hitCenter - spot;
        miss.y = 0f;
        Debug.Log(characterName + " ขวานลงห่างจากกลางวงแดง " + miss.magnitude.ToString("0.00") + " ม.");
        if (TargetInCircle(hitCenter, jumpRadius)) HitTarget(jumpDamage, "จามขวาน");

        yield return new WaitForSeconds(length * (1f - jumpHitPoint) * 0.9f);
        EndSkill();
    }

    // จะลงตรงไหน ถ้าอยากให้หัวขวานฟาดลงที่ spot (หันหน้าจากตำแหน่งตอนนี้ไปหา spot)
    private Vector3 LandPointFor(Vector3 spot)
    {
        Vector3 toSpot = spot - transform.position;
        toSpot.y = 0f;
        Quaternion facing = toSpot.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toSpot) : transform.rotation;
        Vector3 land = spot - facing * Vector3.Scale(jumpAxeOffset, transform.localScale);
        land.y = transform.position.y;
        return land;
    }

    // สกิล 2: ตั้งโล่ guardTime วินาที (TakeDamage เช็กว่าโดนจากด้านหน้าหรือหลัง)
    private IEnumerator GuardRoutine()
    {
        lastGuardTime = Time.time;
        isGuarding = true;
        PlayState("Attack4", 1f);
        Debug.Log(characterName + " ตั้งโล่! (ตีด้านหลังได้)");

        // พื้นสีฟ้าด้านหน้า = มุมที่โล่กันได้ หมุนตามบอส (ผู้เล่นเห็นว่าต้องอ้อมไปทางไหน)
        AttackIndicator zone = null;
        if (indicatorMaterial != null)
        {
            zone = AttackIndicator.Create(GroundPosition(), transform.forward, 3f, guardAngle, guardTime,
                indicatorMaterial, AttackIndicator.Blue);
            indicators.Add(zone);
        }
        float timer = 0f;
        while (timer < guardTime)
        {
            if (zone != null)
            {
                zone.MoveTo(GroundPosition());
                zone.FaceTo(transform.forward);
            }
            timer += Time.deltaTime;
            yield return null;
        }
        HideIndicator(zone);
        isGuarding = false;
        EndSkill();
    }
    #endregion

    // ตั้งโล่: โดนจากด้านหน้า = กันได้ / ด้านหลัง-ด้านข้าง = โดนปกติ
    public override void TakeDamage(float damage)
    {
        if (isAlive && isGuarding && target != null)
        {
            Vector3 toAttacker = FlatDirectionToTarget();
            if (Vector3.Angle(transform.forward, toAttacker) <= guardAngle / 2f)
            {
                Debug.Log(characterName + " ใช้โล่กันไว้ได้");
                StartCoroutine(FlashHit(new Color(0.45f, 0.75f, 1f)));
                return;
            }
        }
        base.TakeDamage(damage);
    }

    protected override void Die()
    {
        // ตายกลางอากาศ -> วางลงพื้นก่อน (ไม่งั้นศพลอยค้าง)
        if (inAir)
        {
            Vector3 p = transform.position;
            transform.position = new Vector3(p.x, groundY, p.z);
            inAir = false;
        }
        isGuarding = false;
        for (int i = 0; i < indicators.Count; i++)
        {
            if (indicators[i] != null) Destroy(indicators[i].gameObject);
        }
        indicators.Clear();
        if (animator != null) animator.SetFloat("AttackSpeed", data != null ? data.attackAnimSpeed : 1f);
        base.Die();
    }

    #region Test (คลิก ⋮ ที่คอมโพเนนต์ OrcBoss ตอนกด Play -> สั่งใช้สกิลทันที ไม่ต้องรอสุ่ม)
    [ContextMenu("Test Skill/1 Jump")]
    private void TestJump() { if (CanTest()) StartSkill(JumpRoutine()); }

    [ContextMenu("Test Skill/2 Guard")]
    private void TestGuard() { if (CanTest()) StartSkill(GuardRoutine()); }

    [ContextMenu("Test Skill/3 Sweep")]
    private void TestSweep() { if (CanTest()) StartSkill(SweepRoutine()); }

    [ContextMenu("Test Skill/4 Slam")]
    private void TestSlam() { if (CanTest()) StartSkill(SlamRoutine()); }

    private bool CanTest()
    {
        return Application.isPlaying && isAlive && !isAttacking && target != null && target.IsAlive;
    }
    #endregion

    #region Helpers
    private void StartSkill(IEnumerator routine)
    {
        isAttacking = true;
        if (animator != null) animator.SetFloat("Speed", 0f);
        StartCoroutine(routine);
    }

    private void EndSkill()
    {
        isAttacking = false;
        lastAttackTime = Time.time; // พักระหว่างท่า = attackCooldown ใน EnemyData
        if (animator != null) animator.SetFloat("AttackSpeed", data.attackAnimSpeed);
        ReturnToMove();
    }

    private void PlayState(string state, float speed)
    {
        if (animator == null) return;
        animator.SetFloat("AttackSpeed", speed);
        animator.CrossFadeInFixedTime(state, 0.1f, 0, 0f);
    }

    // ความยาวท่าจริง (วินาที) หลังคูณความเร็ว
    private float ClipLength(AnimationClip clip, float speed)
    {
        if (clip == null || speed <= 0f) return 1f;
        return clip.length / speed;
    }

    // หาขวานในมือ (Renderer ที่ชื่อมี "Axe") กับมือขวาที่ถือ
    private void FindAxe()
    {
        axeMesh = null;
        axeHand = null;
        MeshFilter[] meshes = GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < meshes.Length; i++)
        {
            if (meshes[i].name.Contains("Axe")) { axeMesh = meshes[i]; break; }
        }
        Transform[] all = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].name == "hand.r") { axeHand = all[i]; break; }
        }
    }

    // ตำแหน่งหัวขวานตอนนี้ = ปลายด้ามด้านที่ไกลจากมือ (แนวยาวสุดของโมเดลขวาน)
    private Vector3 AxeHeadPosition()
    {
        if (axeMesh == null || axeMesh.sharedMesh == null)
        {
            return transform.position + transform.forward * 2f * transform.localScale.x;
        }
        Bounds b = axeMesh.sharedMesh.bounds;
        Vector3 axis = new Vector3(b.extents.x, 0f, 0f);
        if (b.extents.y > axis.magnitude) axis = new Vector3(0f, b.extents.y, 0f);
        if (b.extents.z > axis.magnitude) axis = new Vector3(0f, 0f, b.extents.z);
        Vector3 endA = axeMesh.transform.TransformPoint(b.center + axis);
        Vector3 endB = axeMesh.transform.TransformPoint(b.center - axis);
        Vector3 hand = axeHand != null ? axeHand.position : axeMesh.transform.position;
        return (endA - hand).sqrMagnitude > (endB - hand).sqrMagnitude ? endA : endB;
    }

    // ตอนเกิด: สั่ง Animator ไปค้างที่ท่ากระโดดตรงจังหวะขวานโดน 1 เฟรม วัดตำแหน่งหัวขวาน แล้วกลับท่ายืน
    private void MeasureJumpAxeOffset()
    {
        jumpAxeOffset = new Vector3(0f, 0f, 1.5f);
        if (animator == null || axeMesh == null || animator.runtimeAnimatorController == null) return;
        animator.Play("Attack3", 0, jumpHitPoint);
        animator.Update(0f);
        RememberJumpAxeOffset();
        animator.Play("Move", 0, 0f);
        animator.Update(0f);
        Debug.Log(characterName + " หัวขวานตอนจามอยู่ที่ (ซ้าย-ขวา " + (jumpAxeOffset.x * transform.localScale.x).ToString("0.0")
                  + ", หน้า " + (jumpAxeOffset.z * transform.localScale.z).ToString("0.0") + ") ม.");
    }

    // จำว่าตอนจามโดน หัวขวานอยู่ตรงไหนเทียบกับตัวบอส (วัดซ้ำทุกครั้งที่จาม)
    private void RememberJumpAxeOffset()
    {
        Vector3 local = transform.InverseTransformPoint(AxeHeadPosition());
        jumpAxeOffset = new Vector3(local.x, 0f, local.z);
    }

    // จุดที่จะกระโดดไปลง = ตำแหน่งผู้เล่น (ไกลเกิน jumpMaxRange ก็ไปได้แค่นั้น)
    private Vector3 JumpSpot()
    {
        Vector3 toTarget = FlatDirectionToTarget();
        if (toTarget.magnitude > jumpMaxRange) toTarget = toTarget.normalized * jumpMaxRange;
        return transform.position + toTarget;
    }

    // ระดับพื้นใต้เท้าบอส = ก้นแคปซูล CharacterController - skinWidth
    // (ตัวที่ขยายสเกลจะจมพื้นนิดหน่อย ถ้าใช้ transform.position.y ตรง ๆ พื้นแดงจะจมใต้พื้น)
    private float GroundY()
    {
        if (controller == null) return transform.position.y;
        float bottom = (controller.center.y - controller.height / 2f) * transform.localScale.y;
        return transform.position.y + bottom - controller.skinWidth;
    }

    private Vector3 GroundPosition()
    {
        return new Vector3(transform.position.x, GroundY(), transform.position.z);
    }

    private AttackIndicator ShowIndicator(Vector3 center, Vector3 forward, float radius, float angle, float duration)
    {
        if (indicatorMaterial == null) return null;
        Vector3 ground = new Vector3(center.x, GroundY(), center.z);
        AttackIndicator indicator = AttackIndicator.Create(ground, forward, radius, angle, duration, indicatorMaterial);
        indicators.Add(indicator);
        return indicator;
    }

    private void HideIndicator(AttackIndicator indicator)
    {
        if (indicator == null) return;
        indicators.Remove(indicator);
        Destroy(indicator.gameObject);
    }

    // ผู้เล่นอยู่ในวงกลมไหม (เผื่อรัศมีตัวผู้เล่น 0.3)
    private bool TargetInCircle(Vector3 center, float radius)
    {
        if (target == null || !target.IsAlive) return false;
        Vector3 d = target.transform.position - center;
        d.y = 0f;
        return d.magnitude <= radius + 0.3f;
    }

    // ผู้เล่นอยู่ในรูปพัดไหม: ใกล้พอ + อยู่ในมุม
    private bool TargetInSector(Vector3 center, Vector3 forward, float radius, float angle)
    {
        if (target == null || !target.IsAlive) return false;
        Vector3 d = target.transform.position - center;
        d.y = 0f;
        if (d.magnitude > radius + 0.3f) return false;
        if (d.magnitude < 0.5f) return true; // ชิดตัวบอส
        forward.y = 0f;
        return Vector3.Angle(forward, d) <= angle / 2f;
    }

    private void HitTarget(float damage, string skillName)
    {
        target.TakeDamage(damage);
        Debug.Log(characterName + " " + skillName + "โดน " + target.characterName + " : " + damage);
    }

    // วาดระยะสกิลในหน้าต่าง Scene ไว้ปรับค่า
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * slamForward, slamRadius);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sweepRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, jumpMinRange);
        if (Application.isPlaying) Gizmos.DrawWireSphere(AxeHeadPosition(), 0.3f); // หัวขวาน
        Gizmos.DrawWireSphere(transform.position, jumpMaxRange);
    }
    #endregion
}
