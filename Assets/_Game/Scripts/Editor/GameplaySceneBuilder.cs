using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เมนู BoatVsBoss > Update Gameplay Waves (เครื่องมือใน Editor ไม่ได้อยู่ในตัวเกม)
// ใส่รายการเวฟให้ Room ทั้ง 3 ห้องในซีน Gameplay — ปรับจำนวน/ชนิดศัตรูที่ ConfigureWaves() แล้วกดเมนูอีกครั้ง
// แก้แค่เวฟ ของอื่นในซีน (หน้าจบ / คัตซีนบอส / ระบบอัปเกรด) ไม่โดนแตะ
//
// ผังดันเจี้ยน (แกน z): ห้อง 1 Ember Vault (กลาง 0) | ห้อง 2 Warden Hall (กลาง 32) | ห้อง 3 ลานบอส (กลาง 66)
public static class GameplaySceneBuilder
{
    private const string TargetScene = "Assets/_Game/Scenes/Gameplay.unity";
    private const string EnemyFolder = "Assets/_Game/Data/Enemies/";

    private const float Room1Z = 0f;
    private const float Room2Z = 32f;
    private const float ArenaZ = 66f;

    // จัดเวฟ (เป้าหมาย: เล่นจบประมาณ 6-10 นาที)
    // ห้อง 1 = 4 เวฟ อุ่นเครื่อง / ห้อง 2 = 5 เวฟ มีโกเลมกลางห้องกับท้ายห้อง / ห้อง 3 = บอสออคตัวเดียว (เสกก็อบลินเอง)
    private static void ConfigureWaves(Room room1, Room room2, Room room3)
    {
        EnemyData gob = Data("Goblin"), skel = Data("Skeleton"), golem = Data("Golem");
        EnemyData green = Data("SlimeGreen"), blue = Data("SlimeBlue"), orc = Data("OrcBoss");

        // 1. ห้อง 1 (26 x 24) ผู้เล่นเริ่มที่ z = -6.5
        Vector3[] p1 = Points(Room1Z, new Vector2(-6.4f, 5.1f), new Vector2(6.4f, 5.1f), new Vector2(0f, 7.5f),
            new Vector2(-9f, -1f), new Vector2(9f, -1f), new Vector2(-4f, 9f), new Vector2(4f, 9f), new Vector2(-9.5f, 4.5f), new Vector2(9.5f, 4.5f),
            new Vector2(-6f, -6f), new Vector2(6f, -6f), new Vector2(0f, 1.5f), new Vector2(-9f, -8f), new Vector2(9f, -8f));
        room1.waves.Add(MakeWave(p1, 0f, gob, gob, gob));
        room1.waves.Add(MakeWave(p1, 0f, gob, green, gob, green, gob));
        room1.waves.Add(MakeWave(p1, 0f, skel, gob, gob, skel, gob));
        room1.waves.Add(MakeWave(p1, 0f, gob, green, skel, blue, gob, gob, green));

        // 2. ห้อง 2 (26 x 24) เข้าทางใต้
        Vector3[] p2 = Points(Room2Z, new Vector2(-6f, 4f), new Vector2(6f, 4f), new Vector2(0f, 6.5f),
            new Vector2(-8.5f, -1f), new Vector2(8.5f, -1f), new Vector2(-7f, -6f), new Vector2(7f, -6f), new Vector2(3f, 8.5f), new Vector2(-3f, 8.5f),
            new Vector2(0f, 0f), new Vector2(-4f, -8f), new Vector2(4f, -8f));
        Vector3[] golemPoints = Points(Room2Z, new Vector2(0f, 5f), new Vector2(-6f, 2f), new Vector2(6f, 2f), new Vector2(0f, 8f),
            new Vector2(-8f, 6f), new Vector2(8f, 6f), new Vector2(-4f, -3f), new Vector2(4f, -3f));
        room2.waves.Add(MakeWave(p2, 0f, skel, gob, skel, gob, gob, gob));
        room2.waves.Add(MakeWave(p2, 0f, blue, green, gob, blue, gob, green, gob));
        Wave midGolem = MakeWave(golemPoints, 0f, golem, gob, gob, gob);
        midGolem.telegraphTime = 1.4f; // ตัวใหญ่เกิดช้ากว่า ให้รู้ว่าโกเลมมา
        room2.waves.Add(midGolem);
        room2.waves.Add(MakeWave(p2, 0f, skel, gob, skel, blue, gob, skel, gob));
        Wave warden = MakeWave(golemPoints, 0f, golem, golem, skel, gob, skel, gob);
        warden.telegraphTime = 1.4f;
        room2.waves.Add(warden);

        // 3. ห้อง 3 มีแค่บอสออคตัวเดียว (ลูกน้องมาจากสกิลเสกก็อบลินของ OrcBoss แทน)
        Wave bossWave = MakeWave(Points(ArenaZ, new Vector2(0f, 4f)), 0f, orc);
        bossWave.telegraphTime = 2f;
        room3.waves.Add(bossWave);

        // 4. พักระหว่างเวฟนานขึ้นนิด ให้หายใจ/เก็บของ
        room1.timeBetweenWaves = 3f;
        room2.timeBetweenWaves = 3f;
    }

