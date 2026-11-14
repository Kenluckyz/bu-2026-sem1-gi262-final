using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// UI ของดันเจี้ยน (ไม่รวมแถบเลือด/สกิลของ Boat ซึ่งเป็น HUD + SkillBarUI เดิม)
// - มุมขวาบน: ห้อง / เวฟ / ศัตรูที่เหลือ
// - กลางจอ: ป้ายประกาศ (เข้าห้อง, เวฟใหม่, ผ่านห้อง)
// - บนกลาง: แถบเลือดบอส
// - หน้าจบเกม (ชนะ / แพ้) และหน้าหยุดเกม
// สร้าง UI ในโค้ดตอนเริ่มเกม (แบบเดียวกับ EnemySpawnPanel)
public class GameplayHUD : MonoBehaviour
{
    private static readonly Color Ivory = new Color(0.93f, 0.89f, 0.78f);
    private static readonly Color Gold = new Color(0.9f, 0.62f, 0.28f);
    private static readonly Color Muted = new Color(0.62f, 0.72f, 0.72f);
    private static readonly Color CardColor = new Color(0.03f, 0.05f, 0.07f, 0.85f);
    private static readonly Color BossRed = new Color(0.85f, 0.16f, 0.1f);

    private GameManager game;
    private DungeonManager dungeon;
    private EnemyManager enemyManager;
    private Font font;

    // มุมขวาบน
    private Text roomText;
    private Text waveText;
    private Text countText;

    // ป้ายกลางจอ
    private Text bannerTitle;
    private Text bannerSub;
    private float bannerTimer;
    private const float BannerTime = 2.6f;

    // แถบเลือดบอส
    private GameObject bossBar;
    private Text bossName;
    private RectTransform bossFill;
    private RectTransform bossTrail; // แถบขาวที่ลดตามช้า ๆ ให้เห็นว่าโดนไปเท่าไร
    private Enemy boss;
    private float bossRatio = 1f;
    private float trailRatio = 1f;
    private const float BossBarWidth = 640f;

    // หน้าจบ / หยุด
    private GameObject endPanel;
    private Text endTitle;
    private Text endStats;
    private GameObject pausePanel;

    public void SetUP(GameManager gameManager, DungeonManager dungeonManager, EnemyManager enemies)
    {
        game = gameManager;
        dungeon = dungeonManager;
        enemyManager = enemies;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
        Build();

        dungeon.OnAnnounce += ShowBanner;
        dungeon.OnBossSpawned += ShowBoss;
    }

    private void OnDestroy()
    {
        if (dungeon != null)
        {
            dungeon.OnAnnounce -= ShowBanner;
            dungeon.OnBossSpawned -= ShowBoss;
        }
        if (boss != null) boss.OnHPChanged -= UpdateBoss;
    }

    private void Update()
    {
        if (dungeon == null) return;
        UpdateRoomCard();

        // ป้ายกลางจอ: ค้างไว้แล้วค่อย ๆ จางหาย (ใช้เวลาจริง ไม่ช้าตามสโลว์โมชัน)
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(bannerTimer / 0.6f);
            SetAlpha(bannerTitle, alpha);
            SetAlpha(bannerSub, alpha);
        }

