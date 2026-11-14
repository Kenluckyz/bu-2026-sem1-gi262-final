using System.Collections;
using UnityEngine;

// โกเลม: อึด เดินช้า โดนตีไม่ชะงัก — สืบทอดจาก Enemy
// สกิล 1 ทุ่มพื้น : ตีปกติของ Enemy (Attack1) แต่ override OnAttackHit() ให้โดนทุกอย่างรอบ ๆ (วงกลม)
// สกิล 2 ปาหิน   : ผู้เล่นอยู่ไกล + คูลดาวน์ครบ -> ยกหินเหนือหัวแล้วปา (Attack2) มีคูลดาวน์ของตัวเอง
public class GolemEnemy : Enemy
{
    [Header("Golem - สกิล 1 ทุ่มพื้น")]
    public float slamRadius = 3.2f;        // รัศมีแรงกระแทก (ตอน modelScale = 1 — ตัวใหญ่ขึ้น วงใหญ่ตาม)
    public float slamForward = 1.3f;       // จุดกระแทกอยู่หน้าตัวกี่เมตร (กำปั้นลงหน้าตัว, ขยายตามสเกลเหมือนกัน)

    [Header("Golem - สกิล 2 ปาหิน")]
    public Rock rockPrefab;                // หินที่ปาออกไป
    public GameObject heldRockPrefab;      // หินที่ถือในมือ (แค่โชว์ ระหว่างยกขึ้น)
    public float throwDamage = 14f;
    public float throwCooldown = 6f;       // คูลดาวน์ของท่าปาหิน (วินาที)
    public float throwMinRange = 6.5f;       // ผู้เล่นต้องอยู่ไกลกว่านี้ถึงจะปา (ใกล้กว่านี้ใช้ทุ่มพื้น)
    public float throwMaxRange = 15f;
    public float rockFlightTime = 1.2f;
    public float rockArcHeight = 3f;
    [Range(0f, 1f)] public float pickPoint = 0.21f;     // จังหวะหยิบหิน (สัดส่วนของท่า Throw)
    [Range(0f, 1f)] public float releasePoint = 0.62f;  // จังหวะปล่อยหิน

    private float lastThrowTime = -99f;
    private GameObject heldRock;

    public override void SetUP()
    {
        base.SetUP();
        lastThrowTime = Time.time - throwCooldown * 0.5f; // เกิดมาแล้วรอแป๊บนึงก่อนปาครั้งแรก
        Debug.Log("SetUP " + characterName + " (ทุ่มพื้น / ปาหิน)");
    }

    protected override void Update()
    {
        if (!isAlive) return;

        // 1. กำลังทำท่าอยู่ / ไม่มีเป้า ให้ Enemy จัดการ
        if (data == null || target == null || !target.IsAlive || isAttacking || isStunned)
        {
            base.Update();
            return;
        }

        // 2. สกิล 2: ผู้เล่นอยู่ไกล และคูลดาวน์ปาหินครบ -> หันไปหาแล้วปา
        Vector3 toTarget = FlatDirectionToTarget();
        float distance = toTarget.magnitude;
        bool throwReady = Time.time - lastThrowTime >= throwCooldown;
        if (throwReady && distance >= throwMinRange && distance <= throwMaxRange)
        {
            Move(Vector3.zero);
            FaceDirection(toTarget);
            if (Vector3.Angle(transform.forward, toTarget) < 15f)
            {
                StartCoroutine(ThrowRoutine());
            }
            return;
        }

        // 3. ปกติ: เดินเข้าหา ถึงระยะแล้วทุ่มพื้น (สกิล 1) — ใช้ของ Enemy
        base.Update();
    }

    // สกิล 1: จังหวะกำปั้นกระทบพื้น — โดนถ้าผู้เล่นอยู่ในวงกลมหน้าตัว
    protected override void OnAttackHit()
    {
        if (target == null || !target.IsAlive) return;

        // วงกระแทกขยายตามขนาดตัว (modelScale ใน EnemyData)
        float scale = transform.localScale.x;
        Vector3 center = transform.position + transform.forward * slamForward * scale;
        Vector3 toPlayer = target.transform.position - center;
        toPlayer.y = 0f;
        if (toPlayer.magnitude <= slamRadius * scale)
        {
            target.TakeDamage(Damage);
            Debug.Log(characterName + " ทุ่มพื้นโดน " + target.characterName + " : " + Damage);
        }
    }

    // สกิล 2: ก้มหยิบหิน -> ยกเหนือหัว -> ปา (ท่า Attack2)
    private IEnumerator ThrowRoutine()
    {
        isAttacking = true;
        lastThrowTime = Time.time;

        // 1. เล่นท่า แล้วอ่านความยาวท่าจริง
        float length = 1.6f;
        if (animator != null)
        {
            animator.CrossFadeInFixedTime("Attack2", 0.1f, 0, 0f);
            yield return null;
            AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(0);
            if (!info.IsName("Attack2")) info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.length > 0.05f) length = info.length;
        }

        // 2. จังหวะหยิบ: โชว์หินในมือ
        yield return new WaitForSeconds(length * pickPoint);
        ShowHeldRock(true);

        // 3. จังหวะปล่อย: ซ่อนหินในมือ แล้วปาหินจริงไปหาผู้เล่น
        yield return new WaitForSeconds(length * (releasePoint - pickPoint));
        Vector3 start = HandsPosition();
        ShowHeldRock(false);
        if (rockPrefab != null && target != null && target.IsAlive)
        {
            Vector3 end = target.transform.position + Vector3.up * 0.8f;
            Rock rock = Instantiate(rockPrefab, start, Quaternion.identity);
            rock.Launch(start, end, rockFlightTime, rockArcHeight, ScaleDamage(throwDamage), target);
        }

        // 4. ส่งแขนตามจนจบท่า
        yield return new WaitForSeconds(length * (1f - releasePoint) * 0.9f);
        isAttacking = false;
        ReturnToMove();
    }

    // กลางระหว่างมือสองข้าง (โกเลมเป็น Humanoid -> ขอตำแหน่งกระดูกจาก Animator ได้เลย)
    private Vector3 HandsPosition()
    {
        if (animator != null && animator.isHuman)
        {
            Transform l = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform r = animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (l != null && r != null) return (l.position + r.position) / 2f;
        }
        return transform.position + Vector3.up * 2.6f * transform.localScale.y;
    }

    private void ShowHeldRock(bool show)
    {
        if (heldRockPrefab == null) return;
        if (show && heldRock == null)
        {
            heldRock = Instantiate(heldRockPrefab, HandsPosition(), Quaternion.identity);
        }
        if (!show && heldRock != null)
        {
            Destroy(heldRock);
            heldRock = null;
        }
    }

    // หินในมือตามมือไปตลอดท่า
    private void LateUpdate()
    {
        if (heldRock != null) heldRock.transform.position = HandsPosition();
    }

    protected override void Die()
    {
        ShowHeldRock(false);
        base.Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        float scale = transform.localScale.x;
        Gizmos.DrawWireSphere(transform.position + transform.forward * slamForward * scale, slamRadius * scale);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, throwMinRange);
        Gizmos.DrawWireSphere(transform.position, throwMaxRange);
    }
}
