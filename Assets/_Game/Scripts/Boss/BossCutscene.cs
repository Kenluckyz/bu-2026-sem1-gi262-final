// ส่วนบอส (Keerati)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NightForge;

// cutscene ตอนบอสเกิด แถบดำ > กล้องไปหาบอส > ยันต์เรียกบอส > ชื่อบอส > กลับมาที่ Boat
// หยุดเกมไว้ (timeScale 0) เลยใช้เวลาแบบ unscaled ทั้งหมด
// ไม่แก้สคริปต์เพื่อน แค่ปิดไว้แล้วเปิดคืน
// ให้รันก่อนสคริปต์อื่น จะได้จับบอสทันก่อนบอสใช้สกิล
[DefaultExecutionOrder(-100)]
public class BossCutscene : MonoBehaviour
{
    [SerializeField] private GameObject summonVFX;
    [SerializeField] private float summonScale = 1.6f;
    [SerializeField] private float summonLift = 0.25f; // ยกขึ้นนิดนึง ไม่ให้พื้นบัง
    [SerializeField] private float moveTime = 0.8f; // กล้องเลื่อนกี่วิ
    [SerializeField] private float revealPoint = 0.55f; // บอสโผล่ตอนยันต์ไปกี่ส่วน
    [SerializeField] private float faceTime = 1.1f; // ซูมหน้ากี่วิ
    [SerializeField] private float holdAfterReveal = 2.2f; // ช็อตหมุนรอบ + ชื่อ กี่วิ
    [SerializeField] private float shakeStrength = 0.25f;
    [Header("โคลสอัปหน้าบอส (เท่าของความสูงบอส ยิ่งน้อยยิ่งใกล้)")]
    [SerializeField] private float closeStart = 0.4f; // ระยะกล้องตอนเริ่ม
    [SerializeField] private float closeEnd = 0.22f; // ระยะกล้องตอนจบ
    [SerializeField] private float closeZoomFov = 8f; // ซูมกระแทก
    [SerializeField] private float cutsceneFov = 28f; // fov แคบ ไม่เห็นขอบแมพ

    [Header("UI ในซีน (BossFX > BossIntroCanvas) แก้ข้อความ/สี/ตำแหน่งใน Hierarchy ได้")]
    [SerializeField] private GameObject introCanvas;
    [SerializeField] private RectTransform topBar;
    [SerializeField] private RectTransform bottomBar;
    [SerializeField] private Text title;
    [SerializeField] private Text subtitle;
    [SerializeField] private RawImage vignette;
    [SerializeField] private Image flash;
    [SerializeField] private float barHeight = 130f; // แถบดำสูงสุด

    private bool played;
    // โหลดของหนักไว้ก่อน ไม่งั้นกระตุกตอนบอสเกิด
    private NFEffect preparedFX;
    // สีที่ตั้งไว้ในซีน (โค้ดแค่ปรับความใส)
    private Color titleColor;
    private Color subtitleColor;
    private Color vignetteColor;

    private void Start()
    {
        if (introCanvas != null)
        {
            // ฟอนต์ไทยของ Windows ใส่ในซีนไม่ได้ เลยเปลี่ยนตอนเริ่มเกม และโหลดตัวอักษรไว้ก่อน
            Font font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma" }, 32);
            font.RequestCharactersInTexture(title.text + subtitle.text, 96);
            title.font = font;
            subtitle.font = font;
            titleColor = title.color;
            subtitleColor = subtitle.color;
            vignetteColor = vignette.color;
            introCanvas.SetActive(false);
        }
        if (summonVFX != null)
        {
            // สร้างยันต์ไว้ก่อนแบบ Prewarm
            Camera cam = Camera.main;
            Vector3 near = cam != null ? cam.transform.position + cam.transform.forward * 6f : Vector3.zero;
            preparedFX = Instantiate(summonVFX, near, Quaternion.identity).GetComponent<NFEffect>();
            preparedFX.loop = false;
            preparedFX.playing = false;
            preparedFX.time = preparedFX.Duration;
        }
    }

    private void Update()
    {
        if (played) return;
        // ค้างไว้เฟรมสุดท้าย มองไม่เห็น
        if (preparedFX != null && preparedFX.Duration > 0f) preparedFX.time = preparedFX.Duration;
        // หาทุกเฟรม (ตอนแรกหาทุก 0.2 วิ บอสกระโดดใส่ก่อน)
        OrcBoss boss = FindAnyObjectByType<OrcBoss>();
        if (boss == null || !boss.IsAlive) return;
        played = true; // เล่นครั้งเดียว
        StartCoroutine(Play(boss));
    }

