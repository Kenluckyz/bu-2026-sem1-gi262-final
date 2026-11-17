using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DungeonManager : MonoBehaviour
{
    [SerializeField] private List<Room> rooms = new List<Room>();
    [SerializeField] private EnemyManager enemyManager;
    [SerializeField] private Player player;
    [SerializeField] private Material indicatorMaterial;
    [SerializeField] private float firstRoomDelay = 2.5f;
    [Range(0f, 1f)] [SerializeField] private float healOnRoomClear = 0.35f;
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
    public event Action<string, string> OnAnnounce; //พวกข้อความ ผ่านห้อง ที่ส่งต่อไปให้ GameplayHUD โชว์

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

    public void GenerateDungeon(int level)
    {
        //  ตรวจสอบว่า list มีห้องมั้ย เเล้วก็นำห้องที่มีมาเพิ่ม
        if (rooms.Count == 0) rooms.AddRange(GetComponentsInChildren<Room>());

        // ใช้ Insertion Sort เพื่อใช้เรียงลำดับห้อง 
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

        // วนลูปทุกห้องเพื่อ setup เวลา clear หรือ เริ่ม wave เเละ boss spawn
        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            room.SetUP(enemyManager, player, indicatorMaterial); // ส่งข้อมูลที่แต่ละห้องต้องใช้
            room.OnCleared += HandleRoomCleared;
            room.OnWaveStarted += HandleWaveStarted;
            room.OnBossSpawned += HandleBossSpawned;
        }
    }

    private IEnumerator FirstRoomRoutine()
    {
        if (rooms.Count == 0) yield break; // ตรวจสอบว่ามีห้องมั้ย
        OnAnnounce?.Invoke("ROOM 1", "Defeat every enemy to break the seal"); // ประกาศของห้องเเรกว่าให้ ผู้เล่นสู้จัดการศัตรูให้หมด
        yield return new WaitForSeconds(firstRoomDelay); // รอเวลาก่อนเริ่มห้องเเรก 
        EnterRoom(0); // เริ่มห้องแรก
    }

    private void Update()
    {
        if (player == null || !player.IsAlive || dungeonCleared) return; // ตรวจสอบว่าปู้เล่นยังอยู่มั้ย เคลียดันเจี้ยนได้หรือยัง 

        Room room = CurrentRoom;
        int next = currentRoomIndex + 1;
        if (room != null && room.IsCleared && next < rooms.Count && rooms[next].IsPlayerInside(player.transform.position)) // ต้องผ่านเงื่อนไขทั้งหมด ถึงจะผ่านไปห้องต่อไปได้ 
        {
            EnterRoom(next);
        }

        // จุดฮีล 
        if (!sanctuaryUsed && sanctuary != null)
        {
            Vector3 diff = player.transform.position - sanctuary.position; //คำนวณระยะห่างระหว่าง ตัวละคร กับจุดฮีล
            diff.y = 0f; //ไม่สนใจความสูง คำนวณระยะห่างเฉพาะแกน X และ Z
            if (diff.magnitude <= sanctuaryRadius) // ตรวจสอบว่า Boat เข้าจุดวงฮีลหรือยัง
            {
                sanctuaryUsed = true;
                player.Heal(player.MaxHP); // ฮีลเลือดผู้เล่น
                OnAnnounce?.Invoke("SANCTUARY", "Your wounds are healed. The warlord awaits...");
            }
        }
    }

    public void EnterRoom(int index)
    {
        if (index < 0 || index >= rooms.Count) return; //ตรวจสอบว่าห้องที่ต้องเข้าไปมีอยู่จิงมั้ย
        currentRoomIndex = index; // บันทึกว่าตอนนี้เราอยู่ห้องไหน
        Room room = rooms[index]; // ดึงข้อมูลห้องจาก List Room มาเก็บไว้ในตัวแปร room
        string subtitle = room.isBossRoom ? "Slay the warlord" : room.waves.Count + " waves - survive them all"; // เชควาสใช่ห้องบอสทั้ย
        OnAnnounce?.Invoke("ROOM " + (index + 1), subtitle); // ส่งข้อความไปให้ GameplayHUD โชว์ว่าอยู่ห้องไหน 
        room.StartRoom(); // เรียกใช้method startRoom 
        OnRoomEntered?.Invoke(room);
    }

    public void UnlockNextRoom()
    {
        Room room = CurrentRoom; // ดึงข้อมูลห้องปัจจุบันมาเก็บไว้ในตัวแปร room
        if (room == null) return; 
        room.OpenDoor(); // เรียกใช้ method OpenDoor ของห้องปัจจุบันเพื่อเปิดประตูไปห้องต่อไป
        if (currentRoomIndex + 1 >= rooms.Count) // ตรงนี้ใช้ตรวจสอบว่า ยังมีห้องถัดไปเหลืออยู่หรือไม่
        {
            dungeonCleared = true;
            Debug.Log("[Dungeon] ผ่านดันเจี้ยนแล้ว!");
            OnDungeonCleared?.Invoke();
            return;
        }
        if (player != null) player.Heal(player.MaxHP * healOnRoomClear); // ฮีลเลือดผู้เล่นเมื่อผ่านห้อง 
        OnAnnounce?.Invoke("CHAMBER CLEARED", "The seal is broken - follow the lanterns");
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

}
