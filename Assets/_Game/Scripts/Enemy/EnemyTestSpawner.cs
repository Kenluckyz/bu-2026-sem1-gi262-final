using UnityEngine;
using UnityEngine.InputSystem;

// ใช้ในซีน Test_Enemy เท่านั้น (ของจริง DungeonManager จะเป็นคนสั่ง EnemyManager)
// เริ่มเกม: เกิดศัตรู startCount ตัว / กด G: เกิดเพิ่ม 1 ตัว / ตายหมด: เกิดชุดใหม่
// ตอนกด Play: คลิก ⋮ มุมขวาของคอมโพเนนต์นี้ใน Inspector -> Spawn Selected / Spawn Boss / Kill All Enemies
public class EnemyTestSpawner : MonoBehaviour
{
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private EnemyData[] enemies;
    [SerializeField] private int startCount = 3;
    [SerializeField] private bool respawnWhenCleared = true;
    [SerializeField] private float respawnDelay = 2f;
    [Tooltip("ลำดับใน enemies ที่จะเกิดเมื่อสั่ง Spawn Selected (0 = ตัวแรก)")]
    [SerializeField] private int selectedIndex = 0;
    [Tooltip("บอส (ไม่สุ่มเกิดในชุดปกติ) — เกิดด้วยเมนู Spawn Boss")]
    [SerializeField] private EnemyData boss;

    private void Start()
    {
        if (enemyManager == null) enemyManager = FindAnyObjectByType<EnemyManager>();
        if (enemyManager == null || enemies == null || enemies.Length == 0)
        {
            Debug.LogWarning("EnemyTestSpawner: ยังไม่ได้ใส่ EnemyManager หรือ EnemyData");
            enabled = false;
            return;
        }
        enemyManager.OnAllDefeated += HandleAllDefeated;
        SpawnWave();
    }

    private void OnDestroy()
    {
        if (enemyManager != null) enemyManager.OnAllDefeated -= HandleAllDefeated;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.gKey.wasPressedThisFrame) SpawnOne();
    }

    private void SpawnWave()
    {
        for (int i = 0; i < startCount; i++) SpawnOne();
    }

    private void SpawnOne()
    {
        EnemyData data = enemies[Random.Range(0, enemies.Length)];
        enemyManager.SpawnEnemy(data);
    }

    // เมนูทดสอบใน Inspector (ใช้ตอนกด Play)
    [ContextMenu("Spawn Selected")]
    private void SpawnSelected()
    {
        if (enemyManager == null || enemies == null || enemies.Length == 0) return;
        int index = Mathf.Clamp(selectedIndex, 0, enemies.Length - 1);
        enemyManager.SpawnEnemy(enemies[index]);
    }

    [ContextMenu("Spawn Boss")]
    private void SpawnBoss()
    {
        if (enemyManager == null || boss == null) return;
        enemyManager.SpawnEnemy(boss);
    }

    // ตายหมดแล้วเกิดชุดใหม่เองไหม (EnemySpawnPanel มีปุ่มเปิด/ปิด)
    public bool RespawnWhenCleared
    {
        get { return respawnWhenCleared; }
        set { respawnWhenCleared = value; }
    }

    [ContextMenu("Kill All Enemies")]
    public void KillAll()
    {
        if (enemyManager == null) return;
        // ก๊อปรายการก่อน เพราะศัตรูที่ตายจะถูกลบออกจาก List ระหว่างวนลูป (และสไลม์จะเพิ่มลูกเข้ามา)
        Enemy[] targets = new Enemy[enemyManager.AliveEnemies.Count];
        for (int i = 0; i < targets.Length; i++)
        {
            targets[i] = enemyManager.AliveEnemies[i];
        }
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] != null) targets[i].TakeDamage(9999f);
        }
    }

    private void HandleAllDefeated()
    {
        if (respawnWhenCleared) Invoke(nameof(SpawnWave), respawnDelay);
    }
}
