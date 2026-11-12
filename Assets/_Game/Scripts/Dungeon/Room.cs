using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ห้องหนึ่งห้องในดันเจี้ยน (Room ตาม Diagram) มีหลายเวฟ
// ผู้เล่นเดินเข้าห้อง -> ประตูหลังปิด (ขังไว้) -> เวฟ 1, 2, 3 ... -> ตายหมด -> ประตูหน้าเปิด
// ห้องบอส: เวฟแรกคือบอส / เวฟถัดไปคือลูกน้อง เกิดเมื่อเลือดบอสลดถึงที่กำหนด / บอสตาย = ผ่านห้อง
public class Room : MonoBehaviour
{
    [Header("Room")]
    public int roomId;
    public string roomName = "Room";
    public bool isBossRoom;
    [Tooltip("ขนาดครึ่งหนึ่งของพื้นที่ (x = กว้าง, y = ลึก) วัดจากตัว Room ใช้เช็กว่าผู้เล่นเดินเข้ามาแล้ว")]
    public Vector2 enterHalfSize = new Vector2(7f, 6f);
    public List<Wave> waves = new List<Wave>();
    [Tooltip("พักระหว่างเวฟกี่วินาที")]
    public float timeBetweenWaves = 2.5f;

    [Header("ประตูทางเข้า (ปิดขังตอนเริ่มห้อง)")]
    public Transform entryGate;
    public Collider entryBlocker;
    public Light entrySeal;

    [Header("ประตูทางออก (เปิดเมื่อผ่านห้อง)")]
    public Transform exitGate;
    public Collider exitBlocker;
    public Light exitSeal;

    private static readonly Color OpenColor = new Color(0.2f, 1f, 0.65f);
    private static readonly Color ClosedColor = new Color(1f, 0.18f, 0.08f);
    private const float GateDownY = -3.7f; // ประตูจมลงพื้น = เปิด
    private const float GateSpeed = 5f;

    private int currentWaveIndex = -1;
    private bool isStarted;
    private bool isCleared;
    private bool isWaiting; // กำลังพักก่อนเวฟถัดไป
    private Enemy boss;

    private EnemyManager manager;
    private Player player;
    private Material indicatorMaterial;

    public event Action<Room, int> OnWaveStarted;  // (ห้อง, เวฟที่)
    public event Action<Room> OnCleared;
    public event Action<Enemy> OnBossSpawned;

    public EnemyManager Manager => manager;
    public Player Player => player;
    public Material IndicatorMaterial => indicatorMaterial;
    public bool IsStarted => isStarted;
    public bool IsCleared => isCleared;
    public Enemy Boss => boss;
    public int CurrentWaveNumber => currentWaveIndex + 1;
    public int WaveCount => isBossRoom ? 1 : waves.Count;

    // DungeonManager เรียกตอนเริ่มเกม
    public void SetUP(EnemyManager enemyManager, Player target, Material indicatorMat)
    {
        manager = enemyManager;
        player = target;
        indicatorMaterial = indicatorMat;
        for (int i = 0; i < waves.Count; i++) waves[i].waveNumber = i + 1;
        SnapGates();
    }

    // ผู้เล่นอยู่ในพื้นที่ห้องไหม (เช็กสี่เหลี่ยมรอบตัว Room)
    public bool IsPlayerInside(Vector3 position)
    {
        Vector3 local = position - transform.position;
        return Mathf.Abs(local.x) <= enterHalfSize.x && Mathf.Abs(local.z) <= enterHalfSize.y;
    }

    public void StartRoom()
    {
        if (isStarted) return;
        isStarted = true;
        Debug.Log("[Room] เข้าห้อง " + roomId + " : " + roomName);
        StartNextWave();
    }

    public void StartNextWave()
    {
        currentWaveIndex++;
        if (currentWaveIndex >= waves.Count) return;
        // ห้องบอส: เวฟถัดจากบอสรอเลือดบอสลดก่อน ไม่เริ่มต่อกันเอง
        if (isBossRoom && currentWaveIndex > 0) return;
        StartCoroutine(waves[currentWaveIndex].SpawnWave(this));
        OnWaveStarted?.Invoke(this, currentWaveIndex + 1);
    }

