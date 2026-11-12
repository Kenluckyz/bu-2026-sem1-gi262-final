using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// เมนู BoatVsBoss > Build Gameplay Scene
// ก็อปซีน Test_Dungeon (ของเพื่อน) เป็นซีน Gameplay แล้วต่อเติมให้เล่นได้ตั้งแต่ต้นจนจบ:
// 1. ห้องที่ 3 = ลานบอสออค (ก็อปห้อง Ember Vault ไปต่อท้ายจุดพัก) + สะพาน + ประตู
// 2. ใช้ Player (DungeonPlayer.prefab) ที่อยู่ในซีนอยู่แล้ว + ใส่ HUD กับ Skill Bar เดิม (SceneBuilder.BuildHUD)
// 3. ใส่ GameManager / DungeonManager / Room / Wave / EnemyManager / GameplayHUD
// ไม่แก้ซีน Test_Dungeon เลย ทำงานกับไฟล์ Gameplay.unity อย่างเดียว
//
// ผังดันเจี้ยน (แกน z):
//  -12 ทางเข้า | ห้อง 1 Ember Vault (กลาง 0) | ประตู 12 | สะพาน | ประตู 20 | ห้อง 2 Warden Hall (กลาง 32)
//  | ประตู 44 | จุดพัก (47) | สะพาน 49-54 | ประตู 54 | ห้อง 3 ลานบอส (กลาง 66) | บัลลังก์ 78
public static class GameplaySceneBuilder
{
    private const string SourceScene = "Assets/_Game/Scenes/Test_Dungeon.unity";
    private const string TargetScene = "Assets/_Game/Scenes/Gameplay.unity";
    private const string IndicatorMaterial = "Assets/_Game/Materials/AttackIndicator.mat";
    private const string EnemyFolder = "Assets/_Game/Data/Enemies/";
    private const string DungeonMaterials = "Assets/_Game/TestDungeon/Materials/";

    private const float Room1Z = 0f;
    private const float Room2Z = 32f;
    private const float ArenaZ = 66f;          // กลางลานบอส = ก็อป Ember Vault เลื่อนไป 66 เมตร
    private const float SanctuaryEndZ = 49f;
    private const float ArenaGateZ = 54f;      // ประตูเข้าลานบอส = ก็อปประตู 44 เลื่อนไป 10 เมตร

    private static Transform root;
    private static List<Transform> groups = new List<Transform>();

    [MenuItem("BoatVsBoss/Build Gameplay Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("ออกจาก Play Mode ก่อน");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // 1. ก็อปซีน (มีอยู่แล้ว = สร้างใหม่ทับ)
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScene) != null)
        {
            if (!EditorUtility.DisplayDialog("Build Gameplay Scene", "มีซีน Gameplay อยู่แล้ว สร้างใหม่ทับเลยไหม?", "สร้างใหม่", "ยกเลิก")) return;
            AssetDatabase.DeleteAsset(TargetScene);
        }
        if (!AssetDatabase.CopyAsset(SourceScene, TargetScene))
        {
            Debug.LogError("ก็อป " + SourceScene + " ไม่สำเร็จ");
            return;
        }
        Scene scene = EditorSceneManager.OpenScene(TargetScene, OpenSceneMode.Single);

        // 2. หาของในซีนเพื่อน
        GameObject rootObj = FindRoot(scene, "TestDungeon");
        Player player = Object.FindAnyObjectByType<Player>();
        if (rootObj == null || player == null)
        {
            Debug.LogError("ไม่พบ TestDungeon หรือ Player ในซีน Test_Dungeon");
            return;
        }
        root = rootObj.transform;
        groups.Clear();
        string[] groupNames = { "01 Architecture", "02 Set Dressing", "03 Lanterns & Crystals", "05 Forgotten Treasury Props", "06 Abandoned Crypt Details" };
        for (int i = 0; i < groupNames.Length; i++)
        {
            Transform g = root.Find(groupNames[i]);
            if (g != null) groups.Add(g);
        }

        // 3. ลานบอส + สะพาน + ประตู
        BuildBossArena();

