using System;
using System.Collections.Generic;
using UnityEngine;

// คุมศัตรูที่ยังมีชีวิตในห้อง (EnemyManager ตาม Diagram)
// DungeonManager.SpawnNextEnemy() -> EnemyManager.SpawnEnemy(room.GetNextEnemyData())
// ศัตรูตายครบ -> OnAllDefeated -> DungeonManager ไปห้องถัดไป
public class EnemyManager : MonoBehaviour
{
    [SerializeField] private Player player;
    [Tooltip("จุดเกิดศัตรู วนใช้ทีละจุด — ไม่ใส่จะสุ่มรอบ ๆ ตัว EnemyManager")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float randomSpawnRadius = 6f;
    [Header("ล้อมผู้เล่น")]
    [Tooltip("แจกช่องรอบตัวผู้เล่นใหม่ทุกกี่วินาที")]
    [SerializeField] private float slotRefreshTime = 0.5f;
    [Header("เก่งขึ้นตามเลเวลผู้เล่น")]
    [Tooltip("ศัตรูที่เกิดใหม่ได้เลเวลเท่าผู้เล่น -> เลือด/ดาเมจคูณตาม hpPerLevel / damagePerLevel ใน EnemyData")]
    [SerializeField] private bool scaleWithPlayerLevel = true;
    [Tooltip("เลเวลศัตรูสูงสุด (กันตัวเลขพุ่งเกิน)")]
    [SerializeField] private int maxEnemyLevel = 20;

    // ศัตรูที่ยังไม่ตาย (List ตาม Diagram) — เพิ่มตอนเกิด ลบตอนตาย
    private List<Enemy> aliveEnemies = new List<Enemy>();
    private int nextSpawnIndex;
    private float slotTimer;

    public event Action OnAllDefeated;
    public event Action<Enemy> OnEnemyDied;
    public IReadOnlyList<Enemy> AliveEnemies => aliveEnemies;
    public Player Player => player;

    private void Awake()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
    }

    private void Update()
    {
        slotTimer -= Time.deltaTime;
        if (slotTimer <= 0f)
        {
            slotTimer = slotRefreshTime;
            AssignSurroundSlots();
        }
    }

    // แจกช่องรอบตัวผู้เล่นให้ศัตรูเท่า ๆ กัน (n ตัว = ห่างกัน 360/n องศา)
    // ตัวที่อยู่ซ้ายได้ช่องซ้าย ตัวที่อยู่ขวาได้ช่องขวา -> ไม่มีใครต้องเดินตัดหน้ากัน
    private void AssignSurroundSlots()
    {
        if (player == null) return;

        // 1. เก็บศัตรูที่ล้อมได้ เรียงตามมุมรอบตัวผู้เล่น (Insertion Sort ลง List)
        List<Enemy> ring = new List<Enemy>();
        List<float> angles = new List<float>();
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            Enemy enemy = aliveEnemies[i];
            if (enemy == null || !enemy.IsAlive || !enemy.surroundPlayer) continue;

            Vector3 fromPlayer = enemy.transform.position - player.transform.position;
            float angle = Mathf.Atan2(fromPlayer.x, fromPlayer.z) * Mathf.Rad2Deg;

            int index = angles.Count;
            while (index > 0 && angles[index - 1] > angle) index--;
            angles.Insert(index, angle);
            ring.Insert(index, enemy);
        }
        if (ring.Count == 0) return;

        // 2. ระยะห่างของช่อง
        float step = 360f / ring.Count;

        // 3. หามุมเริ่มที่ทุกตัวเดินน้อยที่สุด = ค่าเฉลี่ยแบบวงกลมของ (มุมจริง - ลำดับ x step)
        float sumX = 0f;
        float sumZ = 0f;
        for (int i = 0; i < ring.Count; i++)
        {
            float a = (angles[i] - i * step) * Mathf.Deg2Rad;
            sumX += Mathf.Sin(a);
            sumZ += Mathf.Cos(a);
        }
        float startAngle = Mathf.Atan2(sumX, sumZ) * Mathf.Rad2Deg;