    private IEnumerator NextWaveAfterDelay()
    {
        isWaiting = true;
        yield return new WaitForSeconds(timeBetweenWaves);
        isWaiting = false;
        StartNextWave();
    }

    // Wave แจ้งทุกครั้งที่เกิดศัตรู: ให้ไล่ผู้เล่นตลอด / ห้องบอส: ตัวแรกที่เกิด = บอส
    public void HandleEnemySpawned(Enemy enemy)
    {
        if (enemy == null) return;
        enemy.huntPlayer = true;
        if (isBossRoom && boss == null)
        {
            boss = enemy;
            OnBossSpawned?.Invoke(boss);
        }
    }

    private void Update()
    {
        UpdateGates();
        if (!isStarted || isCleared || manager == null) return;

        if (isBossRoom) UpdateBossRoom();
        else UpdateNormalRoom();
    }

    private void UpdateNormalRoom()
    {
        if (isWaiting) return;
        if (!waves[currentWaveIndex].IsCompleted(manager)) return;

        if (currentWaveIndex + 1 < waves.Count) StartCoroutine(NextWaveAfterDelay());
        else Clear();
    }

    private void UpdateBossRoom()
    {
        if (boss == null) return; // บอสยังไม่เกิด (วงม่วงยังไม่หมดเวลา)

        // 1. บอสตาย = ผ่านห้อง
        if (!boss.IsAlive)
        {
            Clear();
            return;
        }

        // 2. เลือดบอสลดถึงที่กำหนด -> เรียกลูกน้อง (เวฟละครั้ง)
        float hpRatio = boss.CurrentHP / boss.MaxHP;
        for (int i = 1; i < waves.Count; i++)
        {
            Wave wave = waves[i];
            if (!wave.HasStarted && hpRatio <= wave.bossHpTrigger)
            {
                StartCoroutine(wave.SpawnWave(this));
                OnWaveStarted?.Invoke(this, i + 1);
            }
        }
    }

    public bool CheckCleared()
    {
        return isCleared;
    }

    private void Clear()
    {
        isCleared = true;
        Debug.Log("[Room] ผ่านห้อง " + roomName);
        OpenDoor();
        OnCleared?.Invoke(this);
    }

    public void OpenDoor()
    {
        // ประตูเลื่อนลงเองใน UpdateGates()
        Debug.Log("[Room] เปิดประตู " + roomName);
    }

    #region Gates
    // ทางเข้า: เปิดก่อนเริ่มห้อง / ปิดระหว่างสู้ / เปิดอีกครั้งเมื่อผ่าน
    private bool EntryOpen => !isStarted || isCleared;
    private bool ExitOpen => isCleared;

    private void SnapGates()
    {
        SetGate(entryGate, entryBlocker, entrySeal, EntryOpen, true);
        SetGate(exitGate, exitBlocker, exitSeal, ExitOpen, true);
    }

    private void UpdateGates()
    {
        SetGate(entryGate, entryBlocker, entrySeal, EntryOpen, false);
        SetGate(exitGate, exitBlocker, exitSeal, ExitOpen, false);
    }

    private void SetGate(Transform gate, Collider blocker, Light seal, bool open, bool instant)
    {
        if (blocker != null) blocker.enabled = !open;
        if (seal != null) seal.color = open ? OpenColor : ClosedColor;
        if (gate == null) return;
        Vector3 p = gate.localPosition;
        float targetY = open ? GateDownY : 0f;
        p.y = instant ? targetY : Mathf.MoveTowards(p.y, targetY, GateSpeed * Time.deltaTime);
        gate.localPosition = p;
    }
    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(enterHalfSize.x * 2f, 1f, enterHalfSize.y * 2f));
        Gizmos.color = Wave.SpawnColor;
        for (int i = 0; i < waves.Count; i++)
        {
            for (int j = 0; j < waves[i].spawnPoints.Count; j++) Gizmos.DrawWireSphere(waves[i].spawnPoints[j], 0.5f);
        }
    }
}
