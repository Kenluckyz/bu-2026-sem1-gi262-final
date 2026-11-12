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
// ทำงานก่อนสคริปต์อื่นในเฟรมเดียวกัน (ลำดับ -100) จะได้จับบอสได้ก่อนที่ Update ของบอสจะเริ่มใช้สกิลแรก
[DefaultExecutionOrder(-100)]
public class BossCutscene : MonoBehaviour
{
    [SerializeField] private GameObject summonVFX;          // VFX/BossTalismans/BossTalismans.prefab (3D)
    [SerializeField] private float summonScale = 1.6f;
    [SerializeField] private float summonLift = 0.25f;      // ยกวงยันต์ขึ้นจากพื้น ไม่ให้แผ่นพื้นบัง
    [SerializeField] private string bossTitle = "ORC WARLORD";
    [SerializeField] private string bossSubtitle = "เจ้าแห่งบัลลังก์โลหิต";
    [SerializeField] private float moveTime = 0.8f;         // กล้องเลื่อนไป/กลับกี่วิ
    [SerializeField] private float revealPoint = 0.55f;     // บอสโผล่ตอนยันต์เล่นไปกี่ส่วน
    [SerializeField] private float faceTime = 1.1f;         // โคลสอัปหน้ากี่วิ
    [SerializeField] private float holdAfterReveal = 2.2f;  // ช็อตฮีโร่หมุนรอบตัว + ชื่อ กี่วิ
    [SerializeField] private float shakeStrength = 0.25f;
    [Header("โคลสอัปหน้าบอส (เท่าของความสูงบอส ยิ่งน้อยยิ่งใกล้)")]
    [SerializeField] private float closeStart = 0.4f;   // ระยะกล้องตอนบอสเพิ่งโผล่
    [SerializeField] private float closeEnd = 0.22f;     // ระยะกล้องตอนจบ (ดันเข้ามาถึงตรงนี้)
    [SerializeField] private float closeZoomFov = 8f;   // ซูมกระแทกตอนบอสโผล่ (ลด FOV ชั่วครู่)
    [SerializeField] private float cutsceneFov = 28f;   // เลนส์แคบตลอด cutscene = ภาพแน่น ไม่เห็นแมพ

    private bool played;
    // เตรียมของหนักไว้ตั้งแต่โหลดฉาก (สร้างตอนบอสเกิดจะกระตุกก่อน cutscene เริ่ม)
    private NFEffect preparedFX;
    private Font preparedFont;
    private Texture2D preparedVignette;