    private IEnumerator Play(OrcBoss boss)
    {
        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        Player player = FindAnyObjectByType<Player>();
        if (cam == null || introCanvas == null) yield break;

        // 1. หยุดเกม ปิดกล้องตาม ให้บอสหันมาหาผู้เล่น
        float oldScale = Time.timeScale;
        Time.timeScale = 0f;
        if (follow != null) follow.enabled = false;
        if (player != null) player.enabled = false;
        boss.enabled = false; // ปิด AI บอสไว้ก่อน
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - boss.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) boss.transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        // วัดความสูงบอสไว้ตั้งกล้อง
        List<Renderer> bossRenderers = new List<Renderer>();
        Bounds body = new Bounds(boss.transform.position, Vector3.one);
        bool hasBounds = false;
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue; // ชิ้นที่ปิดอยู่แล้วไม่ต้องเปิด
            bossRenderers.Add(r);
            if (!(r is SkinnedMeshRenderer)) continue; // วัดแค่ตัวโมเดล
            if (!hasBounds) { body = r.bounds; hasBounds = true; }
            else body.Encapsulate(r.bounds);
        }
        float H = Mathf.Clamp(body.size.y, 1.5f, 8f);
        CharacterController cc = boss.GetComponent<CharacterController>();
        if (!hasBounds && cc != null) H = cc.height * boss.transform.localScale.y;
        Vector3 feet = boss.transform.position;
        Vector3 head = new Vector3(feet.x, body.max.y - H * 0.18f, feet.z);
        Vector3 fwd = boss.transform.forward;
        Vector3 right = boss.transform.right;
        SetVisible(bossRenderers, false); // ซ่อนไว้ก่อน ค่อยโผล่จากยันต์

        // เปิด UI cutscene เริ่มจากใส ๆ
        introCanvas.SetActive(true);
        SetBars(0f);
        title.color = WithAlpha(titleColor, 0f);
        subtitle.color = WithAlpha(subtitleColor, 0f);
        vignette.color = WithAlpha(vignetteColor, 0f);
        flash.color = new Color(1f, 1f, 1f, 0f);
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 offset = player != null ? startPos - player.transform.position : new Vector3(0f, 7f, -5.5f);
        float baseFov = cam.fieldOfView;

        // ซ่อน HUD กับ Boat
        List<Canvas> hiddenCanvases = new List<Canvas>();
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.enabled && c.gameObject != introCanvas) { c.enabled = false; hiddenCanvases.Add(c); }
        }
        // ซ่อนพื้นแดงที่ค้างอยู่ด้วย
        List<Renderer> indicatorRenderers = new List<Renderer>();
        foreach (AttackIndicator ind in FindObjectsByType<AttackIndicator>(FindObjectsSortMode.None))
        {
            foreach (Renderer r in ind.GetComponentsInChildren<Renderer>()) if (r.enabled) indicatorRenderers.Add(r);
        }
        SetVisible(indicatorRenderers, false);
        List<Renderer> playerRenderers = new List<Renderer>();
        if (player != null)
        {
            foreach (Renderer r in player.GetComponentsInChildren<Renderer>()) if (r.enabled) playerRenderers.Add(r);
            SetVisible(playerRenderers, false);
        }

        // ช็อต A มองลงที่วงยันต์
        Vector3 wideTarget = feet + Vector3.up * H * 0.15f;
        Vector3 widePos = feet + fwd * H * 1.7f + right * H * 0.5f + Vector3.up * H * 1.5f;
        Quaternion wideRot = Quaternion.LookRotation(wideTarget - widePos);
        // ช็อต B ซูมหน้าบอส
        Vector3 closeFrom = head + fwd * H * closeStart + right * H * 0.3f - Vector3.up * H * 0.1f;
        Vector3 closeTo = head + fwd * H * closeEnd + right * H * 0.08f - Vector3.up * H * 0.12f;

        // 2. แถบดำเข้า กล้องบินไปช็อต A
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(startPos, widePos, e), Quaternion.Slerp(startRot, wideRot, e));
            if (!cam.orthographic) cam.fieldOfView = Mathf.Lerp(baseFov, cutsceneFov, e);
            SetBars(e);
            vignette.color = WithAlpha(vignetteColor, vignetteColor.a * e);
        });

        // 3. ยันต์เรียกบอส กล้องเลื่อนข้างช้าๆ
        // เกมหยุดอยู่ เลยเลื่อนเวลาเอฟเฟกต์เอง
        NFEffect fx = preparedFX; // ใช้ตัวที่เตรียมไว้
        if (fx != null)
        {
            fx.transform.SetPositionAndRotation(feet + Vector3.up * summonLift, Quaternion.Euler(0f, boss.transform.eulerAngles.y, 0f));
            fx.transform.localScale = Vector3.one * summonScale;
            fx.loop = false;
            fx.playing = false;
            fx.time = 0f;
        }
        float fxTime = fx != null && fx.Duration > 0f ? fx.Duration : 2f;
        float summonTime = fxTime * revealPoint;
        Vector3 wideEnd = widePos - right * H * 0.8f - Vector3.up * H * 0.3f;
        yield return Animate(summonTime, k =>
        {
            if (fx != null) fx.time = k * summonTime;
            Vector3 p = Vector3.Lerp(widePos, wideEnd, Mathf.SmoothStep(0f, 1f, k));
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(wideTarget - p));
        });

        // 4. บอสโผล่ แฟลช + ซูมหน้า + จอสั่น
        SetVisible(bossRenderers, true);
        float shownTime = 0f;
        while (shownTime < faceTime)
        {
            shownTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shownTime / faceTime);
            if (fx != null) fx.time = Mathf.Min(fxTime, summonTime + shownTime);
            Vector3 p = Vector3.Lerp(closeFrom, closeTo, 1f - Mathf.Pow(1f - k, 3f)); // เร็วก่อนแล้วช้า
            float shake = Mathf.Max(0f, 1f - shownTime / 0.6f) * shakeStrength;
            p += (Vector3)(Random.insideUnitCircle * shake);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(head - p));
            if (!cam.orthographic) cam.fieldOfView = cutsceneFov - closeZoomFov * Mathf.Max(0f, 1f - shownTime / 0.4f);
            flash.color = new Color(1f, 0.55f, 0.45f, Mathf.Max(0f, 1f - shownTime / 0.35f)); // แฟลชจาง
            yield return null;
        }

        // 5. หมุนกล้องรอบบอส + ชื่อขึ้น
        float heroDist = H * 2.4f;
        float startAngle = 35f, endAngle = -35f; // 0 = หน้าตรง
        shownTime = 0f;
        while (shownTime < holdAfterReveal)
        {
            shownTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shownTime / holdAfterReveal);
            if (fx != null) fx.time = Mathf.Min(fxTime, summonTime + faceTime + shownTime);
            float ang = Mathf.Lerp(startAngle, endAngle, Mathf.SmoothStep(0f, 1f, k));
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
            Vector3 p = feet + dir * heroDist + Vector3.up * H * 0.25f; // กล้องต่ำ มองเงย
            Vector3 look = feet + Vector3.up * H * 0.45f; // ให้ชื่ออยู่ล่าง ไม่บังบอส
            float shake = Mathf.Max(0f, 1f - shownTime / 0.35f) * shakeStrength * 0.6f;
            p += (Vector3)(Random.insideUnitCircle * shake);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(look - p));
            if (!cam.orthographic) cam.fieldOfView = cutsceneFov + 12f; // กว้างขึ้นนิด
            // ชื่อใหญ่แล้วหดลง
            float ta = Mathf.Clamp01(shownTime / 0.25f);
            title.color = WithAlpha(titleColor, ta);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, 1f - Mathf.Pow(1f - ta, 3f));
            float sa = Mathf.Clamp01((shownTime - 0.3f) / 0.4f);
            subtitle.color = WithAlpha(subtitleColor, sa);
            flash.color = new Color(1f, 0.85f, 0.6f, Mathf.Max(0f, 0.6f - shownTime / 0.25f));
            yield return null;
        }

        // 6. กล้องกลับมาที่ Boat
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        Vector3 backPos = player != null ? player.transform.position + offset : startPos;
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, backPos, e), Quaternion.Slerp(fromRot, startRot, e));
            SetBars(1f - e);
            if (!cam.orthographic) cam.fieldOfView = Mathf.Lerp(cutsceneFov, baseFov, e);
            vignette.color = WithAlpha(vignetteColor, vignetteColor.a * (1f - e));
            title.color = WithAlpha(titleColor, 1f - e);
            subtitle.color = WithAlpha(subtitleColor, 1f - e);
        });

        // 7. คืนทุกอย่าง เริ่มสู้
        cam.transform.rotation = startRot;
        if (!cam.orthographic) cam.fieldOfView = baseFov;
        foreach (Canvas c in hiddenCanvases) if (c != null) c.enabled = true;
        SetVisible(playerRenderers, true);
        SetVisible(indicatorRenderers, true);
        introCanvas.SetActive(false);
        if (fx != null) Destroy(fx.gameObject);
        if (follow != null) follow.enabled = true;
        if (boss != null) boss.enabled = true;
        if (player != null && player.IsAlive) player.enabled = true;
        Time.timeScale = oldScale > 0f ? oldScale : 1f;
    }

    private IEnumerator Animate(float duration, System.Action<float> step)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            step(Mathf.Clamp01(t / duration));
            yield return null;
        }
        step(1f);
    }

    private static void SetVisible(List<Renderer> renderers, bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = visible;
        }
    }

    private void SetBars(float k)
    {
        float h = barHeight * k;
        topBar.sizeDelta = new Vector2(0f, h);
        bottomBar.sizeDelta = new Vector2(0f, h);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }
}