        // 4. EnemyManager (ซีนเพื่อนยังไม่มี)
        GameObject managerObj = new GameObject("EnemyManager");
        EnemyManager enemyManager = managerObj.AddComponent<EnemyManager>();
        SetField(enemyManager, "player", player);

        // 5. HUD + Skill Bar เดิมของ Boat (Player เป็น DungeonPlayer.prefab อยู่แล้ว กล้องใช้ CameraFollow ของเพื่อน)
        SceneBuilder.BuildHUD(player);

        // 6. ระบบเกม
        GameObject gameObj = new GameObject("Gameplay");
        GameManager game = gameObj.AddComponent<GameManager>();
        GameplayHUD hud = gameObj.AddComponent<GameplayHUD>();
        DungeonAmbience ambience = gameObj.AddComponent<DungeonAmbience>();
        CollectAmbience(ambience);

        GameObject dungeonObj = new GameObject("Dungeon");
        dungeonObj.transform.SetParent(gameObj.transform, false);
        DungeonManager dungeon = dungeonObj.AddComponent<DungeonManager>();

        Room room1 = CreateRoom(dungeonObj.transform, 1, "The Ember Vault", Room1Z, new Vector2(12f, 11f), false);
        SetDoor(room1, false, 12f);
        Room room2 = CreateRoom(dungeonObj.transform, 2, "The Warden's Hall", Room2Z, new Vector2(12f, 10f), false);
        SetDoor(room2, true, 20f);
        SetDoor(room2, false, 44f);
        Room room3 = CreateRoom(dungeonObj.transform, 3, "Throne of the Orc Warlord", ArenaZ, new Vector2(12f, 10f), true);
        SetDoor(room3, true, ArenaGateZ);
        ConfigureWaves(room1, room2, room3);

        Transform sanctuary = new GameObject("Sanctuary (heal)").transform;
        sanctuary.SetParent(dungeonObj.transform, false);
        sanctuary.position = new Vector3(0f, 0f, 47f);

        SetField(dungeon, "enemyManager", enemyManager);
        SetField(dungeon, "player", player);
        SetField(dungeon, "indicatorMaterial", AssetDatabase.LoadAssetAtPath<Material>(IndicatorMaterial));
        SetField(dungeon, "sanctuary", sanctuary);
        SerializedObject dso = new SerializedObject(dungeon);
        SerializedProperty roomList = dso.FindProperty("rooms");
        Room[] rooms = { room1, room2, room3 };
        roomList.arraySize = rooms.Length;
        for (int i = 0; i < rooms.Length; i++) roomList.GetArrayElementAtIndex(i).objectReferenceValue = rooms[i];
        dso.ApplyModifiedPropertiesWithoutUndo();

        SetField(game, "player", player);
        SetField(game, "dungeon", dungeon);
        SetField(game, "enemyManager", enemyManager);
        SetField(game, "hud", hud);

