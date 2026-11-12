// ส่วนบอส (Keerati)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using NightForge;

// Cutscene ตอนบอส Orc เกิด: แถบดำแบบหนัง -> กล้องไปหาบอส -> ยันต์โลหิตเรียกบอสออกมา + จอสั่น
// -> ชื่อบอสขึ้นกลางจอ -> กล้องกลับมาที่ Boat แล้วเริ่มสู้
// หยุดเวลาเกม (timeScale 0) ระหว่าง cutscene บอส/ลูกน้อง/ผู้เล่นจะได้ไม่ขยับ — ทุกอย่างในนี้ใช้เวลาจริง (unscaled)
// ไม่แก้ OrcBoss / DungeonManager / CameraFollow ของเพื่อน แค่ปิดชั่วคราวแล้วเปิดคืน
public class BossCutscene : MonoBehaviour
{
    [SerializeField] private GameObject summonVFX;          // VFX/BossTalismans/BossTalismans.prefab (3D)
    [SerializeField] private float summonScale = 1.6f;
    [SerializeField] private string bossTitle = "ORC WARLORD";
    [SerializeField] private string bossSubtitle = "เจ้าแห่งบัลลังก์โลหิต";
    [SerializeField] private float moveTime = 0.8f;         // กล้องเลื่อนไป/กลับกี่วิ
    [SerializeField] private float revealPoint = 0.55f;     // บอสโผล่ตอนยันต์เล่นไปกี่ส่วน
    [SerializeField] private float holdAfterReveal = 1.6f;  // ค้างให้ดูบอสกับชื่อกี่วิ
    [SerializeField] private float shakeStrength = 0.25f;
    [Header("โคลสอัปหน้าบอส (เท่าของความสูงบอส ยิ่งน้อยยิ่งใกล้)")]
    [SerializeField] private float closeStart = 0.75f;  // ระยะกล้องตอนบอสเพิ่งโผล่
    [SerializeField] private float closeEnd = 0.4f;     // ระยะกล้องตอนจบ (ดันเข้ามาถึงตรงนี้)
    [SerializeField] private float closeZoomFov = 12f;  // ลด FOV ตอนโคลสอัป (ซูมเลนส์เข้าอีก)

    private bool played;
    private float searchTimer;

    private void Update()
    {
        if (played) return;
        searchTimer -= Time.unscaledDeltaTime;
        if (searchTimer > 0f) return;
        searchTimer = 0.2f;
        OrcBoss boss = FindAnyObjectByType<OrcBoss>();
        if (boss == null || !boss.IsAlive) return;
        played = true; // เล่นครั้งเดียวต่อด่าน
        StartCoroutine(Play(boss));
    }

    private IEnumerator Play(OrcBoss boss)
    {
        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        Player player = FindAnyObjectByType<Player>();
        if (cam == null) yield break;

        // 1. หยุดเกม ปิดกล้องตามตัว ให้บอสหันหาผู้เล่น (กล้องจะไปยืนหน้าบอส ระหว่างบอสกับ Boat)
        float oldScale = Time.timeScale;
        Time.timeScale = 0f;
        if (follow != null) follow.enabled = false;
        if (player != null) player.enabled = false;
        boss.enabled = false; // AI บอสหยุดคิดระหว่าง cutscene (ไม่แก้สคริปต์ แค่ปิดคอมโพเนนต์)
        if (player != null)
        {
            Vector3 toPlayer = player.transform.position - boss.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) boss.transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        // วัดความสูงตัวบอส (ก่อนซ่อน) ไว้คำนวณมุมกล้อง: H = สูงทั้งตัว, head = ระดับหน้า
        List<Renderer> bossRenderers = new List<Renderer>();
        Bounds body = new Bounds(boss.transform.position, Vector3.one);
        bool hasBounds = false;
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (!r.enabled) continue; // ชิ้นที่บอสปิดไว้เอง (แสงตอนโดนตี) ห้ามเปิดคืน
            bossRenderers.Add(r);
            if (!(r is SkinnedMeshRenderer) && !(r is MeshRenderer)) continue;
            if (!hasBounds) { body = r.bounds; hasBounds = true; }
            else body.Encapsulate(r.bounds);
        }
        float H = Mathf.Max(1.5f, body.size.y);
        Vector3 feet = boss.transform.position;
        Vector3 head = new Vector3(feet.x, body.max.y - H * 0.18f, feet.z);
        Vector3 fwd = boss.transform.forward;
        Vector3 right = boss.transform.right;
        SetVisible(bossRenderers, false); // ซ่อนไว้ โผล่ออกมาจากยันต์

        GameObject ui = BuildUI(out RectTransform topBar, out RectTransform bottomBar, out Text title, out Text subtitle);
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 offset = player != null ? startPos - player.transform.position : new Vector3(0f, 7f, -5.5f);
        float baseFov = cam.fieldOfView;

        // ช็อต A: มุมต่ำกว้าง ๆ หน้าบอสเยื้องขวา เห็นวงยันต์ทั้งวง (กล้องมองเงยขึ้นนิด ๆ ให้บอสดูยักษ์)
        Vector3 wideTarget = feet + Vector3.up * H * 0.45f;
        Vector3 widePos = feet + fwd * H * 2.6f + right * H * 0.9f + Vector3.up * H * 0.35f;
        Quaternion wideRot = Quaternion.LookRotation(wideTarget - widePos);
        // ช็อต B: โคลสอัปหน้าบอส มุมต่ำ ค่อย ๆ ดันกล้องเข้า (push-in) + โค้งเข้าหาหน้าตรง
        Vector3 closeFrom = head + fwd * H * closeStart + right * H * 0.3f - Vector3.up * H * 0.1f;
        Vector3 closeTo = head + fwd * H * closeEnd + right * H * 0.08f - Vector3.up * H * 0.12f;