        // 4. แจกช่อง
        for (int i = 0; i < ring.Count; i++)
        {
            ring[i].SetSurroundAngle(startAngle + i * step);
        }
    }

    // เกิดที่จุดเกิดถัดไป
    public Enemy SpawnEnemy(EnemyData data)
    {
        return SpawnEnemy(data, GetSpawnPosition());
    }

    // เกิดตรงตำแหน่งที่กำหนด (สไลม์แตกตัวจะใช้อันนี้)
    public Enemy SpawnEnemy(EnemyData data, Vector3 position)
    {
        if (data == null || data.prefab == null)
        {
            Debug.LogError("EnemyManager: EnemyData หรือ prefab ว่าง");
            return null;
        }

        Quaternion rotation = Quaternion.identity;
        if (player != null)
        {
            Vector3 look = player.transform.position - position;
            look.y = 0f;
            if (look.sqrMagnitude > 0.01f) rotation = Quaternion.LookRotation(look);
        }

        Enemy enemy = Instantiate(data.prefab, position, rotation);
        enemy.name = data.enemyName;
        int level = GetEnemyLevel();
        enemy.Init(data, player, level);
        if (level > 1)
        {
            Debug.Log("[EnemyManager] " + data.enemyName + " Lv." + level + " HP x" + data.HPMultiplier(level).ToString("0.00")
                + " Damage x" + data.DamageMultiplier(level).ToString("0.00"));
        }
        Register(enemy);
        return enemy;
    }

    // ศัตรูที่วางในซีนเองก็นับได้ ถ้าเรียก Register
    public void Register(Enemy enemy)
    {
        if (enemy == null || aliveEnemies.Contains(enemy)) return;
        // ศัตรูไม่ชนกันเอง (CharacterController ชนกันแล้วปีนขึ้นหัวกันได้ = ลอย/ซ้อนหัว)
        // ระยะห่างระหว่างกันใช้ Enemy.Separation() ดันออกแทน
        CharacterController newCC = enemy.GetComponent<CharacterController>();
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            if (aliveEnemies[i] == null) continue;
            CharacterController otherCC = aliveEnemies[i].GetComponent<CharacterController>();
            if (newCC != null && otherCC != null) Physics.IgnoreCollision(newCC, otherCC, true);
        }
        aliveEnemies.Add(enemy);
        enemy.SetManager(this);
        slotTimer = 0f; // มีตัวใหม่ แจกช่องใหม่ทันที
        enemy.OnDied += HandleEnemyDied;
    }

    // เลเวลของศัตรูที่จะเกิด = เลเวลผู้เล่นตอนนี้ (ไม่เกิน maxEnemyLevel)
    public int GetEnemyLevel()
    {
        if (!scaleWithPlayerLevel || player == null) return 1;
        return Mathf.Clamp(player.Stats.Level, 1, maxEnemyLevel);
    }

    public int GetAliveCount()
    {
        return aliveEnemies.Count;
    }

    // ศัตรูที่ใกล้ผู้เล่นที่สุด (ไว้ใช้กับสกิลล็อกเป้า)
    public Enemy FindNearestEnemy()
    {
        if (player == null) return null;
        return FindNearestEnemy(player.transform.position);
    }

    public Enemy FindNearestEnemy(Vector3 from)
    {
        Enemy nearest = null;
        float best = float.MaxValue;
        for (int i = 0; i < aliveEnemies.Count; i++)
        {
            float d = (aliveEnemies[i].transform.position - from).sqrMagnitude;
            if (d < best)
            {
                best = d;
                nearest = aliveEnemies[i];
            }
        }
        return nearest;
    }

    private void HandleEnemyDied(Character character)
    {
        character.OnDied -= HandleEnemyDied;
        Enemy enemy = character as Enemy;
        aliveEnemies.Remove(enemy);
        OnEnemyDied?.Invoke(enemy);

        if (aliveEnemies.Count == 0)
        {
            Debug.Log("All enemies defeated");
            OnAllDefeated?.Invoke();
        }
    }

    private Vector3 GetSpawnPosition()
    {
        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[nextSpawnIndex % spawnPoints.Length];
            nextSpawnIndex++;
            if (point != null) return point.position;
        }
        Vector2 r = UnityEngine.Random.insideUnitCircle * randomSpawnRadius;
        return transform.position + new Vector3(r.x, 0f, r.y);
    }
}
