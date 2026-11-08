using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// แผงปุ่มเกิดศัตรูมุมขวาบน — ใช้ทดสอบในซีน Test_Enemy เท่านั้น
// กดรูปศัตรูตัวไหน = เกิดตัวนั้น 1 ตัว / Kill All = ฆ่าทุกตัว / Auto Respawn = ตายหมดแล้วเกิดชุดใหม่เอง
// God Mode = เติมเลือด Boat เต็มตลอด (ดูท่าศัตรูได้นาน ๆ ไม่ตาย)
// สร้าง UI ในโค้ดตอนเริ่มเกม (แบบเดียวกับ SceneBuilder.BuildHUD) — เมนู BoatVsBoss > Build Enemy Spawn Panel ใส่รูปไอคอนให้
public class EnemySpawnPanel : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public EnemyData data;
        public Sprite icon;
    }

    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyTestSpawner testSpawner;
    [SerializeField] private Entry[] entries;
    [SerializeField] private int columns = 3;
    [SerializeField] private float buttonSize = 96f;

    private static readonly Color PanelColor = new Color(0.08f, 0.08f, 0.1f, 0.85f);
    private static readonly Color ButtonColor = new Color(0.2f, 0.2f, 0.24f, 1f);
    private static readonly Color KillColor = new Color(0.7f, 0.15f, 0.12f, 1f);
    private static readonly Color OnColor = new Color(0.2f, 0.55f, 0.25f, 1f);

    private Font font;
    private Text countText;
    private Text respawnText;
    private Image respawnImage;
    private Text godText;
    private Image godImage;
    private bool godMode;

    private void Start()
    {
        if (enemyManager == null) enemyManager = FindAnyObjectByType<EnemyManager>();
        if (testSpawner == null) testSpawner = FindAnyObjectByType<EnemyTestSpawner>();
        if (enemyManager == null || entries == null)
        {
            Debug.LogWarning("EnemySpawnPanel: ไม่พบ EnemyManager หรือยังไม่ได้ใส่รายการศัตรู");
            return;
        }
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        BuildPanel();
    }

    private void Update()
    {
        if (countText != null && enemyManager != null)
        {
            countText.text = "Alive : " + enemyManager.GetAliveCount();
        }
        // God Mode: เติมเลือดเต็มทุกเฟรม (ใช้ Heal ของ Character ไม่ต้องแก้โค้ด Player)
        if (godMode && enemyManager != null && enemyManager.Player != null && enemyManager.Player.IsAlive)
        {
            enemyManager.Player.Heal(enemyManager.Player.MaxHP);
        }
    }

    #region Buttons
    private void Spawn(EnemyData data)
    {
        if (enemyManager == null || data == null) return;
        enemyManager.SpawnEnemy(data);
        Debug.Log("[Spawn Panel] เกิด " + data.enemyName);
    }

    private void KillAll()
    {
        if (testSpawner != null) testSpawner.KillAll();
    }

    private void ToggleRespawn()
    {
        if (testSpawner == null) return;
        testSpawner.RespawnWhenCleared = !testSpawner.RespawnWhenCleared;
        RefreshRespawnButton();
    }

    private void ToggleGodMode()
    {
        godMode = !godMode;
        godText.text = godMode ? "God Mode (Boat) : ON" : "God Mode (Boat) : OFF";
        godImage.color = godMode ? OnColor : ButtonColor;
    }

    private void RefreshRespawnButton()
    {
        bool on = testSpawner != null && testSpawner.RespawnWhenCleared;
        respawnText.text = on ? "Auto Respawn : ON" : "Auto Respawn : OFF";
        respawnImage.color = on ? OnColor : ButtonColor;
    }
    #endregion

    #region Build UI
    // ปุ่ม UI ต้องมี EventSystem ในซีน (โปรเจกต์ใช้ Input System ตัวใหม่ -> InputSystemUIInputModule)
    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        es.transform.SetParent(transform, false);
    }

    private void BuildPanel()
    {
        // 1. Canvas ทับบน HUD
        GameObject canvasObj = new GameObject("SpawnCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // 2. ขนาดแผง: ไอคอน columns ช่องต่อแถว + แถวปุ่มด้านล่าง
        float gap = 8f;
        float labelHeight = 20f;
        float cellHeight = buttonSize + labelHeight;
        int rows = Mathf.CeilToInt(entries.Length / (float)columns);
        float width = columns * buttonSize + (columns + 1) * gap;
        float height = 34f + rows * (cellHeight + gap) + 4 * (36f + gap) + gap;

        RectTransform panel = CreateRect("SpawnPanel", canvasObj.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(1f, 1f); // มุมขวาบน
        panel.anchoredPosition = new Vector2(-16f, -16f);
        panel.sizeDelta = new Vector2(width, height);
        panel.gameObject.AddComponent<Image>().color = PanelColor;

        Text title = CreateText(panel, "Title", "SPAWN ENEMY (Test)", 18, TextAnchor.MiddleCenter);
        SetRect(title.rectTransform, 0f, -4f, width, 28f);

        // 3. ปุ่มไอคอนศัตรู (วนลูปวางเป็นตาราง)
        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || entry.data == null) continue;
            int col = i % columns;
            int row = i / columns;
            float x = gap + col * (buttonSize + gap);
            float y = -34f - row * (cellHeight + gap);
            CreateEnemyButton(panel, entry, x, y, labelHeight);
        }

        // 4. แถวปุ่มด้านล่าง
        float bottomY = -34f - rows * (cellHeight + gap);
        Button kill = CreateWideButton(panel, "KillAll", "Kill All", KillColor, gap, bottomY, width - gap * 2, out _);
        kill.onClick.AddListener(KillAll);

        Button respawn = CreateWideButton(panel, "Respawn", "", ButtonColor, gap, bottomY - 44f, width - gap * 2, out respawnText);
        respawnImage = respawn.GetComponent<Image>();
        respawn.onClick.AddListener(ToggleRespawn);
        RefreshRespawnButton();

        Button god = CreateWideButton(panel, "GodMode", "", ButtonColor, gap, bottomY - 88f, width - gap * 2, out godText);
        godImage = god.GetComponent<Image>();
        god.onClick.AddListener(ToggleGodMode);
        godMode = true; // เริ่มแบบเปิด -> กดครั้งเดียวก็ปิด
        ToggleGodMode();

        countText = CreateText(panel, "Count", "Alive : 0", 16, TextAnchor.MiddleCenter);
        SetRect(countText.rectTransform, 0f, bottomY - 132f, width, 36f);
    }

    private void CreateEnemyButton(RectTransform panel, Entry entry, float x, float y, float labelHeight)
    {
        RectTransform rt = CreateRect(entry.data.enemyName, panel);
        SetRect(rt, x, y, buttonSize, buttonSize + labelHeight);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.color = ButtonColor;
        Button button = rt.gameObject.AddComponent<Button>();
        EnemyData data = entry.data; // เก็บไว้ในตัวแปรของรอบนี้ ให้ปุ่มแต่ละอันจำศัตรูของตัวเอง
        button.onClick.AddListener(() => Spawn(data));

        // รูปศัตรู
        RectTransform iconRt = CreateRect("Icon", rt);
        SetRect(iconRt, 4f, -4f, buttonSize - 8f, buttonSize - 8f);
        Image icon = iconRt.gameObject.AddComponent<Image>();
        icon.sprite = entry.icon;
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        if (entry.icon == null) icon.color = new Color(1f, 1f, 1f, 0.1f);

        // ชื่อใต้รูป
        Text label = CreateText(rt, "Name", entry.data.enemyName, 13, TextAnchor.MiddleCenter);
        SetRect(label.rectTransform, 0f, -(buttonSize - 2f), buttonSize, labelHeight);
    }

    private Button CreateWideButton(RectTransform panel, string objName, string label, Color color,
        float x, float y, float width, out Text text)
    {
        RectTransform rt = CreateRect(objName, panel);
        SetRect(rt, x, y, width, 36f);
        Image bg = rt.gameObject.AddComponent<Image>();
        bg.color = color;
        Button button = rt.gameObject.AddComponent<Button>();
        text = CreateText(rt, "Label", label, 16, TextAnchor.MiddleCenter);
        SetRect(text.rectTransform, 0f, 0f, width, 36f);
        return button;
    }

    private RectTransform CreateRect(string objName, Transform parent)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    // วางจากมุมซ้ายบนของ parent (x ไปขวา, y ลงล่างเป็นลบ)
    private void SetRect(RectTransform rt, float x, float y, float width, float height)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(width, height);
    }

    private Text CreateText(Transform parent, string objName, string value, int size, TextAnchor anchor)
    {
        RectTransform rt = CreateRect(objName, parent);
        Text text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = Color.white;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }
    #endregion
}