        // 2. แถบดำเข้า + กล้องบินจากมุมเกมไปช็อต A
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(startPos, widePos, e), Quaternion.Slerp(startRot, wideRot, e));
            SetBars(topBar, bottomBar, e);
        });

        // 3. ยันต์โลหิตเรียกบอส กล้องค่อย ๆ เลื่อนข้าง (dolly) ให้ภาพมีชีวิต
        // เวลาเกมหยุดอยู่ NFEffect เดินเองไม่ได้ เลยเลื่อนเวลาของเอฟเฟกต์เองด้วยเวลาจริง
        NFEffect fx = null;
        if (summonVFX != null)
        {
            fx = Instantiate(summonVFX, feet, Quaternion.Euler(0f, boss.transform.eulerAngles.y, 0f)).GetComponent<NFEffect>();
            fx.transform.localScale = Vector3.one * summonScale;
            fx.loop = false;
            fx.playing = false;
            fx.time = 0f;
        }
        float fxTime = fx != null && fx.Duration > 0f ? fx.Duration : 2f;
        float summonTime = fxTime * revealPoint;
        Vector3 wideEnd = widePos - right * H * 0.5f + fwd * H * 0.2f;
        yield return Animate(summonTime, k =>
        {
            if (fx != null) fx.time = k * summonTime;
            Vector3 p = Vector3.Lerp(widePos, wideEnd, Mathf.SmoothStep(0f, 1f, k));
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(wideTarget - p));
        });

        // 4. บอสโผล่ -> ตัดเป็นโคลสอัปหน้า + จอสั่น + ซูมกระแทก (FOV แคบลงแล้วคืน) + ชื่อขึ้น
        SetVisible(bossRenderers, true);
        float shownTime = 0f;
        while (shownTime < holdAfterReveal)
        {
            shownTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shownTime / holdAfterReveal);
            if (fx != null) fx.time = Mathf.Min(fxTime, summonTime + shownTime);
            Vector3 p = Vector3.Lerp(closeFrom, closeTo, 1f - Mathf.Pow(1f - k, 3f)); // เร็วตอนแรก ช้าตอนท้าย
            float shake = Mathf.Max(0f, 1f - shownTime / 0.6f) * shakeStrength;
            p += (Vector3)(Random.insideUnitCircle * shake);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(head - p));
            if (!cam.orthographic) cam.fieldOfView = baseFov - closeZoomFov - 10f * Mathf.Max(0f, 1f - shownTime / 0.4f); // แคบลง = ซูมเข้าหน้า
            float a = Mathf.Clamp01(shownTime / 0.3f);
            title.color = new Color(1f, 0.85f, 0.4f, a);
            subtitle.color = new Color(1f, 0.6f, 0.55f, a);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, a);
            yield return null;
        }
        if (!cam.orthographic) cam.fieldOfView = baseFov;

        // 5. กล้องกลับมาที่ Boat มุมเกมเดิม แถบดำออก
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        Vector3 backPos = player != null ? player.transform.position + offset : startPos;
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, backPos, e), Quaternion.Slerp(fromRot, startRot, e));
            SetBars(topBar, bottomBar, 1f - e);
            title.color = new Color(1f, 0.85f, 0.4f, 1f - e);
            subtitle.color = new Color(1f, 0.6f, 0.55f, 1f - e);
        });

        // 6. คืนทุกอย่าง แล้วเริ่มสู้
        cam.transform.rotation = startRot;
        Destroy(ui);
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

    private IEnumerator Wait(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static void SetVisible(List<Renderer> renderers, bool visible)
    {
        for (int i = 0; i < renderers.Count; i++)
        {
            if (renderers[i] != null) renderers[i].enabled = visible;
        }
    }

    private static void SetBars(RectTransform top, RectTransform bottom, float k)
    {
        float h = 130f * k; // แถบดำสูงสุด 130 px (จอ 1080)
        top.sizeDelta = new Vector2(0f, h);
        bottom.sizeDelta = new Vector2(0f, h);
    }

    private GameObject BuildUI(out RectTransform top, out RectTransform bottom, out Text title, out Text subtitle)
    {
        GameObject canvasObj = new GameObject("BossIntroCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // ทับ HUD
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        top = MakeBar(canvasObj.transform, "TopBar", new Vector2(0f, 1f));
        bottom = MakeBar(canvasObj.transform, "BottomBar", new Vector2(0f, 0f));

        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma" }, 32);
        title = MakeText(canvasObj.transform, "Title", font, 96, new Vector2(0f, -330f));
        title.text = bossTitle;
        subtitle = MakeText(canvasObj.transform, "Subtitle", font, 36, new Vector2(0f, -410f));
        subtitle.text = bossSubtitle;
        return canvasObj;
    }

    private static RectTransform MakeBar(Transform parent, string objName, Vector2 anchorY)
    {
        GameObject bar = new GameObject(objName, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, anchorY.y);
        rt.anchorMax = new Vector2(1f, anchorY.y);
        rt.pivot = new Vector2(0.5f, anchorY.y);
        rt.sizeDelta = Vector2.zero;
        bar.GetComponent<Image>().color = Color.black;
        return rt;
    }

    private static Text MakeText(Transform parent, string objName, Font font, int size, Vector2 pos)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(1400f, 130f);
        rt.anchoredPosition = pos;
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(1f, 1f, 1f, 0f);
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }
}
