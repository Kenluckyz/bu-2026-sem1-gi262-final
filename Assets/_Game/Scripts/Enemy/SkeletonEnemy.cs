using UnityEngine;

// โครงกระดูก: ศัตรูตีไกล — สืบทอดจาก Enemy (Inheritance แบบ RangeEnemy : Enemy ใน week03)
// ต่างจาก Enemy แค่ 2 อย่าง (Polymorphism):
//   1. override OnAttackHit() : ตีไม่โดนตรง ๆ แต่ปาหินออกไปแทน
//   2. override Update()      : ผู้เล่นเข้าใกล้เกินไป ถอยหนีก่อน แล้วค่อยปาใหม่
public class SkeletonEnemy : Enemy
{
    [Header("Skeleton - ปาหิน")]
    public Rock rockPrefab;            // ลูกหินที่จะปา
    public Transform throwHand;        // มือขวา (Wrist_R) หินออกจากตรงนี้
    public float flightTime = 0.9f;    // หินลอยกี่วินาทีถึงเป้า
    public float arcHeight = 1.5f;     // ความสูงของวิถีโค้ง

    [Header("Skeleton - ระยะ")]
    [Tooltip("ผู้เล่นเข้ามาใกล้กว่านี้ โครงกระดูกจะถอยหนี")]
    public float fleeRange = 3.5f;

    public override void SetUP()
    {
        base.SetUP();
        surroundPlayer = false; // ตีไกล ยืนตรงไหนก็ปาได้ ไม่ต้องเข้าไปแย่งช่องล้อมกับพวกตีใกล้
        Debug.Log("SetUP " + characterName + " (ปาหิน)");
    }

    protected override void Update()
    {
        if (!isAlive) return;

        // 1. กำลังปา / ชะงัก / ไม่มีเป้า ให้ Enemy จัดการตามปกติ
        if (data == null || target == null || !target.IsAlive || isAttacking || isStunned)
        {
            base.Update();
            return;
        }

        // 2. ผู้เล่นใกล้เกินไป ถอยหลังหนี (หันหน้าเข้าหาผู้เล่นไว้ จะได้ปาต่อได้ทันที)
        Vector3 toTarget = FlatDirectionToTarget();
        if (toTarget.magnitude < fleeRange)
        {
            FaceDirection(toTarget);
            Move(-toTarget.normalized * 0.8f);
            return;
        }

        // 3. ระยะปกติ ไล่/ปา เหมือน Enemy ทั่วไป
        base.Update();
    }

    // จังหวะมือปล่อยหิน (hitPoint ใน EnemyData)
    protected override void OnAttackHit()
    {
        if (rockPrefab == null || target == null || !target.IsAlive) return;

        // 1. จุดเริ่ม: มือขวา (ไม่มีก็ใช้ระดับอก)
        Vector3 start = throwHand != null ? throwHand.position : transform.position + Vector3.up * 1.2f;
        // 2. เป้าหมาย: ตำแหน่งผู้เล่นตอนปล่อยหิน — ผู้เล่นเดินหลบได้
        Vector3 end = target.transform.position + Vector3.up * 0.8f;

        // 3. สร้างหินแล้วสั่งให้บินไป
        Rock rock = Instantiate(rockPrefab, start, Quaternion.identity);
        rock.Launch(start, end, flightTime, arcHeight, Damage, target);
    }
}
