using UnityEngine;
using UnityEngine.UI;

// UI ของดันเจี้ยน (ไม่รวมแถบเลือด/สกิลของ Boat ซึ่งเป็น HUD + SkillBarUI เดิม)
// - มุมขวาบน: ห้อง / เวฟ / ศัตรูที่เหลือ
// - กลางจอ: ป้ายประกาศ (เข้าห้อง, เวฟใหม่, ผ่านห้อง)
// - บนกลาง: แถบเลือดบอส
// - หน้าจบเกม (ชนะ / แพ้) และหน้าหยุดเกม
// ตัว UI อยู่ในซีนแล้ว (GameplayCanvas ใต้ object นี้) ย้าย/เปลี่ยนสี/ขนาดใน Hierarchy ได้เลย
// สคริปต์นี้แค่เปลี่ยนข้อความ เปิด/ปิด และย่อแถบเลือดบอส
public class GameplayHUD : MonoBehaviour
{
    [Header("มุมขวาบน")]
    [SerializeField] private Text roomText;
    [SerializeField] private Text waveText;
    [SerializeField] private Text countText;

    [Header("ป้ายกลางจอ")]
    [SerializeField] private Text bannerTitle;
    [SerializeField] private Text bannerSub;
    [SerializeField] private float bannerTime = 2.6f; // ค้างกี่วิก่อนจาง

    [Header("แถบเลือดบอส")]
    [SerializeField] private GameObject bossBar;
    [SerializeField] private Text bossName;
    [SerializeField] private RectTransform bossFill;
    [SerializeField] private RectTransform bossTrail; // แถบขาวที่ลดตามช้า ๆ ให้เห็นว่าโดนไปเท่าไร

    [Header("หน้าจบ / หยุด")]
    [SerializeField] private GameObject endPanel;
    [SerializeField] private Text endTitle;
    [SerializeField] private Text endStats;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Color victoryColor = new Color(0.9f, 0.62f, 0.28f);
    [SerializeField] private Color defeatColor = new Color(0.9f, 0.3f, 0.25f);

    private DungeonManager dungeon;
    private EnemyManager enemyManager;
    private float bannerTimer;

    private Enemy boss;
    private float bossBarWidth; // ความกว้างเต็มของแถบบอส (อ่านจากซีน)
    private float bossRatio = 1f;
    private float trailRatio = 1f;

    public void SetUP(DungeonManager dungeonManager, EnemyManager enemies)
    {
        dungeon = dungeonManager;
        enemyManager = enemies;

        // 1. เช็คว่าลาก UI ใส่ครบไหม (ไม่ครบ = ยังไม่ได้กดเมนู Bake Gameplay UI)
        if (roomText == null || bannerTitle == null || bossBar == null || endPanel == null || pausePanel == null)
        {
            Debug.LogError("GameplayHUD: ยังไม่มี UI ในซีน กดเมนู BoatVsBoss > Bake Gameplay UI ก่อน");
            enabled = false;
            return;
        }

        // 2. ค่าเริ่มต้น: ซ่อนแถบบอส / หน้าจบ / หน้าหยุด และป้ายกลางจอ
        bossBarWidth = bossFill.sizeDelta.x;
        bossBar.SetActive(false);
        endPanel.SetActive(false);
        pausePanel.SetActive(false);
        SetAlpha(bannerTitle, 0f);
        SetAlpha(bannerSub, 0f);

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
        if (bossBar.activeSelf)
        {
            trailRatio = Mathf.MoveTowards(trailRatio, bossRatio, Time.unscaledDeltaTime * 0.35f);
            if (trailRatio < bossRatio) trailRatio = bossRatio;
            bossTrail.sizeDelta = new Vector2(bossBarWidth * trailRatio, bossTrail.sizeDelta.y);
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
        roomText.text = "ROOM " + (dungeon.CurrentRoomIndex + 1);
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
        bannerTimer = bannerTime;
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
        endTitle.color = victory ? victoryColor : defeatColor;
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
        bossFill.sizeDelta = new Vector2(bossBarWidth * bossRatio, bossFill.sizeDelta.y);
    }

    private void SetAlpha(Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }
}
