using UnityEngine;

// เอฟเฟกต์ท่าบอส ไม่แก้ OrcBoss ของเพื่อน
// ดูว่าบอสเล่นท่าไหน ถึงจังหวะดาเมจก็เล่นเอฟเฟกต์
// Attack3 กระโดดจาม = หลุมระเบิด
// Attack1 สับพื้น = หอกผุดรอบตัว
public class BossSkillVFX : MonoBehaviour
{
    [Header("เอฟเฟกต์ (ตั้งให้อัตโนมัติจากเมนู)")]
    [SerializeField] private GameObject craterBlast;
    [SerializeField] private GameObject spearRing;
    [SerializeField] private float craterScale = 1.8f; // ปรับให้พอดีวงดาเมจ
    [SerializeField] private float spearRingScale = 2.2f;
    [SerializeField] private float groundLift = 0.2f; // ยกพ้นพื้น

    private OrcBoss boss;
    private Animator bossAnimator;
    private int lastStateHash;
    private float lastNormalized;
    private float searchTimer;

    // โหลดเอฟเฟกต์ไว้ก่อนเหมือนตอน Boat ได้สกิล
    // ไม่งั้นกระตุกตอนบอสใช้สกิลแรก
    private void Start()
    {
        Camera cam = Camera.main;
        Vector3 near = cam != null ? cam.transform.position + cam.transform.forward * 6f : Vector3.zero;
        VFXManager.Prewarm(craterBlast, near);
        VFXManager.Prewarm(spearRing, near);
    }

    private void Update()
    {
        if (boss == null)
        {
            // บอสเกิดทีหลัง หาทุก 0.25 วิ
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
            lastStateHash = info.fullPathHash; // ท่าใหม่ นับใหม่
            lastNormalized = 0f;
        }

        if (info.IsName("Attack3") && Crossed(boss.jumpHitPoint, t))
        {
            // ตรงหัวขวาน
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

    // ท่าเพิ่งผ่านจุดนี้ในเฟรมนี้
    private bool Crossed(float point, float now)
    {
        return lastNormalized < point && now >= point;
    }

    // ใช้ VFXManager ตัวเดียวกับของ Boat
    private void PlayVFX(GameObject prefab, Vector3 position, float scale)
    {
        Vector3 ground = new Vector3(position.x, boss.transform.position.y + groundLift, position.z);
        Quaternion rotation = Quaternion.Euler(0f, boss.transform.eulerAngles.y, 0f);
        VFXManager.Play(prefab, ground, rotation, scale, 0f);
    }
}
