using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ศัตรูหนึ่งชุดในห้อง (Wave ตาม Diagram) — สั่ง EnemyManager ให้เกิดทีละตัว
// ก่อนเกิดทุกตัวจะมีวงสีม่วงบนพื้นเตือนก่อน ผู้เล่นจะได้รู้ว่าศัตรูจะโผล่ตรงไหน
[System.Serializable]
public class Wave
{
    public int waveNumber;
    [Tooltip("ศัตรูที่จะเกิดในเวฟนี้ เรียงตามลำดับ")]
    public List<EnemyData> enemiesToSpawn = new List<EnemyData>();
    [Tooltip("จุดเกิด (ตำแหน่งในโลก) วนใช้ทีละจุด")]
    public List<Vector3> spawnPoints = new List<Vector3>();
    [Tooltip("เกิดห่างกันกี่วินาที")]
    public float spawnInterval = 0.4f;
    [Tooltip("วงม่วงเตือนก่อนเกิดกี่วินาที")]
    public float telegraphTime = 1f;
    [Tooltip("ห้องบอส: 0 = เวฟปกติ / มากกว่า 0 = ลูกน้องบอส เกิดเมื่อเลือดบอสเหลือน้อยกว่าสัดส่วนนี้ (0.6 = 60%)")]
    [Range(0f, 1f)] public float bossHpTrigger;

    public static readonly Color SpawnColor = new Color(0.75f, 0.25f, 1f, 1f);
    private const float SafeDistance = 4f; // ไม่เกิดชิดผู้เล่นเกินนี้

    private bool hasStarted;
    private int pendingCount; // ตัวที่กำลังรอเกิด (วงม่วงยังไม่หมดเวลา)

    public bool HasStarted => hasStarted;
    public bool IsSpawning => pendingCount > 0;

    // เริ่มเวฟ: Room เป็นคนเรียก StartCoroutine ให้ (Wave ไม่ใช่ MonoBehaviour)
    public IEnumerator SpawnWave(Room room)
    {
        hasStarted = true;
        pendingCount = enemiesToSpawn.Count;
        Debug.Log("[Wave] " + room.roomName + " เวฟ " + waveNumber + " : ศัตรู " + enemiesToSpawn.Count + " ตัว");

        // 1. เรียงจุดเกิดจากใกล้ผู้เล่นไปไกล (ศัตรูโผล่ในจอ เห็นวงเตือนก่อน)
        List<Vector3> ordered = SortByDistance(room.Player);

        // 2. วนลูปเกิดทีละตัว เว้นระยะ spawnInterval
        for (int i = 0; i < enemiesToSpawn.Count; i++)
        {
            Vector3 point = ordered[i % ordered.Count];
            room.StartCoroutine(SpawnOne(room, enemiesToSpawn[i], point));
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    // ศัตรู 1 ตัว: วงม่วงขยายจนเต็ม -> เกิด
    private IEnumerator SpawnOne(Room room, EnemyData data, Vector3 point)
    {
        AttackIndicator indicator = null;
        if (room.IndicatorMaterial != null && data != null)
        {
            float radius = 0.7f * data.modelScale;
            indicator = AttackIndicator.Create(point, Vector3.forward, radius, 360f, telegraphTime,
                room.IndicatorMaterial, SpawnColor);
        }
        yield return new WaitForSeconds(telegraphTime);
        if (indicator != null) Object.Destroy(indicator.gameObject);

        if (data != null && room.Manager != null)
        {
            Enemy enemy = room.Manager.SpawnEnemy(data, point);
            room.HandleEnemySpawned(enemy);
        }
        pendingCount--;
    }

    // จุดเกิดที่ห่างผู้เล่นอย่างน้อย SafeDistance เรียงจากใกล้ไปไกล (Insertion Sort)
    // ไม่มีจุดไหนห่างพอ = ใช้ทุกจุด
    private List<Vector3> SortByDistance(Player player)
    {
        List<Vector3> result = new List<Vector3>();
        List<float> distances = new List<float>();
        Vector3 from = player != null ? player.transform.position : Vector3.zero;
        for (int k = 0; k < spawnPoints.Count; k++)
        {
            Vector3 diff = spawnPoints[k] - from;
            diff.y = 0f;
            float d = diff.magnitude;
            if (d < SafeDistance) continue;

            int index = distances.Count;
            while (index > 0 && distances[index - 1] > d) index--;
            distances.Insert(index, d);
            result.Insert(index, spawnPoints[k]);
        }
        if (result.Count == 0) result.AddRange(spawnPoints);
        if (result.Count == 0) result.Add(from + Vector3.forward * SafeDistance);
        return result;
    }

    // เวฟจบ = เกิดครบแล้ว และศัตรูในห้องตายหมด
    public bool IsCompleted(EnemyManager manager)
    {
        return hasStarted && pendingCount <= 0 && manager.GetAliveCount() == 0;
    }
}
