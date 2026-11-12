using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// คุมภาพรวมของเกม (GameManager ตาม Diagram)
// - Boat ตาย -> Game Over / บอสตาย -> Victory -> กด R หรือปุ่ม เล่นใหม่
// - Esc = หยุดเกม
// - นับเวลาที่เล่นกับจำนวนศัตรูที่ฆ่า ไว้โชว์ตอนจบ
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState gameState = GameState.Playing;
    [SerializeField] private Player player;
    [SerializeField] private DungeonManager dungeon;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private GameplayHUD hud;
    [SerializeField] private int currentLevel = 1;

    [Header("Game Feel")]
    [Tooltip("บอสตาย: สโลว์โมชันช้าแค่ไหน (0.25 = ช้าลง 4 เท่า)")]
    [SerializeField] private float bossKillSlowMotion = 0.25f;
    [SerializeField] private float bossKillSlowTime = 1.5f;   // วินาทีจริง
    [SerializeField] private float gameOverDelay = 2f;        // รอท่าตายเล่นจบก่อนขึ้นหน้าจบ

    private int killCount;
    private float startTime;

    public GameState State => gameState;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; // กันค้างสโลว์/หยุด จากรอบก่อน
    }

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        if (dungeon == null) dungeon = FindAnyObjectByType<DungeonManager>();
        if (enemyManager == null) enemyManager = FindAnyObjectByType<EnemyManager>();
        if (hud == null) hud = FindAnyObjectByType<GameplayHUD>();
        if (player == null || dungeon == null || enemyManager == null || hud == null)
        {
            Debug.LogError("GameManager: ต้องมี Player, DungeonManager, EnemyManager และ GameplayHUD ในซีน");
            enabled = false;
            return;
        }

        hud.SetUP(this, dungeon, enemyManager);
        player.OnDied += HandlePlayerDied;
        dungeon.OnDungeonCleared += HandleDungeonCleared;
        dungeon.OnBossSpawned += HandleBossSpawned;
        enemyManager.OnEnemyDied += HandleEnemyDied;

        gameState = GameState.Playing;
        startTime = Time.time;
        Debug.Log("[GameManager] เริ่มเกม เลเวล " + currentLevel);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (player != null) player.OnDied -= HandlePlayerDied;
        if (dungeon != null)
        {
            dungeon.OnDungeonCleared -= HandleDungeonCleared;
            dungeon.OnBossSpawned -= HandleBossSpawned;
        }
        if (enemyManager != null) enemyManager.OnEnemyDied -= HandleEnemyDied;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame && (gameState == GameState.Playing || gameState == GameState.Paused))
        {
            TogglePause();
        }
        if (keyboard.rKey.wasPressedThisFrame && (gameState == GameState.Victory || gameState == GameState.GameOver))
        {
            Restart();
        }
#if UNITY_EDITOR
        DebugKeys(keyboard);
#endif
    }

#if UNITY_EDITOR
    // ปุ่มลัดไว้ทดสอบใน Editor เท่านั้น (Build จริงไม่มี)
    // F8 = อมตะ / F9 = ฆ่าศัตรูทั้งหมด / F10 = วาร์ปไปห้องถัดไป (ห้องตอนนี้ต้องผ่านแล้ว) / F11 = บอสเลือดลด 20%
    private bool godMode;

    private void DebugKeys(Keyboard keyboard)
    {
        if (gameState != GameState.Playing) return;
        if (keyboard.f8Key.wasPressedThisFrame)
        {
            godMode = !godMode;
            Debug.Log("[Debug] God Mode : " + godMode);
        }
        if (godMode && player.IsAlive) player.Heal(player.MaxHP);
        if (keyboard.f9Key.wasPressedThisFrame)
        {
            Debug.Log("[Debug] ฆ่าศัตรูทั้งหมด");
            StartCoroutine(KillRemainingEnemies());
        }
        if (keyboard.f11Key.wasPressedThisFrame && dungeon.CurrentRoom != null && dungeon.CurrentRoom.Boss != null)
        {
            Enemy boss = dungeon.CurrentRoom.Boss;
            boss.TakeDamage(boss.MaxHP * 0.2f);
            Debug.Log("[Debug] บอสเลือดเหลือ " + boss.CurrentHP + "/" + boss.MaxHP);
        }
        if (keyboard.f10Key.wasPressedThisFrame)
        {
            Room next = dungeon.GetRoom(dungeon.CurrentRoomIndex + 1);
            if (dungeon.CurrentRoom != null && dungeon.CurrentRoom.IsCleared && next != null)
            {
                Vector3 target = next.transform.position + new Vector3(0f, 0.2f, -next.enterHalfSize.y + 1.5f);
                CharacterController cc = player.GetComponent<CharacterController>();
                cc.enabled = false;
                player.transform.position = target;
                cc.enabled = true;
                Debug.Log("[Debug] วาร์ปไป " + next.roomName);
            }
        }
    }
