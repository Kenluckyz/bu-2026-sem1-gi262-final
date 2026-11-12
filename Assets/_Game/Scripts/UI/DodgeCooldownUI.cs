using UnityEngine;
using UnityEngine.UI;

// ช่องคูลดาวน์ของ Shift หลบ อยู่ข้างแถบสกิล — หน้าตาเดียวกับช่องสกิล (วงกลม + วงแหวน + เงาหมุนตามเวลา)
// สร้าง UI ด้วยโค้ดตอนเริ่มเกม ใส่ลงใต้ Canvas "HUD" เดิม ไม่ต้องแก้ HUD
public class DodgeCooldownUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Sprite slotBackground; // circle_slot
    [SerializeField] private Sprite slotRing;       // skill_slot_ring
    [SerializeField] private Sprite maskSprite;     // circle_white
    [SerializeField] private Vector2 position = new Vector2(250f, 54f); // จากกลางล่างจอ (ถัดจากช่อง 3)
    [SerializeField] private float size = 96f;

    private Image mask;
    private Text cooldownText;
    private Text label;

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        GameObject hud = GameObject.Find("HUD");
        if (player == null || hud == null)
        {
            Debug.LogWarning("DodgeCooldownUI: ไม่พบ Player หรือ Canvas HUD");
            return;
        }
        Build(hud.transform);
    }

    private void Update()
    {
        if (mask == null) return;
        float remaining = player.DodgeCooldownRemaining;
        mask.fillAmount = player.DodgeCooldown > 0f ? remaining / player.DodgeCooldown : 0f;
        cooldownText.text = remaining > 0f ? remaining.ToString("0.0") : "";
        label.enabled = remaining <= 0f; // พร้อมกด = โชว์คำว่า DODGE
    }

    private void Build(Transform hud)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject slot = new GameObject("DodgeSlot", typeof(RectTransform));
        slot.transform.SetParent(hud, false);
        RectTransform rt = slot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(size, size);

        Image bg = MakeImage(slot.transform, "BG", slotBackground, 0.86f);
        bg.color = new Color(0.2f, 0.35f, 0.5f);
        label = MakeText(slot.transform, "Label", font, 18, new Color(0.75f, 0.92f, 1f));
        label.text = "DODGE";
        mask = MakeImage(slot.transform, "Cooldown", maskSprite, 0.86f);
        mask.color = new Color(0f, 0f, 0f, 0.6f);
        mask.type = Image.Type.Filled;
        mask.fillMethod = Image.FillMethod.Radial360;
        mask.fillOrigin = (int)Image.Origin360.Top;
        mask.fillClockwise = false;
        mask.fillAmount = 0f;
        cooldownText = MakeText(slot.transform, "CooldownText", font, 26, Color.white);
        MakeImage(slot.transform, "Ring", slotRing, 1f);

        Text key = MakeText(slot.transform, "Key", font, 18, Color.white);
        key.text = "Shift";
        key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        key.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        key.rectTransform.sizeDelta = new Vector2(80f, 26f);
    }

    private Image MakeImage(Transform parent, string objName, Sprite sprite, float scale)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size * scale, size * scale);
        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private Text MakeText(Transform parent, string objName, Font font, int fontSize, Color color)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(size, size);
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }
}