        // แถบขาวลดตามแถบแดงช้า ๆ
        if (bossBar != null && bossBar.activeSelf)
        {
            trailRatio = Mathf.MoveTowards(trailRatio, bossRatio, Time.unscaledDeltaTime * 0.35f);
            if (trailRatio < bossRatio) trailRatio = bossRatio;
            bossTrail.sizeDelta = new Vector2(BossBarWidth * trailRatio, bossTrail.sizeDelta.y);
        }
    }

    private void UpdateRoomCard()
    {
        Room room = dungeon.CurrentRoom;
        if (room == null)
        {
            roomText.text = "ENTERING THE DUNGEON";
            waveText.text = "";
            countText.text = "";
            return;
        }
        roomText.text = "ROOM " + (dungeon.CurrentRoomIndex + 1) + " / " + dungeon.RoomCount + "   " + room.roomName.ToUpper();
        if (room.IsCleared) waveText.text = "CLEARED";
        else if (room.isBossRoom) waveText.text = "BOSS FIGHT";
        else waveText.text = "WAVE  " + Mathf.Max(room.CurrentWaveNumber, 1) + " / " + room.WaveCount;
        int alive = enemyManager != null ? enemyManager.GetAliveCount() : 0;
        if (room.IsCleared) countText.text = room.isBossRoom ? "Dungeon cleared!" : "Go to the next chamber";
        else countText.text = alive + (alive == 1 ? " ENEMY LEFT" : " ENEMIES LEFT");
    }

    #region Public
    public void ShowBanner(string title, string subtitle)
    {
        bannerTitle.text = title.ToUpper();
        bannerSub.text = subtitle;
        bannerTimer = BannerTime;
        SetAlpha(bannerTitle, 1f);
        SetAlpha(bannerSub, 1f);
    }

    public void ShowBoss(Enemy enemy)
    {
        boss = enemy;
        bossBar.SetActive(true);
        bossName.text = enemy.characterName.ToUpper() + "   LV." + enemy.Level; // บอสเก่งขึ้นตามเลเวลผู้เล่น
        bossRatio = 1f;
        trailRatio = 1f;
        boss.OnHPChanged += UpdateBoss;
        UpdateBoss(boss.CurrentHP, boss.MaxHP);
    }

    public void HideBoss()
    {
        if (bossBar != null) bossBar.SetActive(false);
    }

    public void ShowEnd(bool victory, float playTime, int kills)
    {
        endPanel.SetActive(true);
        endTitle.text = victory ? "VICTORY" : "YOU HAVE FALLEN";
        endTitle.color = victory ? Gold : new Color(0.9f, 0.3f, 0.25f);
        int minutes = Mathf.FloorToInt(playTime / 60f);
        int seconds = Mathf.FloorToInt(playTime % 60f);
        string first = victory ? "The Orc Warlord is slain. The dungeon is cleared!" : "The dungeon claims another soul...";
        endStats.text = first + "\n\nTime  " + minutes.ToString("00") + ":" + seconds.ToString("00") + "      Enemies defeated  " + kills;
    }

    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
    }
    #endregion

    private void UpdateBoss(float current, float max)
    {
        bossRatio = max > 0f ? current / max : 0f;
        bossFill.sizeDelta = new Vector2(BossBarWidth * bossRatio, bossFill.sizeDelta.y);
    }

    #region Build UI
    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    private void Build()
    {
        // 1. Canvas (ทับ HUD ของ Boat)
        GameObject canvasObj = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.transform.SetParent(transform, false);
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform root = canvasObj.transform;

        // 2. การ์ดห้อง มุมขวาบน
        RectTransform card = Panel(root, "RoomCard", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(430f, 118f), CardColor);
        card.pivot = new Vector2(1f, 1f);
        Panel(card, "GoldRule", new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(4f, 118f), Gold).pivot = new Vector2(0f, 0.5f);
        roomText = Label(card, "Room", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(400f, 26f), "", 17, Gold, TextAnchor.MiddleCenter);
        waveText = Label(card, "Wave", new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(400f, 40f), "", 32, Ivory, TextAnchor.MiddleCenter);
        countText = Label(card, "Count", new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(400f, 24f), "", 17, Muted, TextAnchor.MiddleCenter);

        // 3. ป้ายกลางจอ
        bannerTitle = Label(root, "BannerTitle", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1400f, 70f), "", 54, Ivory, TextAnchor.MiddleCenter);
        bannerSub = Label(root, "BannerSub", new Vector2(0.5f, 0.72f), new Vector2(0f, -52f), new Vector2(1400f, 34f), "", 24, Gold, TextAnchor.MiddleCenter);
        AddShadow(bannerTitle);
        AddShadow(bannerSub);

        // 4. แถบเลือดบอส บนกลาง
        RectTransform bar = Panel(root, "BossBar", new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(BossBarWidth + 16f, 62f), CardColor);
        bossBar = bar.gameObject;
        bossName = Label(bar, "BossName", new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(BossBarWidth, 24f), "", 19, Gold, TextAnchor.MiddleCenter);
        Panel(bar, "Track", new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(BossBarWidth, 16f), new Color(0.15f, 0.08f, 0.08f));
        bossTrail = Panel(bar, "Trail", new Vector2(0.5f, 0f), new Vector2(-BossBarWidth / 2f, 18f), new Vector2(BossBarWidth, 16f), new Color(1f, 0.9f, 0.75f));
        bossTrail.pivot = new Vector2(0f, 0.5f);
        bossFill = Panel(bar, "Fill", new Vector2(0.5f, 0f), new Vector2(-BossBarWidth / 2f, 18f), new Vector2(BossBarWidth, 16f), BossRed);
        bossFill.pivot = new Vector2(0f, 0.5f);
        bossBar.SetActive(false);

        // 5. ปุ่มที่ใช้ได้ มุมซ้ายล่าง
        Label(root, "Controls", new Vector2(0f, 0f), new Vector2(200f, 40f), new Vector2(360f, 50f),
            "WASD  Move     LMB  Attack\n1 2 3  Skills     Esc  Pause", 16, Muted, TextAnchor.MiddleLeft);

        // 6. หน้าจบเกม (เต็มจอ มืด ๆ)
        RectTransform end = Panel(root, "EndPanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 3000f), new Color(0f, 0f, 0f, 0.72f));
        end.GetComponent<Image>().raycastTarget = true;
        endPanel = end.gameObject;
        endTitle = Label(end, "Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 100f), "", 86, Gold, TextAnchor.MiddleCenter);
        AddShadow(endTitle);
        endStats = Label(end, "Stats", new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(1200f, 110f), "", 26, Ivory, TextAnchor.MiddleCenter);
        Button again = CreateButton(end, "PlayAgain", "PLAY AGAIN  [R]", new Vector2(0f, -120f));
        again.onClick.AddListener(game.Restart);
        endPanel.SetActive(false);

        // 7. หน้าหยุดเกม
        RectTransform pause = Panel(root, "PausePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 3000f), new Color(0f, 0f, 0f, 0.6f));
        pause.GetComponent<Image>().raycastTarget = true;
        pausePanel = pause.gameObject;
        Label(pause, "Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(800f, 90f), "PAUSED", 72, Ivory, TextAnchor.MiddleCenter);
        Button resume = CreateButton(pause, "Resume", "RESUME  [Esc]", new Vector2(0f, -20f));
        resume.onClick.AddListener(game.TogglePause);
        Button restart = CreateButton(pause, "Restart", "RESTART", new Vector2(0f, -100f));
        restart.onClick.AddListener(game.Restart);
        pausePanel.SetActive(false);
    }

    private RectTransform Panel(Transform parent, string objName, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = Rect(parent, objName, anchor, pos, size);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private Text Label(Transform parent, string objName, Vector2 anchor, Vector2 pos, Vector2 size, string value, int fontSize, Color color, TextAnchor align)
    {
        RectTransform rt = Rect(parent, objName, anchor, pos, size);
        Text text = rt.gameObject.AddComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateButton(Transform parent, string objName, string label, Vector2 pos)
    {
        RectTransform rt = Panel(parent, objName, new Vector2(0.5f, 0.5f), pos, new Vector2(360f, 62f), new Color(0.16f, 0.12f, 0.08f, 0.95f));
        Image image = rt.GetComponent<Image>();
        image.raycastTarget = true;
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        button.colors = colors;
        Label(rt, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 62f), label, 24, Ivory, TextAnchor.MiddleCenter);
        return button;
    }

    private RectTransform Rect(Transform parent, string objName, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private void AddShadow(Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(2f, -3f);
    }

    private void SetAlpha(Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
    #endregion
}