    // หา Room 1-3 ในซีน Gameplay -> ล้างเวฟเก่า -> ใส่เวฟใหม่ -> เซฟซีน
    [MenuItem("BoatVsBoss/Update Gameplay Waves")]
    public static void UpdateWaves()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("ออกจาก Play Mode ก่อน");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != TargetScene)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);
        }

        // 1. หา Room ทั้ง 3 ห้องตาม roomId
        Room[] found = Object.FindObjectsByType<Room>(FindObjectsInactive.Include);
        Room[] rooms = new Room[4];
        for (int i = 0; i < found.Length; i++)
        {
            if (found[i].roomId >= 1 && found[i].roomId <= 3) rooms[found[i].roomId] = found[i];
        }
        if (rooms[1] == null || rooms[2] == null || rooms[3] == null)
        {
            Debug.LogError("ไม่พบ Room 1-3 ในซีน Gameplay");
            return;
        }

        // 2. ล้างเวฟเก่า แล้วใส่เวฟใหม่
        for (int i = 1; i <= 3; i++)
        {
            Undo.RecordObject(rooms[i], "Update Gameplay Waves");
            rooms[i].waves.Clear();
        }
        ConfigureWaves(rooms[1], rooms[2], rooms[3]);
        for (int i = 1; i <= 3; i++) EditorUtility.SetDirty(rooms[i]);

        // 3. บันทึก
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameplaySceneBuilder] อัปเดตเวฟแล้ว: ห้อง 1 = " + rooms[1].waves.Count + " เวฟ, ห้อง 2 = "
            + rooms[2].waves.Count + " เวฟ, ห้องบอส = " + rooms[3].waves.Count + " เวฟ (บอสอย่างเดียว)");
    }

    private static Wave MakeWave(Vector3[] points, float bossTrigger, params EnemyData[] enemies)
    {
        Wave wave = new Wave();
        wave.enemiesToSpawn.AddRange(enemies);
        wave.spawnPoints.AddRange(points);
        wave.bossHpTrigger = bossTrigger;
        return wave;
    }

    // แปลง (x, z ห่างจากกลางห้อง) เป็นตำแหน่งในโลก
    private static Vector3[] Points(float roomZ, params Vector2[] offsets)
    {
        Vector3[] result = new Vector3[offsets.Length];
        for (int i = 0; i < offsets.Length; i++) result[i] = new Vector3(offsets[i].x, 0.12f, roomZ + offsets[i].y);
        return result;
    }

    private static EnemyData Data(string fileName)
    {
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + fileName + ".asset");
        if (data == null) Debug.LogError("ไม่พบ EnemyData: " + fileName);
        return data;
    }
}
