using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// คุมลำดับห้องในดันเจี้ยน (DungeonManager ตาม Diagram)
// ห้อง 1 -> ผ่าน -> เปิดประตู -> ผู้เล่นเดินเข้าห้อง 2 -> ... -> ห้องบอส -> บอสตาย = ผ่านดันเจี้ยน
public class DungeonManager : MonoBehaviour
{
    [SerializeField] private List<Room> rooms = new List<Room>();
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private Player player;
    [Tooltip("Material ของวงเตือนบนพื้น (Sprites/Default) ใช้ร่วมกับ AttackIndicator")]
    [SerializeField] private Material indicatorMaterial;
    [Tooltip("รอกี่วินาทีก่อนห้องแรกเริ่ม")]
    [SerializeField] private float firstRoomDelay = 2.5f;
    [Tooltip("ผ่านห้องแล้วฟื้นเลือดกี่ส่วน (0.35 = 35% ของเลือดเต็ม)")]
    [Range(0f, 1f)] [SerializeField] private float healOnRoomClear = 0.35f;

    [Header("Sanctuary - จุดพักก่อนห้องบอส")]
    [SerializeField] private Transform sanctuary;
    [SerializeField] private float sanctuaryRadius = 2.5f;

    private int currentRoomIndex = -1;
    private bool sanctuaryUsed;
    private bool dungeonCleared;

    public event Action<Room> OnRoomEntered;
    public event Action<Room> OnRoomCleared;
    public event Action<Room, int> OnWaveStarted;
    public event Action<Enemy> OnBossSpawned;
    public event Action OnDungeonCleared;
    public event Action<string, string> OnAnnounce; // (หัวข้อใหญ่, ข้อความเล็ก) ให้ HUD แสดงกลางจอ

    public Room CurrentRoom => currentRoomIndex >= 0 && currentRoomIndex < rooms.Count ? rooms[currentRoomIndex] : null;
    public int CurrentRoomIndex => currentRoomIndex;
    public int RoomCount => rooms.Count;
    public EnemyManager Enemies => enemyManager;

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        if (enemyManager == null) enemyManager = FindAnyObjectByType<EnemyManager>();
        GenerateDungeon(1);
        StartCoroutine(FirstRoomRoutine());
    }

    // เตรียมห้องทั้งหมด: เรียงตาม roomId (Insertion Sort) แล้วผูก event
    public void GenerateDungeon(int level)
    {
        // 1. ไม่ได้ใส่ห้องไว้ -> หาจากลูกของตัวเอง
        if (rooms.Count == 0) rooms.AddRange(GetComponentsInChildren<Room>());

        // 2. Insertion Sort ตาม roomId
        for (int i = 1; i < rooms.Count; i++)
        {
            Room key = rooms[i];
            int j = i - 1;
            while (j >= 0 && rooms[j].roomId > key.roomId)
            {
                rooms[j + 1] = rooms[j];
                j--;
            }
            rooms[j + 1] = key;
        }

        // 3. ตั้งค่าแต่ละห้อง
        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            room.SetUP(enemyManager, player, indicatorMaterial);
            room.OnCleared += HandleRoomCleared;
            room.OnWaveStarted += HandleWaveStarted;
            room.OnBossSpawned += HandleBossSpawned;
        }
        Debug.Log("[Dungeon] เลเวล " + level + " มีทั้งหมด " + rooms.Count + " ห้อง");
    }

    private IEnumerator FirstRoomRoutine()
    {
        if (rooms.Count == 0) yield break;
        OnAnnounce?.Invoke(rooms[0].roomName, "Defeat every enemy to break the seal");
        yield return new WaitForSeconds(firstRoomDelay);
        EnterRoom(0);
    }

    private void Update()
    {
        if (player == null || !player.IsAlive || dungeonCleared) return;

        // 1. ห้องตอนนี้ผ่านแล้ว + ผู้เล่นเดินเข้าห้องถัดไป -> เริ่มห้องนั้น
        Room room = CurrentRoom;
        int next = currentRoomIndex + 1;
        if (room != null && room.IsCleared && next < rooms.Count && rooms[next].IsPlayerInside(player.transform.position))
        {
            EnterRoom(next);
        }

        // 2. จุดพัก: เหยียบวงทองแล้วเลือดเต็ม (ครั้งเดียว)
        if (!sanctuaryUsed && sanctuary != null)
        {
            Vector3 diff = player.transform.position - sanctuary.position;
            diff.y = 0f;
            if (diff.magnitude <= sanctuaryRadius)
            {
                sanctuaryUsed = true;
                player.Heal(player.MaxHP);
                OnAnnounce?.Invoke("SANCTUARY", "Your wounds are healed. The warlord awaits...");
                Debug.Log("[Dungeon] จุดพัก: เลือดเต็ม");
            }
        }
    }

    public void EnterRoom(int index)
    {
        if (index < 0 || index >= rooms.Count) return;
        currentRoomIndex = index;
        Room room = rooms[index];
        string subtitle = room.isBossRoom ? "Slay the warlord" : room.waves.Count + " waves - survive them all";
        OnAnnounce?.Invoke(room.roomName, subtitle);
        room.StartRoom();
        OnRoomEntered?.Invoke(room);
    }

    // ผ่านห้อง: ฟื้นเลือด + เปิดทางไปห้องถัดไป / ห้องสุดท้าย = ผ่านดันเจี้ยน
    public void UnlockNextRoom()
    {
        Room room = CurrentRoom;
        if (room == null) return;
        room.OpenDoor();
        if (currentRoomIndex + 1 >= rooms.Count)
        {
            dungeonCleared = true;
            Debug.Log("[Dungeon] ผ่านดันเจี้ยนแล้ว!");
            OnDungeonCleared?.Invoke();
            return;
        }
        if (player != null) player.Heal(player.MaxHP * healOnRoomClear);
        OnAnnounce?.Invoke("CHAMBER CLEARED", "The seal is broken - follow the lanterns");
    }

    public Room GetRoom(int index)
    {
        if (index < 0 || index >= rooms.Count) return null;
        return rooms[index];
    }

    public bool IsBossRoom()
    {
        Room room = CurrentRoom;
        return room != null && room.isBossRoom;
    }

    private void HandleRoomCleared(Room room)
    {
        OnRoomCleared?.Invoke(room);
        UnlockNextRoom();
    }

    private void HandleWaveStarted(Room room, int waveNumber)
    {
        if (!room.isBossRoom && waveNumber > 1)
        {
            OnAnnounce?.Invoke("WAVE " + waveNumber, waveNumber == room.waves.Count ? "Final wave" : "");
        }
        if (room.isBossRoom && waveNumber > 1)
        {
            OnAnnounce?.Invoke("THE WARLORD CALLS FOR AID", "");
        }
        OnWaveStarted?.Invoke(room, waveNumber);
    }

    private void HandleBossSpawned(Enemy boss)
    {
        OnBossSpawned?.Invoke(boss);
    }

    private void OnDrawGizmosSelected()
    {
        if (sanctuary == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(sanctuary.position, sanctuaryRadius);
    }
}