#endif

    public void TogglePause()
    {
        if (gameState == GameState.Playing)
        {
            gameState = GameState.Paused;
            Time.timeScale = 0f;
            hud.ShowPause(true);
        }
        else if (gameState == GameState.Paused)
        {
            gameState = GameState.Playing;
            Time.timeScale = 1f;
            hud.ShowPause(false);
        }
    }

    // เล่นใหม่ = โหลดซีนนี้ใหม่ทั้งหมด (ศัตรู/ประตู/เลือด กลับไปเริ่มต้น)
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    #region Events
    private void HandleEnemyDied(Enemy enemy)
    {
        killCount++;
    }

    private void HandleBossSpawned(Enemy boss)
    {
        Debug.Log("[GameManager] บอส " + boss.characterName + " ปรากฏตัว!");
    }

    private void HandlePlayerDied(Character character)
    {
        if (gameState == GameState.Victory || gameState == GameState.GameOver) return;
        StartCoroutine(GameOverRoutine());
    }

    private void HandleDungeonCleared()
    {
        if (gameState == GameState.GameOver) return;
        StartCoroutine(VictoryRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        gameState = GameState.GameOver;
        Debug.Log("[GameManager] Game Over");
        yield return new WaitForSecondsRealtime(gameOverDelay);
        hud.ShowEnd(false, Time.time - startTime, killCount);
    }

    private IEnumerator VictoryRoutine()
    {
        gameState = GameState.Victory;
        float playTime = Time.time - startTime;
        Debug.Log("[GameManager] Victory! เวลา " + playTime.ToString("0.0") + " วินาที ฆ่า " + killCount + " ตัว");

        // 1. ลูกน้องที่เหลือตายตามบอส
        StartCoroutine(KillRemainingEnemies());

        // 2. สโลว์โมชันตอนบอสล้ม
        Time.timeScale = bossKillSlowMotion;
        yield return new WaitForSecondsRealtime(bossKillSlowTime);
        Time.timeScale = 1f;
        hud.HideBoss();

        yield return new WaitForSecondsRealtime(1.2f);
        hud.ShowEnd(true, playTime, killCount);
    }

    // ฆ่าศัตรูที่เหลือทั้งหมด: ทำหลายเฟรม เพราะสไลม์ตัวใหญ่ตายแล้วแตกเป็นตัวเล็ก
    // (ตัวเล็กที่เพิ่งเกิดยังไม่ได้ SetUP ในเฟรมนั้น ต้องรอเฟรมถัดไปถึงจะตีโดน)
    private IEnumerator KillRemainingEnemies()
    {
        for (int pass = 0; pass < 10 && enemyManager.GetAliveCount() > 0; pass++)
        {
            // ก็อปรายการก่อน เพราะศัตรูตายแล้วจะถูกลบออกจาก AliveEnemies ระหว่างวนลูป
            Enemy[] alive = new Enemy[enemyManager.AliveEnemies.Count];
            for (int i = 0; i < alive.Length; i++) alive[i] = enemyManager.AliveEnemies[i];
            for (int i = 0; i < alive.Length; i++)
            {
                if (alive[i] != null && alive[i].IsAlive) alive[i].TakeDamage(alive[i].MaxHP * 10f);
            }
            yield return null;
        }
    }
    #endregion
}