    private void Start()
    {
        preparedFont = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma" }, 32);
        preparedFont.RequestCharactersInTexture(bossTitle + bossSubtitle, 96);
        preparedVignette = MakeVignette();
        if (summonVFX != null)
        {
            // สร้างยันต์ไว้หน้ากล้องที่เฟรมสุดท้าย (จางหายแล้ว มองไม่เห็น) ให้ mesh + shader พร้อม แบบ VFXManager.Prewarm
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
        // ยันต์ที่เตรียมไว้ ค้างที่เฟรมสุดท้าย (มองไม่เห็น) จนกว่าจะถึง cutscene
        if (preparedFX != null && preparedFX.Duration > 0f) preparedFX.time = preparedFX.Duration;
        // หาทุกเฟรมจนกว่าจะเจอ (เดิมหาทุก 0.2 วิ บอสเลยกระโดดใส่ไปก่อน cutscene)
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
            if (!(r is SkinnedMeshRenderer)) continue; // วัดเฉพาะตัวโมเดล (ชิ้นอื่นเช่นวงพื้น/แถบเลือดทำให้สูงเกินจริง)
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
        SetVisible(bossRenderers, false); // ซ่อนไว้ โผล่ออกมาจากยันต์

        GameObject ui = BuildUI(out RectTransform topBar, out RectTransform bottomBar, out Text title, out Text subtitle, out RawImage vignette, out Image flash);
        Vector3 startPos = cam.transform.position;
        Quaternion startRot = cam.transform.rotation;
        Vector3 offset = player != null ? startPos - player.transform.position : new Vector3(0f, 7f, -5.5f);
        float baseFov = cam.fieldOfView;

        // ซ่อน HUD กับตัว Boat ระหว่าง cutscene (ไม่บังหน้าบอส ไม่รกจอ)
        List<Canvas> hiddenCanvases = new List<Canvas>();
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            if (c.isRootCanvas && c.enabled && c.gameObject != ui) { c.enabled = false; hiddenCanvases.Add(c); }
        }
        // กันไว้อีกชั้น: ถ้ามีพื้นแดงเตือนท่าค้างอยู่ ซ่อนไว้ระหว่าง cutscene
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

        // ช็อต A: มองลงจากหน้าบอส เห็นวงยันต์เต็มจอ เลนส์แคบ (FOV ต่ำ) ภาพจะมีแต่พื้นกับยันต์ ไม่เห็นขอบแมพ
        Vector3 wideTarget = feet + Vector3.up * H * 0.15f;
        Vector3 widePos = feet + fwd * H * 1.7f + right * H * 0.5f + Vector3.up * H * 1.5f;
        Quaternion wideRot = Quaternion.LookRotation(wideTarget - widePos);
        // ช็อต B: โคลสอัปหน้าบอส มุมต่ำ ค่อย ๆ ดันกล้องเข้า (push-in) + โค้งเข้าหาหน้าตรง
        Vector3 closeFrom = head + fwd * H * closeStart + right * H * 0.3f - Vector3.up * H * 0.1f;
        Vector3 closeTo = head + fwd * H * closeEnd + right * H * 0.08f - Vector3.up * H * 0.12f;

        // 2. แถบดำเข้า + กล้องบินจากมุมเกมไปช็อต A
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(startPos, widePos, e), Quaternion.Slerp(startRot, wideRot, e));
            if (!cam.orthographic) cam.fieldOfView = Mathf.Lerp(baseFov, cutsceneFov, e);
            SetBars(topBar, bottomBar, e);
            vignette.color = new Color(0.12f, 0f, 0f, 0.85f * e);
        });

        // 3. ยันต์โลหิตเรียกบอส กล้องค่อย ๆ เลื่อนข้าง (dolly) ให้ภาพมีชีวิต
        // เวลาเกมหยุดอยู่ NFEffect เดินเองไม่ได้ เลยเลื่อนเวลาของเอฟเฟกต์เองด้วยเวลาจริง
        NFEffect fx = preparedFX; // ใช้ตัวที่เตรียมไว้ตอนโหลดฉาก ไม่สร้างใหม่ตรงนี้
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
        Vector3 wideEnd = widePos - right * H * 0.8f - Vector3.up * H * 0.3f; // หมุนรอบวงยันต์ช้า ๆ
        yield return Animate(summonTime, k =>
        {
            if (fx != null) fx.time = k * summonTime;
            Vector3 p = Vector3.Lerp(widePos, wideEnd, Mathf.SmoothStep(0f, 1f, k));
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(wideTarget - p));
        });

        // 4. บอสโผล่: แฟลชแดงขาวเต็มจอ -> ตัดโคลสอัปหน้าบอส ดันกล้องเข้า + จอสั่น + ซูมกระแทก
        SetVisible(bossRenderers, true);
        float shownTime = 0f;
        while (shownTime < faceTime)
        {
            shownTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shownTime / faceTime);
            if (fx != null) fx.time = Mathf.Min(fxTime, summonTime + shownTime);
            Vector3 p = Vector3.Lerp(closeFrom, closeTo, 1f - Mathf.Pow(1f - k, 3f)); // เร็วตอนแรก ช้าตอนท้าย
            float shake = Mathf.Max(0f, 1f - shownTime / 0.6f) * shakeStrength;
            p += (Vector3)(Random.insideUnitCircle * shake);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(head - p));
            if (!cam.orthographic) cam.fieldOfView = cutsceneFov - closeZoomFov * Mathf.Max(0f, 1f - shownTime / 0.4f);
            flash.color = new Color(1f, 0.55f, 0.45f, Mathf.Max(0f, 1f - shownTime / 0.35f)); // แฟลชจางใน 0.35 วิ
            yield return null;
        }

        // 5. ช็อตฮีโร่: มุมต่ำมาก กล้องหมุนรอบตัวบอสครึ่งวง เห็นเต็มตัวทะมึน + ชื่อกระแทกลงจอ
        float heroDist = H * 2.4f; // ห่างพอเห็นเต็มตัว
        float startAngle = 35f, endAngle = -35f; // องศารอบตัวบอส (0 = หน้าตรง)
        shownTime = 0f;
        while (shownTime < holdAfterReveal)
        {
            shownTime += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(shownTime / holdAfterReveal);
            if (fx != null) fx.time = Mathf.Min(fxTime, summonTime + faceTime + shownTime);
            float ang = Mathf.Lerp(startAngle, endAngle, Mathf.SmoothStep(0f, 1f, k));
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd;
            Vector3 p = feet + dir * heroDist + Vector3.up * H * 0.25f;           // ต่ำระดับเข่า มองเงย
            Vector3 look = feet + Vector3.up * H * 0.45f; // บอสอยู่กลางจอค่อนบน ชื่ออยู่ล่าง ไม่บัง
            float shake = Mathf.Max(0f, 1f - shownTime / 0.35f) * shakeStrength * 0.6f;
            p += (Vector3)(Random.insideUnitCircle * shake);
            cam.transform.SetPositionAndRotation(p, Quaternion.LookRotation(look - p));
            if (!cam.orthographic) cam.fieldOfView = cutsceneFov + 12f;            // กว้างขึ้นนิดให้เห็นทั้งตัว
            // ชื่อกระแทก: ใหญ่ -> ปกติ ใน 0.25 วิ, ซับไตเติลตามมา
            float ta = Mathf.Clamp01(shownTime / 0.25f);
            title.color = new Color(1f, 0.85f, 0.4f, ta);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, 1f - Mathf.Pow(1f - ta, 3f));
            float sa = Mathf.Clamp01((shownTime - 0.3f) / 0.4f);
            subtitle.color = new Color(1f, 0.6f, 0.55f, sa);
            flash.color = new Color(1f, 0.85f, 0.6f, Mathf.Max(0f, 0.6f - shownTime / 0.25f)); // แฟลชตอนชื่อลง
            yield return null;
        }

        // 5. กล้องกลับมาที่ Boat มุมเกมเดิม แถบดำออก
        Vector3 fromPos = cam.transform.position;
        Quaternion fromRot = cam.transform.rotation;
        Vector3 backPos = player != null ? player.transform.position + offset : startPos;
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.SetPositionAndRotation(Vector3.Lerp(fromPos, backPos, e), Quaternion.Slerp(fromRot, startRot, e));
            SetBars(topBar, bottomBar, 1f - e);
            if (!cam.orthographic) cam.fieldOfView = Mathf.Lerp(cutsceneFov, baseFov, e);
            vignette.color = new Color(0.12f, 0f, 0f, 0.85f * (1f - e));
            title.color = new Color(1f, 0.85f, 0.4f, 1f - e);
            subtitle.color = new Color(1f, 0.6f, 0.55f, 1f - e);
        });

        // 6. คืนทุกอย่าง แล้วเริ่มสู้
        cam.transform.rotation = startRot;
        if (!cam.orthographic) cam.fieldOfView = baseFov;
        foreach (Canvas c in hiddenCanvases) if (c != null) c.enabled = true;
        SetVisible(playerRenderers, true);
        SetVisible(indicatorRenderers, true);
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

    private GameObject BuildUI(out RectTransform top, out RectTransform bottom, out Text title, out Text subtitle, out RawImage vignette, out Image flash)
    {
        GameObject canvasObj = new GameObject("BossIntroCanvas", typeof(Canvas), typeof(CanvasScaler));
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 60; // ทับ HUD
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // ขอบจอมืดแดง (vignette) กลบฉากหลัง ให้สายตาไปอยู่ที่บอสตรงกลาง
        GameObject vig = new GameObject("Vignette", typeof(RectTransform), typeof(RawImage));
        vig.transform.SetParent(canvasObj.transform, false);
        RectTransform vrt = vig.GetComponent<RectTransform>();
        vrt.anchorMin = Vector2.zero; vrt.anchorMax = Vector2.one; vrt.offsetMin = vrt.offsetMax = Vector2.zero;
        RawImage raw = vig.GetComponent<RawImage>();
        raw.texture = preparedVignette != null ? preparedVignette : MakeVignette();
        raw.raycastTarget = false;
        raw.color = new Color(0.12f, 0f, 0f, 0f);
        vignette = raw;
        // แฟลชเต็มจอตอนบอสโผล่
        GameObject fl = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        fl.transform.SetParent(canvasObj.transform, false);
        RectTransform frt = fl.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.offsetMin = frt.offsetMax = Vector2.zero;
        flash = fl.GetComponent<Image>();
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.raycastTarget = false;

        top = MakeBar(canvasObj.transform, "TopBar", new Vector2(0f, 1f));
        bottom = MakeBar(canvasObj.transform, "BottomBar", new Vector2(0f, 0f));

        Font font = preparedFont != null ? preparedFont : Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Tahoma" }, 32);
        title = MakeText(canvasObj.transform, "Title", font, 96, new Vector2(0f, 260f));
        title.text = bossTitle;
        subtitle = MakeText(canvasObj.transform, "Subtitle", font, 36, new Vector2(0f, 185f));
        subtitle.text = bossSubtitle;
        return canvasObj;
    }

    // ภาพวงรีมืดขอบจอ (กลางใส ขอบทึบ) สร้างเองไม่ต้องมีไฟล์รูป
    private static Texture2D MakeVignette()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.2f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
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
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // ชิดล่าง เหนือแถบดำ ไม่บังตัวบอส
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
