using UnityEngine;

// เอฟเฟกต์ท่าของบอส Orc + cutscene ตอนบอสเกิด — ไม่แก้ OrcBoss.cs ของเพื่อน
// ดูว่าบอสกำลังเล่นท่าไหน (Animator) แล้วเล่นเอฟเฟกต์ตอนท่าถึงจังหวะเดียวกับที่ OrcBoss ใช้ทำดาเมจ
//   Attack3 (กระโดดจาม) ถึง jumpHitPoint -> หลุมระเบิด ตรงหัวขวาน
//   Attack1 (สับพื้น)   ถึง slamHitPoint -> หอกผุดทะลวงวง รอบวงดาเมจ
public class BossSkillVFX : MonoBehaviour
{
    [Header("เอฟเฟกต์ (ตั้งให้อัตโนมัติจากเมนู)")]
    [SerializeField] private GameObject craterBlast;   // VFX/Boss/CraterBlast/all.prefab
    [SerializeField] private GameObject spearRing;     // VFX/Boss/SpearRing/all.prefab
    [SerializeField] private float craterSize = 3.5f;  // ขนาด (เมตร) ปรับให้พอดีวงดาเมจ
    [SerializeField] private float spearRingSize = 6f;
    [SerializeField] private float groundLift = 0.6f;  // ยกขึ้นจากพื้นนิดหน่อย ไม่จมพื้น

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
            Play(craterBlast, pos + Vector3.up * groundLift, craterSize);
        }
        if (info.IsName("Attack1") && Crossed(boss.slamHitPoint, t))
        {
            Vector3 pos = boss.transform.position + boss.transform.forward * boss.slamForward;
            Play(spearRing, pos + Vector3.up * groundLift, spearRingSize);
        }
        lastNormalized = t;
    }

    // จังหวะท่าเพิ่งผ่านจุด point ในเฟรมนี้
    private bool Crossed(float point, float now)
    {
        return lastNormalized < point && now >= point;
    }

    // เล่นเอฟเฟกต์ sprite sheet ครั้งเดียวแล้วลบทิ้ง
    public static GameObject Play(GameObject prefab, Vector3 position, float size, bool unscaled = false)
    {
        if (prefab == null) return null;
        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        ParticleSystem ps = fx.GetComponent<ParticleSystem>();
        if (ps == null) return fx;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.loop = false;                      // ไฟล์จาก Lab ตั้งวนไว้ เราเล่นรอบเดียว
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.useUnscaledTime = unscaled;        // cutscene หยุดเวลาเกม เอฟเฟกต์ยังต้องเล่น
        fx.transform.localScale = Vector3.one * (size / 2f); // prefab สูง 2 เมตร
        ps.Play(true);
        Destroy(fx, main.duration + 0.2f);      // Destroy ใช้เวลาเกม ถ้าหยุดเวลาจะลบทีหลังเอง
        return fx;
    }
}
