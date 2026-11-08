using System.Collections;
using UnityEngine;

// สไลม์: ตีใกล้ ความเร็วปานกลาง ตายแล้วแตกเป็นสไลม์เล็ก — สืบทอดจาก Enemy
// ต่างจาก Enemy แค่ override Die() กับ SetUP() (Polymorphism)
// ตัวใหญ่ใน EnemyData มี splitInto = ข้อมูลตัวเล็ก / ตัวเล็กไม่มี splitInto จึงตายเฉย ๆ ไม่แตกต่อ
public class SlimeEnemy : Enemy
{
    [Header("Slime")]
    [Tooltip("ระยะที่สไลม์เล็กกระเด็นออกจากจุดที่ตัวใหญ่ตาย")]
    public float splitSpread = 0.7f;
    [Tooltip("สไลม์เล็กขยายจากเล็ก ๆ จนเต็มตัวภายในกี่วินาที (ดูเหมือนแยกตัวออกมา)")]
    public float popInTime = 0.2f;

    private bool popIn; // ตัวที่เกิดจากการแตกตัว -> ขยายขึ้นตอนเกิด

    public override void SetUP()
    {
        base.SetUP(); // ตั้ง scale ตาม EnemyData.modelScale
        if (popIn) StartCoroutine(PopInRoutine());
    }

    protected override void Die()
    {
        bool isSplitting = data != null && data.splitInto != null && manager != null;

        // 1. แตกตัวก่อน base.Die() — EnemyManager จะได้นับลูกเข้าไปก่อน
        //    ถ้าเรียก base.Die() ก่อน จำนวนศัตรูอาจเหลือ 0 แล้วยิง OnAllDefeated ทั้งที่ลูกยังไม่เกิด
        if (isSplitting)
        {
            for (int i = 0; i < data.splitCount; i++)
            {
                // 2. กระจายลูกเป็นวงรอบตัว (2 ตัว = ซ้าย/ขวา)
                float angle = 360f / data.splitCount * i;
                Vector3 offset = Quaternion.Euler(0f, angle, 0f) * transform.right * splitSpread;
                Enemy child = manager.SpawnEnemy(data.splitInto, transform.position + offset);
                SlimeEnemy slimeChild = child as SlimeEnemy;
                if (slimeChild != null)
                {
                    slimeChild.popIn = true;
                    slimeChild.transform.localScale *= 0.3f; // ย่อทันที กันเฟรมแรกโผล่มาเต็มตัว
                }
            }
            Debug.Log(characterName + " แตกเป็น " + data.splitCount + " ตัว");
        }

        // 3. ตายตามปกติ (แจ้ง OnDied ให้ EnemyManager, ปิด CharacterController)
        base.Die();

        // 4. ตัวที่แตก: หายไปทันที ไม่เล่นท่าตาย ไม่ทิ้งศพ — เหมือนกลายเป็นตัวเล็กไปแล้ว
        //    ตัวเล็ก (ไม่แตก) ยังเล่นท่าตายตามปกติ
        if (isSplitting)
        {
            Destroy(gameObject);
        }
    }

    // ขยายจาก 30% จนเต็มขนาด (ขนาดเต็ม = modelScale ใน EnemyData)
    private IEnumerator PopInRoutine()
    {
        Vector3 fullScale = Vector3.one * (data != null ? data.modelScale : 1f);
        float timer = 0f;
        while (timer < popInTime)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / popInTime);
            transform.localScale = fullScale * Mathf.Lerp(0.3f, 1f, t);
            yield return null;
        }
        transform.localScale = fullScale;
    }
}
