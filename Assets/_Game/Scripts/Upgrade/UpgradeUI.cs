using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// หน้าเลือกอัปเกรด 3 ใบ เปิดตอนเลเวลอัป หยุดเกมไว้จนกว่าจะเลือก
// สร้าง UI ด้วยโค้ดตอนเริ่มเกม (แบบ GameplayHUD) ไม่ต้องลากต่อชิ้นเอง
public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private UpgradeManager upgradeManager;
    [SerializeField] private int optionCount = 3;

    [Header("รูป (ตั้งให้อัตโนมัติจากเมนู)")]
    [SerializeField] private Sprite iconBackground; // circle_slot
    [SerializeField] private Sprite iconRing;       // skill_slot_ring

    // สีโทนไม้ให้เข้ากับ HUD ของ Boat
    private static readonly Color WoodDark = new Color(0.24f, 0.14f, 0.07f, 0.97f);
    private static readonly Color WoodLight = new Color(0.47f, 0.29f, 0.14f, 1f);
    private static readonly Color Gold = new Color(1f, 0.85f, 0.4f);
    private static readonly Color Cream = new Color(1f, 0.95f, 0.85f);

    // เลเวลอัปที่ยังไม่ได้เลือก (Queue แบบ ActionHistoryManager week09) — ขึ้นหลายเลเวลทีเดียวก็ได้เลือกครบ
    private Queue<int> pendingLevelUps = new Queue<int>();
    private List<UpgradeData> currentOptions = new List<UpgradeData>();
    private bool isOpen;
    private bool enablePlayerNextFrame;
    private float previousTimeScale = 1f;

    private Font font;
    private GameObject panel;
    private Text titleText;
    private List<Card> cards = new List<Card>();

    private class Card
    {
        public GameObject root;
        public Image icon;
        public Text iconLabel;
        public Text nameText;
        public Text levelText;
        public Text descText;
    }

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        if (upgradeManager == null) upgradeManager = GetComponent<UpgradeManager>();
        if (player == null || upgradeManager == null)
        {
            Debug.LogWarning("UpgradeUI: ไม่พบ Player หรือ UpgradeManager");
            return;
        }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUI();
        panel.SetActive(false);
        player.Stats.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (player != null) player.Stats.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int level)
    {
        pendingLevelUps.Enqueue(level);
        if (!isOpen) ShowNext();
    }

    private void Update()
    {
        // เปิดผู้เล่นคืนเฟรมถัดไป กันคลิก/ปุ่ม 1-3 ที่ใช้เลือกการ์ดไปต่อยหรือกดสกิลด้วย
        if (enablePlayerNextFrame)
        {
            enablePlayerNextFrame = false;
            if (player != null && player.IsAlive) player.enabled = true;
        }
        if (!isOpen) return;

        Time.timeScale = 0f; // กันปุ่ม Pause ปลดหยุดเกมระหว่างเลือก

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) Pick(0);
        else if (keyboard.digit2Key.wasPressedThisFrame) Pick(1);
        else if (keyboard.digit3Key.wasPressedThisFrame) Pick(2);
    }

    private void ShowNext()
    {
        if (pendingLevelUps.Count == 0)
        {
            Close();
            return;
        }
        int level = pendingLevelUps.Dequeue();

        currentOptions = upgradeManager.GetOptions(player, optionCount);
        if (currentOptions.Count == 0)
        {
            ShowNext(); // ไม่มีอะไรให้เลือกแล้ว ข้ามไป
            return;
        }

        if (!isOpen)
        {
            isOpen = true;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            player.enabled = false; // หยุดรับปุ่มของผู้เล่นระหว่างเลือก
        }
        Time.timeScale = 0f;
        titleText.text = "LEVEL UP!  Lv." + level;

        for (int i = 0; i < cards.Count; i++)
        {
            bool has = i < currentOptions.Count;
            cards[i].root.SetActive(has);
            if (has) FillCard(cards[i], currentOptions[i]);
        }
        panel.SetActive(true);
    }

    private void FillCard(Card card, UpgradeData upgrade)
    {
        Sprite sprite = upgrade.DisplayIcon;
        card.icon.sprite = sprite;
        card.icon.enabled = sprite != null;
        card.iconLabel.text = sprite == null ? StatLabel(upgrade) : "";
        card.nameText.text = upgrade.DisplayName;

        if (upgrade.type == UpgradeType.Skill)
        {
            int level = player.GetSkillLevel(upgrade.skill.skillName);
            card.levelText.text = level == 0 ? "NEW!" : "Lv." + level + " > " + (level + 1);
            card.descText.text = !string.IsNullOrEmpty(upgrade.description) ? upgrade.description : upgrade.skill.description;
        }
        else
        {
            string amount = upgrade.isPercent ? "+" + Mathf.RoundToInt(upgrade.value * 100f) + "%" : "+" + upgrade.value;
            card.levelText.text = amount;
            card.descText.text = upgrade.description;
        }
    }

    private string StatLabel(UpgradeData upgrade)
    {
        switch (upgrade.statType)
        {
            case StatType.MaxHP: return "HP";
            case StatType.Damage: return "ATK";
            default: return "SPD";
        }
    }

    private void Pick(int index)
    {
        if (!isOpen || index >= currentOptions.Count) return;
        upgradeManager.ApplyUpgrade(currentOptions[index], player);
        ShowNext(); // ยังมีเลเวลค้างในคิว เลือกต่อ
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        Time.timeScale = previousTimeScale;
        enablePlayerNextFrame = true;
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    #region Build UI
    private void BuildUI()
    {
        GameObject canvasObj = new GameObject("UpgradeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50; // อยู่บนสุด ทับ HUD
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // พื้นมืดเต็มจอ
        panel = new GameObject("UpgradePanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObj.transform, false);
        Stretch(panel.GetComponent<RectTransform>());
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        titleText = MakeText(panel.transform, "Title", 64, Gold, new Vector2(0f, 360f), new Vector2(1200f, 100f));
        titleText.gameObject.AddComponent<Outline>().effectColor = WoodDark;
        Text hint = MakeText(panel.transform, "Hint", 26, Cream, new Vector2(0f, 290f), new Vector2(1200f, 50f));
        hint.text = "เลือกอัปเกรด 1 อย่าง (คลิก หรือกด 1 / 2 / 3)";

        for (int i = 0; i < optionCount; i++)
        {
            float x = (i - (optionCount - 1) / 2f) * 440f;
            cards.Add(BuildCard(panel.transform, i, new Vector2(x, -40f)));
        }
    }

    private Card BuildCard(Transform parent, int index, Vector2 pos)
    {
        Card card = new Card();
        // กรอบไม้เข้มขอบทอง + ไม้อ่อนด้านใน
        GameObject root = new GameObject("Card" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(380f, 520f);
        rt.anchoredPosition = pos;
        root.GetComponent<Image>().color = WoodDark;
        Outline outline = root.AddComponent<Outline>();
        outline.effectColor = Gold;
        outline.effectDistance = new Vector2(4f, -4f);

        Button button = root.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.55f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        button.colors = colors;
        int captured = index;
        button.onClick.AddListener(() => Pick(captured));

        GameObject inner = new GameObject("Inner", typeof(RectTransform), typeof(Image));
        inner.transform.SetParent(root.transform, false);
        RectTransform innerRt = inner.GetComponent<RectTransform>();
        Stretch(innerRt);
        innerRt.offsetMin = new Vector2(14f, 14f);
        innerRt.offsetMax = new Vector2(-14f, -14f);
        Image innerImg = inner.GetComponent<Image>();
        innerImg.color = WoodLight;
        innerImg.raycastTarget = false;

        // ไอคอนวงกลมแบบช่องสกิลใน HUD
        Image bg = MakeImage(root.transform, "IconBG", iconBackground, new Vector2(0f, 120f), new Vector2(170f, 170f));
        bg.color = new Color(0.15f, 0.1f, 0.06f);
        card.icon = MakeImage(root.transform, "Icon", null, new Vector2(0f, 120f), new Vector2(150f, 150f));
        card.iconLabel = MakeText(root.transform, "IconLabel", 48, Gold, new Vector2(0f, 120f), new Vector2(160f, 160f));
        MakeImage(root.transform, "Ring", iconRing, new Vector2(0f, 120f), new Vector2(196f, 196f));

        Text key = MakeText(root.transform, "Key", 30, Cream, new Vector2(-150f, 220f), new Vector2(50f, 50f));
        key.text = (index + 1).ToString();

        card.nameText = MakeText(root.transform, "Name", 38, Gold, new Vector2(0f, -10f), new Vector2(340f, 56f));
        card.nameText.gameObject.AddComponent<Outline>().effectColor = WoodDark;
        card.levelText = MakeText(root.transform, "Level", 28, Color.white, new Vector2(0f, -60f), new Vector2(340f, 40f));
        card.descText = MakeText(root.transform, "Desc", 24, Cream, new Vector2(0f, -160f), new Vector2(320f, 150f));
        card.descText.alignment = TextAnchor.UpperCenter;
        card.root = root;
        return card;
    }

    private Image MakeImage(Transform parent, string objName, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (sprite == null) img.enabled = false; // ไม่มีรูปก็ซ่อน (Icon จะเปิดเองตอนใส่รูป)
        return img;
    }

    private Text MakeText(Transform parent, string objName, int size, Color color, Vector2 pos, Vector2 boxSize)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.sizeDelta = boxSize;
        rt.anchoredPosition = pos;
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
    #endregion
}
