// ส่วนบอส (Keerati)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cutscene ตอนบอส Orc เกิด: แถบดำแบบหนัง -> กล้องไปหาบอส -> ยันต์โลหิตเรียกบอสออกมา + จอสั่น
// -> ชื่อบอสขึ้นกลางจอ -> กล้องกลับมาที่ Boat แล้วเริ่มสู้
// หยุดเวลาเกม (timeScale 0) ระหว่าง cutscene บอส/ลูกน้อง/ผู้เล่นจะได้ไม่ขยับ — ทุกอย่างในนี้ใช้เวลาจริง (unscaled)
// ไม่แก้ OrcBoss / DungeonManager / CameraFollow ของเพื่อน แค่ปิดชั่วคราวแล้วเปิดคืน
public class BossCutscene : MonoBehaviour
{
    [SerializeField] private GameObject summonVFX;          // VFX/Boss/CursedTalismans/all.prefab
    [SerializeField] private float summonSize = 7f;
    [SerializeField] private string bossTitle = "ORC WARLORD";
    [SerializeField] private string bossSubtitle = "เจ้าแห่งบัลลังก์โลหิต";
    [SerializeField] private float moveTime = 0.8f;         // กล้องเลื่อนไป/กลับกี่วิ
    [SerializeField] private float revealPoint = 0.55f;     // บอสโผล่ตอนยันต์เล่นไปกี่ส่วน
    [SerializeField] private float holdAfterReveal = 1.6f;  // ค้างให้ดูบอสกับชื่อกี่วิ
    [SerializeField] private float shakeStrength = 0.25f;

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

        // 1. หยุดเกม ปิดกล้องตามตัว ซ่อนตัวบอสไว้ก่อน (โผล่ออกมาจากยันต์)
        float oldScale = Time.timeScale;
        Time.timeScale = 0f;
        if (follow != null) follow.enabled = false;
        if (player != null) player.enabled = false;
        boss.enabled = false; // AI บอสหยุดคิดระหว่าง cutscene (ไม่แก้สคริปต์ แค่ปิดคอมโพเนนต์)
        // ซ่อนเฉพาะชิ้นที่เปิดอยู่ (ชิ้นที่บอสปิดไว้เอง เช่นแสงตอนโดนตี ห้ามเปิดคืน)
        List<Renderer> bossRenderers = new List<Renderer>();
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>())
        {
            if (r.enabled) bossRenderers.Add(r);
        }
        SetVisible(bossRenderers, false);

        GameObject ui = BuildUI(out RectTransform topBar, out RectTransform bottomBar, out Text title, out Text subtitle);

        // 2. แถบดำเข้า + กล้องเลื่อนไปหาบอส (มุมกล้องเดิม แค่ย้ายจุดมอง)
        Vector3 startPos = cam.transform.position;
        Vector3 offset = player != null ? startPos - player.transform.position : new Vector3(0f, 7f, -5.5f);
        Vector3 bossPos = boss.transform.position + offset * 0.8f; // ซูมเข้านิดหน่อย
        yield return Animate(moveTime, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.position = Vector3.Lerp(startPos, bossPos, e);
            SetBars(topBar, bottomBar, e);
        });

        // 3. ยันต์โลหิตเรียกบอส
        GameObject fx = BossSkillVFX.Play(summonVFX, boss.transform.position + Vector3.up * 0.6f, summonSize, true);
        float fxTime = fx != null ? fx.GetComponent<ParticleSystem>().main.duration : 2f;
        yield return Wait(fxTime * revealPoint);

        // 4. บอสโผล่ + จอสั่น + ชื่อขึ้น
        SetVisible(bossRenderers, true);
        float shownTime = 0f;
        while (shownTime < holdAfterReveal)
        {
            shownTime += Time.unscaledDeltaTime;
            float shake = Mathf.Max(0f, 1f - shownTime / 0.6f) * shakeStrength;
            cam.transform.position = bossPos + (Vector3)(Random.insideUnitCircle * shake);
            float a = Mathf.Clamp01(shownTime / 0.3f);
            title.color = new Color(1f, 0.85f, 0.4f, a);
            subtitle.color = new Color(1f, 0.6f, 0.55f, a);
            title.rectTransform.localScale = Vector3.one * Mathf.Lerp(1.4f, 1f, a);
            yield return null;
        }

        // 5. กล้องกลับมาที่ Boat แถบดำออก
        Vector3 backPos = player != null ? player.transform.position + offset : startPos;
        yield return Animate(moveTime * 0.75f, k =>
        {
            float e = Mathf.SmoothStep(0f, 1f, k);
            cam.transform.position = Vector3.Lerp(bossPos, backPos, e);
            SetBars(topBar, bottomBar, 1f - e);
            title.color = new Color(1f, 0.85f, 0.4f, 1f - e);
            subtitle.color = new Color(1f, 0.6f, 0.55f, 1f - e);
        });

        // 6. คืนทุกอย่าง แล้วเริ่มสู้
        Destroy(ui);
        if (fx != null) Destroy(fx);
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
