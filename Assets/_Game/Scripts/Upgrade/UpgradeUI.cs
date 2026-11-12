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
    [SerializeField] private Sprite bannerCloth;    // upgrade_banner_cloth (ผ้าสีขาว ย้อมสีตามสกิล)
    [SerializeField] private Sprite bannerTrim;     // upgrade_banner_trim (ขอบทอง ไม้แขวน)
    [SerializeField] private Sprite bannerGlow;     // upgrade_banner_glow (เงาเรืองแสงตอนชี้)

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
        public Image cloth;
        public Image glow;
        public UpgradeCardMotion motion;
        public Image icon;
        public Text iconLabel;
        public Text nameText;
        public Text levelText;
        public Text statText;
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
        // ฟอนต์ไทยของ Windows (Leelawadee UI) อ่านง่ายกว่า Arial ที่ต่อภาษาไทยแบบเบียด ๆ
        font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma" }, 32);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildUI();
        panel.SetActive(false);
        player.Stats.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (player != null) player.Stats.OnLevelUp -= HandleLevelUp;
    }

    // ฆ่าบอสแล้ว (Victory) หรือตาย (GameOver) = เกมจบ ไม่ต้องเลือกอัปเกรดแล้ว ไม่งั้นทับหน้า Victory
    private bool IsGameOver()
    {
        GameManager gm = GameManager.Instance;
        return gm != null && (gm.State == GameState.Victory || gm.State == GameState.GameOver);
    }

    private void HandleLevelUp(int level)
    {
        if (IsGameOver()) return;
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
        if (IsGameOver())
        {
            pendingLevelUps.Clear();
            Close();
            return;
        }

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
        for (int i = 0; i < cards.Count; i++)
        {
            if (cards[i].root.activeSelf) cards[i].motion.PlayIntro(i * 0.08f); // ธงเลื่อนลงมาทีละผืน
        }
    }

    private void FillCard(Card card, UpgradeData upgrade)
    {
        Sprite sprite = upgrade.DisplayIcon;
        card.icon.sprite = sprite;
        card.icon.enabled = sprite != null;
        card.iconLabel.text = sprite == null ? StatLabel(upgrade) : "";
        card.nameText.text = upgrade.DisplayName;
        card.cloth.color = upgrade.cardColor;
        card.glow.color = Color.Lerp(upgrade.cardColor, Color.white, 0.5f);

        if (upgrade.type == UpgradeType.Skill)
        {
            int level = player.GetSkillLevel(upgrade.skill.skillName);
            card.levelText.text = level == 0 ? "NEW!" : Stars(level, upgrade.skill.maxLevel);
            card.descText.text = !string.IsNullOrEmpty(upgrade.description) ? upgrade.description : upgrade.skill.description;
            card.statText.text = SkillStatLine(upgrade.skill, level);
        }
        else
        {
            string amount = upgrade.isPercent ? "+" + Mathf.RoundToInt(upgrade.value * 100f) + "%" : "+" + upgrade.value;
            card.levelText.text = amount;
            card.descText.text = upgrade.description;
            card.statText.text = "";
        }
    }

    // ★ = เลเวลที่มีแล้ว, ☆ ตัวแรก = เลเวลที่จะได้ (เช่น Lv.2 -> 3 = ★★☆)
    private string Stars(int level, int maxLevel)
    {
        string text = "Lv." + level + " > " + (level + 1) + System.Environment.NewLine;
        for (int i = 0; i < maxLevel; i++)
        {
            text += i < level ? "★" : (i == level ? "<color=#FFFFFF>☆</color>" : "<color=#00000055>☆</color>");
        }
        return text;
    }

    // บอกว่าเลือกแล้วแรงขึ้นเท่าไร: ค่าตามเลเวลแบบ Skill.Value() = baseValue + valuePerLevel * (level - 1)
    private string SkillStatLine(SkillData skill, int level)
    {
        string label = skill.kind == SkillKind.Passive ? "โบนัส" : "ดาเมจ";
        string unit = skill.kind == SkillKind.Passive ? "%" : "";
        if (level == 0) return label + " " + skill.baseValue + unit;
        float now = skill.baseValue + skill.valuePerLevel * (level - 1);
        float next = now + skill.valuePerLevel;
        int percent = now > 0f ? Mathf.RoundToInt(skill.valuePerLevel / now * 100f) : 0;
        return label + " " + now + unit + " → " + next + unit + System.Environment.NewLine + "<color=#7CFF6B>+" + percent + "%</color>";
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
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f); // พื้นหลังมืด ให้การ์ดเด่น

        titleText = MakeText(panel.transform, "Title", 64, Gold, new Vector2(0f, 420f), new Vector2(1200f, 100f));
        titleText.gameObject.AddComponent<Outline>().effectColor = WoodDark;
        Text hint = MakeText(panel.transform, "Hint", 26, Cream, new Vector2(0f, 355f), new Vector2(1200f, 50f));
        hint.text = "เลือกอัปเกรด 1 อย่าง (คลิก หรือกด 1 / 2 / 3)";

        for (int i = 0; i < optionCount; i++)
        {
            float x = (i - (optionCount - 1) / 2f) * 440f;
            cards.Add(BuildCard(panel.transform, i, new Vector2(x, -90f)));
        }
    }

    // การ์ด = ธงแขวน: เงาเรือง (ชี้แล้วโผล่) -> ผ้าย้อมสี -> ขอบทอง -> ไอคอน -> ตัวหนังสือ
    private Card BuildCard(Transform parent, int index, Vector2 pos)
    {
        Card card = new Card();
        Vector2 size = new Vector2(344f, 640f); // สัดส่วนเดียวกับรูปธง (519 x 967)
        GameObject root = new GameObject("Card" + (index + 1), typeof(RectTransform), typeof(Image), typeof(Button));
        root.transform.SetParent(parent, false);
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image hit = root.GetComponent<Image>();
        hit.sprite = bannerGlow;
        hit.color = new Color(1f, 1f, 1f, 0f); // มองไม่เห็น แต่ใช้รับคลิกตามรูปธง


        Button button = root.GetComponent<Button>();
        button.transition = Selectable.Transition.None; // เด้ง/เรืองแสงทำเองใน UpgradeCardMotion
        int captured = index;
        button.onClick.AddListener(() => Pick(captured));

        card.glow = MakeImage(root.transform, "Glow", bannerGlow, Vector2.zero, size * 1.07f);
        card.cloth = MakeImage(root.transform, "Cloth", bannerCloth, Vector2.zero, size);
        MakeImage(root.transform, "Trim", bannerTrim, Vector2.zero, size);

        // ไอคอนวงกลมทับกรอบข้าวหลามตัดบนไม้แขวน
        Vector2 iconPos = new Vector2(0f, 262f);
        Image bg = MakeImage(root.transform, "IconBG", iconBackground, iconPos, new Vector2(150f, 150f));
        bg.color = new Color(0.15f, 0.1f, 0.06f);
        card.icon = MakeImage(root.transform, "Icon", null, iconPos, new Vector2(132f, 132f));
        card.iconLabel = MakeText(root.transform, "IconLabel", 44, Gold, iconPos, new Vector2(140f, 140f));
        MakeImage(root.transform, "Ring", iconRing, iconPos, new Vector2(176f, 176f));

        Text key = MakeText(root.transform, "Key", 28, Cream, new Vector2(-120f, 200f), new Vector2(50f, 50f));
        key.text = (index + 1).ToString();
        key.gameObject.AddComponent<Outline>().effectColor = WoodDark;

        card.nameText = MakeText(root.transform, "Name", 40, Color.white, new Vector2(0f, 135f), new Vector2(270f, 56f));
        card.nameText.resizeTextForBestFit = true; // ชื่อยาว (FinalExecution) ย่อเองไม่ล้นธง
        card.nameText.resizeTextMinSize = 22;
        card.nameText.resizeTextMaxSize = 40;
        card.nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        AddShadow(card.nameText);
        card.descText = MakeText(root.transform, "Desc", 23, Color.white, new Vector2(0f, 20f), new Vector2(260f, 150f));
        card.descText.fontStyle = FontStyle.Normal;
        card.descText.lineSpacing = 1.15f;
        card.statText = MakeText(root.transform, "Stat", 26, Color.white, new Vector2(0f, -80f), new Vector2(280f, 80f));
        card.statText.supportRichText = true;
        AddShadow(card.statText);
        card.descText.alignment = TextAnchor.UpperCenter;
        AddShadow(card.descText);
        card.levelText = MakeText(root.transform, "Level", 30, Gold, new Vector2(0f, -185f), new Vector2(300f, 80f));
        card.levelText.supportRichText = true;
        AddShadow(card.levelText);

        card.motion = root.AddComponent<UpgradeCardMotion>();
        card.motion.SetUP(card.glow);
        card.root = root;
        return card;
    }

    private static void AddShadow(Text text)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.07f, 0.02f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
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
