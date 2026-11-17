
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

        hud.SetUP(dungeon, enemyManager);
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
    }

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

    // ชนะแล้วฆ่าศัตรูที่เหลือ (วนหลายเฟรม เพราะสไลม์ที่แตกตัวเกิดเฟรมถัดไป)
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
