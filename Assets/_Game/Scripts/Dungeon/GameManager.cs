
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [SerializeField] private GameState gameState = GameState.Playing;
    [SerializeField] private Player player;
    [SerializeField] private DungeonManager dungeon;
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private GameplayHUD hud;
    [SerializeField] private int currentLevel = 1;

    // เพิ่ม: หน้าจอแพ้และชนะ
    [Header("Result Screens")]
    [SerializeField] private GameObject defeatScreen;
    [SerializeField] private GameObject victoryScreen;

    [Header("Game Feel")]
    [Tooltip("บอสตาย: สโลว์โมชันช้าแค่ไหน (0.25 = ช้าลง 4 เท่า)")]
    [SerializeField] private float bossKillSlowMotion = 0.25f;
    [SerializeField] private float bossKillSlowTime = 1.5f;   
    [SerializeField] private float gameOverDelay = 2f;       

    private int killCount;
    private float startTime;

    public GameState State => gameState;

    private void Awake()
    {
        Instance = this;
        Time.timeScale = 1f; 

        if (defeatScreen != null)
            defeatScreen.SetActive(false);

        if (victoryScreen != null)
            victoryScreen.SetActive(false);
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
        if (keyboard.f7Key.wasPressedThisFrame || keyboard.lKey.wasPressedThisFrame)
        {
            // F7 หรือ L = เลเวลอัป 1 ครั้ง (ไว้ดูศัตรูเก่งขึ้นตามเลเวล)
            player.Stats.AddExp(player.Stats.ExpToNextLevel - player.Stats.Exp);
            Debug.Log("[Debug] ผู้เล่นเลเวล " + player.Stats.Level + " -> ศัตรูที่เกิดใหม่เลเวล " + enemyManager.GetEnemyLevel());
        }
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
    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

        if (defeatScreen != null)
            defeatScreen.SetActive(true);
    }

    private IEnumerator VictoryRoutine()
    {
        gameState = GameState.Victory;
        float playTime = Time.time - startTime;
        Debug.Log("[GameManager] Victory! เวลา " + playTime.ToString("0.0") + " วินาที ฆ่า " + killCount + " ตัว");

        StartCoroutine(KillRemainingEnemies());

        Time.timeScale = bossKillSlowMotion;
        yield return new WaitForSecondsRealtime(bossKillSlowTime);
        Time.timeScale = 1f;
        hud.HideBoss();

        yield return new WaitForSecondsRealtime(1.2f);
        hud.ShowEnd(true, playTime, killCount);

        if (victoryScreen != null)
            victoryScreen.SetActive(true);
    }

    private IEnumerator KillRemainingEnemies()
    {
        for (int pass = 0; pass < 10 && enemyManager.GetAliveCount() > 0; pass++)
        {
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
