using UnityEngine;

// เอฟเฟกต์ท่าของบอส Orc + cutscene ตอนบอสเกิด — ไม่แก้ OrcBoss.cs ของเพื่อน
// ดูว่าบอสกำลังเล่นท่าไหน (Animator) แล้วเล่นเอฟเฟกต์ตอนท่าถึงจังหวะเดียวกับที่ OrcBoss ใช้ทำดาเมจ
//   Attack3 (กระโดดจาม) ถึง jumpHitPoint -> หลุมระเบิด ตรงหัวขวาน
//   Attack1 (สับพื้น)   ถึง slamHitPoint -> หอกผุดทะลวงวง รอบวงดาเมจ
public class BossSkillVFX : MonoBehaviour
{
    [Header("เอฟเฟกต์ (ตั้งให้อัตโนมัติจากเมนู)")]
    [SerializeField] private GameObject craterBlast;   // VFX/BossCraterBlast/BossCraterBlast.prefab (3D แบบสกิลของ Boat)
    [SerializeField] private GameObject spearRing;     // VFX/BossSpearRing/BossSpearRing.prefab
    [SerializeField] private float craterScale = 1.8f;   // ขยายจากขนาดใน Lab ปรับให้พอดีวงดาเมจ
    [SerializeField] private float spearRingScale = 2.2f;
    [SerializeField] private float groundLift = 0.05f;   // ยกจากพื้นนิดเดียว ไม่จมพื้น

    private OrcBoss boss;
    private Animator bossAnimator;
    private int lastStateHash;
    private float lastNormalized;
    private float searchTimer;

    private void Update()
    {
        if (boss == null)
        {
            // บอสเกิดทีหลัง (ตอนเข้าห้องบอส) หาทุก 0.25 วิ
            searchTimer -= Time.unscaledDeltaTime;
            if (searchTimer > 0f) return;
            searchTimer = 0.25f;
            boss = FindAnyObjectByType<OrcBoss>();
            if (boss == null) return;
            bossAnimator = boss.GetComponentInChildren<Animator>();
            lastStateHash = 0;
            return;
        }
        if (!boss.IsAlive || bossAnimator == null) return;

        AnimatorStateInfo info = bossAnimator.GetCurrentAnimatorStateInfo(0);
        float t = info.normalizedTime % 1f;
        if (info.fullPathHash != lastStateHash)
        {
            lastStateHash = info.fullPathHash; // เข้าท่าใหม่ เริ่มนับจังหวะใหม่
            lastNormalized = 0f;
        }

        if (info.IsName("Attack3") && Crossed(boss.jumpHitPoint, t))
        {
            // หัวขวานลงข้างหน้าบอส
            Vector3 pos = boss.transform.position + boss.transform.forward * 2f * boss.transform.localScale.x;
            PlayVFX(craterBlast, pos, craterScale);
        }
        if (info.IsName("Attack1") && Crossed(boss.slamHitPoint, t))
        {
            Vector3 pos = boss.transform.position + boss.transform.forward * boss.slamForward;
            PlayVFX(spearRing, pos, spearRingScale);
        }
        lastNormalized = t;
    }

    // จังหวะท่าเพิ่งผ่านจุด point ในเฟรมนี้
    private bool Crossed(float point, float now)
    {
        return lastNormalized < point && now >= point;
    }

    // ใช้ VFXManager เดียวกับสกิลของ Boat (Object Pool) หันตามบอส เล่นตามความยาวจริงของเอฟเฟกต์
    private void PlayVFX(GameObject prefab, Vector3 position, float scale)
    {
        Vector3 ground = new Vector3(position.x, boss.transform.position.y + groundLift, position.z);
        Quaternion rotation = Quaternion.Euler(0f, boss.transform.eulerAngles.y, 0f);
        VFXManager.Play(prefab, ground, rotation, scale, 0f);
    }
}
