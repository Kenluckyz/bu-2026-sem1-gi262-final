using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// หน้าเลือกการ์ด 3 ใบ ตอนเลเวลอัป หยุดเกมไว้
// ตัว UI อยู่ในซีนแล้ว (UpgradeSystem > UpgradeCanvas) ปรับตำแหน่ง/สี/ขนาดใน Hierarchy ได้เลย
// สคริปต์นี้แค่ใส่ข้อมูลการ์ด เปิด/ปิด และรับการเลือก
public class UpgradeUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private UpgradeManager upgradeManager;

    [Header("UI ในซีน")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Text titleText;
    [SerializeField] private List<Card> cards = new List<Card>(); // จำนวนการ์ด = จำนวนตัวเลือก

    // คิวเลเวลอัป อัปหลายเลเวลก็ได้เลือกครบ (Queue)
    private Queue<int> pendingLevelUps = new Queue<int>();
    private List<UpgradeData> currentOptions = new List<UpgradeData>();
    private bool isOpen;
    private bool enablePlayerNextFrame;
    private float previousTimeScale = 1f;

    // ของในการ์ด 1 ใบ (โชว์ใน Inspector ให้เห็นว่าลากอะไรใส่)
    [System.Serializable]
    public class Card
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
        if (player == null || upgradeManager == null || panel == null)
        {
            Debug.LogWarning("UpgradeUI: ไม่พบ Player / UpgradeManager / UI (กดเมนู BoatVsBoss > Bake Gameplay UI)");
            return;
        }

        // 1. ฟอนต์ไทย Leelawadee อ่านง่ายกว่า Arial (ฟอนต์ของ Windows ใส่ในซีนไม่ได้ เลยเปลี่ยนตอนเริ่มเกม)
        Font font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma" }, 32);
        if (font != null)
        {
            Text[] texts = panel.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < texts.Length; i++) texts[i].font = font;
        }

        // 2. ให้การ์ดรู้ตำแหน่งเดิมกับเงาเรือง (ใช้ตอนเด้ง)
        for (int i = 0; i < cards.Count; i++) cards[i].motion.SetUP(cards[i].glow);

        panel.SetActive(false);
        player.Stats.OnLevelUp += HandleLevelUp;
    }

    private void OnDestroy()
    {
        if (player != null) player.Stats.OnLevelUp -= HandleLevelUp;
    }

    // เกมจบแล้วไม่ต้องเปิด ไม่งั้นทับหน้า Victory
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
        // เปิด player เฟรมหน้า ไม่งั้นคลิกเลือกการ์ดไปต่อยด้วย
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

        Time.timeScale = 0f; // กันปุ่ม pause

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

        currentOptions = upgradeManager.GetOptions(player, cards.Count);
        if (currentOptions.Count == 0)
        {
            ShowNext(); // ไม่มีให้เลือกก็ข้าม
            return;
        }

        if (!isOpen)
        {
            isOpen = true;
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            player.enabled = false; // ปิดปุ่ม player ตอนเลือก
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
            if (cards[i].root.activeSelf) cards[i].motion.PlayIntro(i * 0.08f); // ธงลงทีละผืน
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

    // ดาวเต็ม = เลเวลที่มี ดาวว่างแรก = เลเวลที่จะได้
    private string Stars(int level, int maxLevel)
    {
        string text = "Lv." + level + " > " + (level + 1) + System.Environment.NewLine;
        for (int i = 0; i < maxLevel; i++)
        {
            text += i < level ? "★" : (i == level ? "<color=#FFFFFF>☆</color>" : "<color=#00000055>☆</color>");
        }
        return text;
    }

    // บอกว่าเลือกแล้วแรงขึ้นเท่าไร คิดแบบ Skill.Value()
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

    // ปุ่มการ์ดเรียกตรงนี้ (ตั้งไว้ใน Button > On Click ของการ์ดแต่ละใบ)
    public void Pick(int index)
    {
        if (!isOpen || index >= currentOptions.Count) return;
        upgradeManager.ApplyUpgrade(currentOptions[index], player);
        ShowNext(); // ยังมีในคิว เลือกต่อ
    }

    private void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        panel.SetActive(false);
        Time.timeScale = previousTimeScale;
        enablePlayerNextFrame = true;
    }
}