        // 7. บันทึก + ใส่ใน Build Settings
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings(TargetScene);
        Selection.activeGameObject = gameObj;
        Debug.Log("[GameplaySceneBuilder] สร้างซีน Gameplay เสร็จ: 3 ห้อง (เวฟ 3 + เวฟ 3 + บอสออค) กด Play ได้เลย");
    }

    #region Rooms & Waves
    private static Room CreateRoom(Transform parent, int id, string roomName, float z, Vector2 halfSize, bool boss)
    {
        GameObject obj = new GameObject("Room " + id + " - " + roomName);
        obj.transform.SetParent(parent, false);
        obj.transform.position = new Vector3(0f, 0f, z);
        Room room = obj.AddComponent<Room>();
        room.roomId = id;
        room.roomName = roomName;
        room.enterHalfSize = halfSize;
        room.isBossRoom = boss;
        return room;
    }

    // ประตูที่ z: Portcullis z / Gate collision z / ไฟ Gate seal light ใกล้ ๆ z
    private static void SetDoor(Room room, bool entry, float z)
    {
        string key = Mathf.RoundToInt(z).ToString();
        Transform gate = root.Find("Portcullis " + key);
        Transform blockerT = root.Find("Gate collision " + key);
        Collider blocker = blockerT != null ? blockerT.GetComponent<Collider>() : null;
        Light seal = null;
        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            Vector3 p = lights[i].transform.position;
            if (lights[i].name == "Gate seal light" && Mathf.Abs(p.z - z) < 1f && Mathf.Abs(p.x) < 0.5f) seal = lights[i];
        }
        if (gate == null || blocker == null) Debug.LogWarning("ไม่พบประตูที่ z = " + key);

        if (entry)
        {
            room.entryGate = gate; room.entryBlocker = blocker; room.entrySeal = seal;
        }
        else
        {
            room.exitGate = gate; room.exitBlocker = blocker; room.exitSeal = seal;
        }
    }

    // จัดเวฟ: ห้อง 1 อุ่นเครื่อง / ห้อง 2 หนักขึ้น ปิดท้ายด้วยโกเลม / ห้อง 3 บอสออค + ลูกน้องตอนเลือดลด
    private static void ConfigureWaves(Room room1, Room room2, Room room3)
    {
        EnemyData gob = Data("Goblin"), skel = Data("Skeleton"), golem = Data("Golem");
        EnemyData green = Data("SlimeGreen"), blue = Data("SlimeBlue"), orc = Data("OrcBoss");

        // ห้อง 1 (26 x 24) ผู้เล่นเริ่มที่ z = -6.5 -> ศัตรูเกิดฝั่งไกล/ด้านข้าง
        Vector3[] p1 = Points(Room1Z, new Vector2(-6.4f, 5.1f), new Vector2(6.4f, 5.1f), new Vector2(0f, 7.5f),
            new Vector2(-9f, -1f), new Vector2(9f, -1f), new Vector2(-4f, 9f), new Vector2(4f, 9f), new Vector2(-9.5f, 4.5f), new Vector2(9.5f, 4.5f),
            new Vector2(-6f, -6f), new Vector2(6f, -6f), new Vector2(0f, 1.5f), new Vector2(-9f, -8f), new Vector2(9f, -8f));
        room1.waves.Add(MakeWave(p1, 0f, gob, gob, gob));
        room1.waves.Add(MakeWave(p1, 0f, gob, green, gob, green));
        room1.waves.Add(MakeWave(p1, 0f, skel, gob, gob, blue, gob));

        // ห้อง 2 (26 x 24) เข้าทางใต้ -> ศัตรูเกิดฝั่งเหนือ/ข้าง
        Vector3[] p2 = Points(Room2Z, new Vector2(-6f, 4f), new Vector2(6f, 4f), new Vector2(0f, 6.5f),
            new Vector2(-8.5f, -1f), new Vector2(8.5f, -1f), new Vector2(-7f, -6f), new Vector2(7f, -6f), new Vector2(3f, 8.5f), new Vector2(-3f, 8.5f),
            new Vector2(0f, 0f), new Vector2(-4f, -8f), new Vector2(4f, -8f));
        room2.waves.Add(MakeWave(p2, 0f, skel, gob, skel, gob, gob));
        room2.waves.Add(MakeWave(p2, 0f, blue, green, gob, blue, gob, skel));
        Wave warden = MakeWave(Points(Room2Z, new Vector2(0f, 5f), new Vector2(-6f, 2f), new Vector2(6f, 2f), new Vector2(0f, 8f)),
            0f, golem, gob, gob, skel);
        warden.telegraphTime = 1.4f; // โกเลมเกิดช้ากว่า ให้รู้ว่าตัวใหญ่มา
        room2.waves.Add(warden);

        // ห้อง 3 บอสยืนหน้าบัลลังก์
        Wave bossWave = MakeWave(Points(ArenaZ, new Vector2(0f, 4f)), 0f, orc);
        bossWave.telegraphTime = 2f;
        room3.waves.Add(bossWave);
        Vector3[] adds = Points(ArenaZ, new Vector2(-9f, 0f), new Vector2(9f, 0f), new Vector2(-8f, 6f),
            new Vector2(8f, 6f), new Vector2(-6f, -6f), new Vector2(6f, -6f));
        room3.waves.Add(MakeWave(adds, 0.6f, gob, gob, skel));
        room3.waves.Add(MakeWave(adds, 0.3f, gob, blue, gob, skel));
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
    #endregion

    #region Boss Arena
    private static void BuildBossArena()
    {
        Transform arena = new GameObject("07 Boss Arena (Gameplay)").transform;
        arena.SetParent(root, false);
        Transform architecture = root.Find("01 Architecture");

        // 1. ก็อปห้อง Ember Vault ทั้งห้อง (กำแพง/พื้น/เสา/คบเพลิง/ของแต่ง) ไปไว้ที่ z = 66
        Vector3 shift = new Vector3(0f, 0f, ArenaZ);
        Transform vault = architecture.Find("Ember Vault");
        Transform copy = Object.Instantiate(vault, arena);
        copy.name = "Orc Throne walls";
        copy.position = vault.position + shift;
        for (int i = 0; i < groups.Count; i++)
        {
            CopyArea(groups[i], arena, shift, 14.5f, -11.5f, 11.1f);
        }

        // 2. ประตูลานบอส: ก็อปประตู z = 44 (ซุ้ม/ซี่กรง/กล่องชน/ไฟ/คบเพลิง/ผนังเสริม) มาไว้ที่ z = 54
        Vector3 gateShift = new Vector3(0f, 0f, ArenaGateZ - 44f);
        CopyNamed(architecture, "Sealed arch 44", arena, gateShift);
        CopyNamed(root, "Portcullis 44", root, gateShift).name = "Portcullis " + ArenaGateZ;
        CopyNamed(root, "Gate collision 44", root, gateShift).name = "Gate collision " + ArenaGateZ;
        for (int i = 0; i < groups.Count; i++)
        {
            CopyArea(groups[i], arena, gateShift, 5.5f, 42.9f, 44.6f);
        }

        // 3. ทางตันของจุดพัก (Sanctuary end) -> เปิดเป็นสะพานไปลานบอส
        Material dark = Mat("Obsidian cliff"), stone = Mat("Basalt masonry"), cap = Mat("Worn edge stone");
        Material brass = Mat("Old gold"), floor = Mat("Flagstone 3");
        Transform sanctuaryEnd = architecture.Find("Sanctuary end");
        if (sanctuaryEnd != null) Object.DestroyImmediate(sanctuaryEnd.gameObject);
        float length = ArenaGateZ - SanctuaryEndZ;
        float mid = SanctuaryEndZ + length / 2f;
        Box("Throne bridge foundation", arena, new Vector3(0f, -0.65f, mid), new Vector3(4.4f, 1.2f, length), dark, true);
        Box("Throne bridge walkway", arena, new Vector3(0f, -0.12f, mid), new Vector3(4f, 0.22f, length + 0.4f), floor, true);
        foreach (float x in new[] { -2.75f, 2.75f })
        {
            Box("Sanctuary corner", arena, new Vector3(x, 0.6f, SanctuaryEndZ), new Vector3(1.1f, 1.2f, 0.6f), stone, true);
        }
        for (float z = SanctuaryEndZ + 0.5f; z < ArenaGateZ - 0.4f; z += 1f)
        {
            foreach (float x in new[] { -2.55f, 2.55f })
            {
                Box("Bridge parapet", arena, new Vector3(x, 0.55f, z), new Vector3(0.7f, 1.1f, 0.94f), stone, true);
                Box("Bridge coping", arena, new Vector3(x, 1.16f, z), new Vector3(0.84f, 0.16f, 1.02f), cap, false);
            }
        }

        // 4. ปลายลานบอส: ผนังปิดช่องทางเข้าเดิม + บัลลังก์ของ Orc Warlord
        float back = ArenaZ + 12f;
        Box("Throne back wall", arena, new Vector3(0f, 1.3f, back + 0.3f), new Vector3(8f, 2.6f, 0.9f), dark, true);
        Box("Throne dais", arena, new Vector3(0f, 0.2f, back - 2.2f), new Vector3(4f, 0.4f, 2.2f), cap, true);
        Box("Throne seat", arena, new Vector3(0f, 0.75f, back - 2f), new Vector3(1.8f, 0.7f, 1.2f), stone, true);
        Box("Throne back", arena, new Vector3(0f, 2f, back - 1.5f), new Vector3(2.2f, 2.8f, 0.4f), stone, true);
        Box("Throne gold crest", arena, new Vector3(0f, 3.5f, back - 1.5f), new Vector3(1.4f, 0.28f, 0.45f), brass, false);
        Point(arena, "Warlord ember light", new Vector3(0f, 4.5f, ArenaZ + 5f), new Color(1f, 0.28f, 0.12f), 7f, 18f);
        Point(arena, "Arena dread fill", new Vector3(0f, 6f, ArenaZ - 3f), new Color(0.55f, 0.18f, 0.2f), 5f, 18f);
    }

    // ก็อปของในกลุ่มที่อยู่ในพื้นที่ |x| <= maxX และ minZ <= z <= maxZ (ข้ามกลุ่มย่อยที่ตำแหน่งไม่ตรงกับของข้างใน)
    private static void CopyArea(Transform group, Transform parent, Vector3 shift, float maxX, float minZ, float maxZ)
    {
        int count = group.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = group.GetChild(i);
            string n = child.name;
            if (n == "Ember Vault" || n.StartsWith("Warden Hall") || n.StartsWith("Sealed arch")) continue;
            if (n.StartsWith("Sanctuary") || n.StartsWith("Bridge") || n == "Diamond path marker") continue;
            Vector3 p = child.position;
            if (Mathf.Abs(p.x) > maxX || p.z < minZ || p.z > maxZ) continue;

            Transform c = Object.Instantiate(child, parent);
            c.name = n;
            c.position = p + shift;
        }
    }

    private static Transform CopyNamed(Transform parent, string objName, Transform newParent, Vector3 shift)
    {
        Transform src = parent.Find(objName);
        if (src == null)
        {
            Debug.LogWarning("ไม่พบ " + objName);
            return new GameObject(objName + " (missing)").transform;
        }
        Transform c = Object.Instantiate(src, newParent);
        c.name = objName;
        c.position = src.position + shift;
        return c;
    }

    // ไฟคบเพลิงทั้งดันเจี้ยน (รวมที่ก็อปมา) ให้กะพริบ + วงเวทเรียกศัตรูหมุน
    private static void CollectAmbience(DungeonAmbience ambience)
    {
        List<Transform> flames = new List<Transform>();
        List<Light> lights = new List<Light>();
        List<Transform> markers = new List<Transform>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            string n = all[i].name;
            if (n == "Amber flame" || n == "White hot core") flames.Add(all[i]);
            if (n == "Warm lantern light" && all[i].GetComponent<Light>() != null) lights.Add(all[i].GetComponent<Light>());
            if (n == "Summoning seal") markers.Add(all[i]);
        }
        ambience.flames = flames.ToArray();
        ambience.torchLights = lights.ToArray();
        ambience.spinningMarkers = markers.ToArray();
    }

    private static GameObject Box(string objName, Transform parent, Vector3 position, Vector3 size, Material mat, bool collider)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = objName;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }

    private static Light Point(Transform parent, string objName, Vector3 position, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(objName, typeof(Light));
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        Light light = go.GetComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    private static Material Mat(string matName)
    {
        Material m = AssetDatabase.LoadAssetAtPath<Material>(DungeonMaterials + matName + ".mat");
        if (m == null) Debug.LogWarning("ไม่พบ Material: " + matName);
        return m;
    }
    #endregion

    #region Helpers
    private static GameObject FindRoot(Scene scene, string objName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objName) return roots[i];
        }
        return null;
    }

    private static void SetField(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError(target.GetType().Name + " ไม่มีช่อง " + field);
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AddToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].path == path) return;
        }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
    #endregion
}
