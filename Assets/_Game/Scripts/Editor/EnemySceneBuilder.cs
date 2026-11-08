using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// เมนู BoatVsBoss > Build Test_Enemy Scene : สร้างพื้น ผู้เล่น (Boat) กล้อง HUD และ EnemyManager ที่เกิดก็อบลินให้สู้ทันที
// เมนู BoatVsBoss > Build Goblin Prefab   : สร้างแค่ Prefab + EnemyData + Animator ของก็อบลิน
// เมนู BoatVsBoss > Build Skeleton Prefab : สร้างโครงกระดูกปาหิน (+ ลูกหิน)
// เมนู BoatVsBoss > Build Slime Prefabs   : สไลม์เขียว/ฟ้า (ตายแล้วแตกเป็นตัวเล็ก 2 ตัว)
// เมนู BoatVsBoss > Build Golem Prefab    : โกเลมสีเทา สกิลทุ่มพื้น + ปาหิน (มีคูลดาวน์)
// เมนู BoatVsBoss > Build Enemy Spawn Panel : แผงปุ่มเกิดศัตรูมุมขวาบน (กดรูป = เกิดตัวนั้น / Kill All / Auto Respawn)
// เมนู BoatVsBoss > Build Orc Boss Prefab : บอสออค 4 สกิล (กระโดดจาม / ตั้งโล่ / เหวี่ยงขวาน / สับพื้น) + พื้นแดงเตือน
public static class EnemySceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Enemy.unity";

    private const string GoblinModelPrefab = "Assets/Goblins -characters with animations/Goblins -characters with animations/models/prefab/goblin.prefab";
    private const string GoblinController = "Assets/_Game/Art/Enemies/Goblin/Goblin.controller";
    private const string GoblinPrefabPath = "Assets/_Game/Prefabs/Enemies/Goblin.prefab";
    private const string GoblinDataPath = "Assets/_Game/Data/Enemies/Goblin.asset";
    private const float GoblinHeight = 1.15f; // Boat สูง 1.5 ก็อบลินตัวเล็กกว่า

    private const string SkeletonFolder = "Assets/Skeleton/FBX/";                 // ของ Asset Store (โมเดล + IDLE/Walk/Run)
    private const string SkeletonAnimFolder = "Assets/_Game/Art/Enemies/Skeleton/"; // ท่าที่ทำเพิ่มใน Blender
    private const string SkeletonModelPrefab = "Assets/Skeleton/Prefab/Skeleton.prefab";
    private const string SkeletonController = "Assets/_Game/Art/Enemies/Skeleton/Skeleton.controller";
    private const string SkeletonPrefabPath = "Assets/_Game/Prefabs/Enemies/Skeleton.prefab";
    private const string SkeletonDataPath = "Assets/_Game/Data/Enemies/Skeleton.asset";
    private const string RockPrefabPath = "Assets/_Game/Prefabs/Enemies/Rock.prefab";
    private const float SkeletonHeight = 1.6f;

    private const string SlimeFbx = "Assets/Level 1 Monster Pack/Models/Slime_Level_1.fbx";
    private const string SlimeModelGreen = "Assets/Level 1 Monster Pack/Prefabs/Slime/Slime_Green.prefab";
    private const string SlimeModelBlue = "Assets/Level 1 Monster Pack/Prefabs/Slime/Slime_Blue.prefab";
    private const string SlimeController = "Assets/_Game/Art/Enemies/Slime/Slime.controller";
    private const float SlimeHeight = 0.8f;

    private const string GolemModelPrefab = "Assets/Kevin Iglesias/Characters/Humanoid Giant/Prefabs/URP/URP - Giant01 - Golem.prefab";
    private const string GolemController = "Assets/_Game/Art/Enemies/Golem/Golem.controller";
    private const string GolemPrefabPath = "Assets/_Game/Prefabs/Enemies/Golem.prefab";
    private const string GolemDataPath = "Assets/_Game/Data/Enemies/Golem.asset";
    private const string GolemGreyTexture = "Assets/_Game/Art/Enemies/Golem/Golem_Grey_BaseColor.png";
    private const string GolemNormalMap = "Assets/Kevin Iglesias/Characters/Humanoid Giant/Textures/Giant01/Texture01/Giant01_Texture01-Normal.png";
    private const string GolemRockModel = "Assets/Kevin Iglesias/Characters/Humanoid Giant/Demo Scene/Prefabs/URP/URP - ThrowingRockExample.prefab";
    private const string GolemRockPrefabPath = "Assets/_Game/Prefabs/Enemies/GolemRock.prefab";
    private const float GolemHeight = 2.4f;   // ตัวใหญ่ สูงกว่า Boat (1.5) เกือบเท่าตัว

    [MenuItem("BoatVsBoss/Build Test_Enemy Scene")]
    public static void BuildTestEnemyScene()
    {
        // OpenScene แบบ Single จะทิ้งของที่ยังไม่เซฟในซีนปัจจุบัน — ถามก่อน
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnemyData goblin = BuildGoblin();
        EnemyData skeleton = BuildSkeleton();
        EnemyData[] slimes = BuildSlimes();
        EnemyData golem = BuildGolem();

        foreach (string n in new[] { "Ground", "Player", "EnemyManager" }) DestroyIfExists(n);

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);

        GameObject player = BuildPlayer();
        player.transform.position = new Vector3(0f, 0f, -3f);
        Player playerScript = player.GetComponent<Player>();

        // กล้อง + Bloom + HUD ใช้ของ SceneBuilder (Test_Player) ให้หน้าตาเหมือนกัน
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        var camSo = new SerializedObject(follow);
        camSo.FindProperty("target").objectReferenceValue = player.transform;
        camSo.ApplyModifiedPropertiesWithoutUndo();
        SceneBuilder.BuildBloom(cam);
        SceneBuilder.BuildHUD(playerScript);

        // EnemyManager + จุดเกิด 4 จุดรอบสนาม + ตัวเกิดศัตรูสำหรับทดสอบ
        GameObject managerObj = new GameObject("EnemyManager");
        managerObj.transform.position = new Vector3(0f, 0f, 4f);
        EnemyManager manager = managerObj.AddComponent<EnemyManager>();
        Vector3[] points = { new Vector3(-6f, 0f, 6f), new Vector3(6f, 0f, 6f), new Vector3(0f, 0f, 9f), new Vector3(-8f, 0f, 0f), new Vector3(8f, 0f, 0f) };
        var mso = new SerializedObject(manager);
        mso.FindProperty("player").objectReferenceValue = playerScript;
        SerializedProperty sp = mso.FindProperty("spawnPoints");
        sp.arraySize = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            GameObject p = new GameObject("SpawnPoint_" + i);
            p.transform.SetParent(managerObj.transform, false);
            p.transform.position = points[i];
            sp.GetArrayElementAtIndex(i).objectReferenceValue = p.transform;
        }
        mso.ApplyModifiedPropertiesWithoutUndo();

        EnemyTestSpawner spawner = managerObj.AddComponent<EnemyTestSpawner>();
        var sso = new SerializedObject(spawner);
        sso.FindProperty("enemyManager").objectReferenceValue = manager;
        SerializedProperty list = sso.FindProperty("enemies");
        // ศัตรูที่สุ่มเกิด: ก็อบลิน / โครงกระดูก / สไลม์เขียว / สไลม์ฟ้า / โกเลม
        EnemyData[] testEnemies = { goblin, skeleton, slimes[0], slimes[1], golem };
        list.arraySize = testEnemies.Length;
        for (int i = 0; i < testEnemies.Length; i++)
        {
            list.GetArrayElementAtIndex(i).objectReferenceValue = testEnemies[i];
        }
        sso.FindProperty("startCount").intValue = 4;
        sso.ApplyModifiedPropertiesWithoutUndo();

        // แผงปุ่มเกิดศัตรูมุมขวาบน (ทดสอบ)
        BuildSpawnPanel();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Test_Enemy scene built — กด Play: WASD เดิน, คลิกซ้ายต่อย, 1/2/3 สกิล, G เกิดศัตรูเพิ่ม");
    }

    #region Goblin
    [MenuItem("BoatVsBoss/Build Goblin Prefab")]
    public static EnemyData BuildGoblin()
    {
        EnsureFolder("Assets/_Game/Art/Enemies/Goblin");
        EnsureFolder("Assets/_Game/Prefabs/Enemies");
        EnsureFolder("Assets/_Game/Data/Enemies");

        // 1. ท่าทาง: ทุกท่าอยู่ใน goblin.fbx ไฟล์เดียว
        GoblinImportSettings.EnsureImported(); // idle/run วนลูป
        AnimatorController controller = EnemyAnimatorBuilder.Build(GoblinController, GoblinImportSettings.FbxPath,
            "idle", "run", new[] { "attack_1", "attack_2" }, "hit", "dead");

        // 2. ค่าพลัง (มีแล้วไม่ทับ เผื่อปรับค่าใน Inspector ไว้)
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(GoblinDataPath);
        if (data == null)
        {
            // ก็อบลิน: เลือดน้อย วิ่งไว ตีไว ดาเมจเบา
            data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Goblin";
            data.maxHP = 40f;
            data.damage = 8f;
            data.expDrop = 10;
            data.modelScale = 1f;
            data.moveSpeed = 4.2f;
            data.detectRange = 14f;
            data.attackRange = 1.3f;
            data.attackCooldown = 0.6f;
            data.hitPoint = 0.45f;
            data.attackAnimSpeed = 1.3f;
            data.attackAnims = new[] { "Attack1", "Attack2" };
            data.canBeStunned = true;
            data.stunTime = 0.35f;
            data.corpseTime = 3f;
            AssetDatabase.CreateAsset(data, GoblinDataPath);
        }

        // 3. ตัวศัตรู + โมเดล + แถบเลือด แล้วเซฟเป็น Prefab
        GameObject root = CreateEnemyObject<Enemy>("Goblin", GoblinModelPrefab, controller, GoblinHeight,
            "thigh.L", "thigh.R", null, out Enemy enemy, out Animator anim);
        SaveEnemyPrefab(root, enemy, anim, data, GoblinPrefabPath);
        return data;
    }
    #endregion

    #region Skeleton
    [MenuItem("BoatVsBoss/Build Skeleton Prefab")]
    public static EnemyData BuildSkeleton()
    {
        EnsureFolder("Assets/_Game/Prefabs/Enemies");
        EnsureFolder("Assets/_Game/Data/Enemies");
        EnsureFolder("Assets/_Game/Materials");

        // 1. ท่าทาง: ท่าเดิมจาก Asset Store (IDLE/Walk) + ท่าที่ทำใน Blender (Throw/Death/Hit)
        SkeletonAnimImportSettings.EnsureImported(); // ลบคีย์ที่ทำให้ตัวขยายตอนเล่นท่า
        AnimatorController controller = EnemyAnimatorBuilder.Build(SkeletonController,
            EnemyAnimatorBuilder.LoadFirstClip(SkeletonFolder + "IDLE.fbx"),
            EnemyAnimatorBuilder.LoadFirstClip(SkeletonFolder + "Walk.fbx"),
            new[] { EnemyAnimatorBuilder.LoadFirstClip(SkeletonAnimFolder + "Skeleton_Throw.fbx") },
            EnemyAnimatorBuilder.LoadFirstClip(SkeletonAnimFolder + "Skeleton_Hit.fbx"),
            EnemyAnimatorBuilder.LoadFirstClip(SkeletonAnimFolder + "Skeleton_Death.fbx"));

        // 2. ค่าพลัง (มีแล้วไม่ทับ)
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(SkeletonDataPath);
        if (data == null)
        {
            // โครงกระดูก: เลือดน้อย เดินช้า ยืนปาหินจากระยะไกล
            data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Skeleton";
            data.maxHP = 30f;
            data.damage = 7f;
            data.expDrop = 15;
            data.modelScale = 1f;
            data.moveSpeed = 2.6f;
            data.detectRange = 16f;
            data.attackRange = 9f;         // ปาได้ไกล 9 เมตร
            data.attackCooldown = 2f;
            data.hitPoint = 0.45f;         // มือปล่อยหินที่เฟรม 15/33 ของท่า Throw
            data.attackAnimSpeed = 1f;
            data.attackAnims = new[] { "Attack1" };
            data.canBeStunned = true;
            data.stunTime = 0.4f;
            data.corpseTime = 3f;
            AssetDatabase.CreateAsset(data, SkeletonDataPath);
        }

        // 3. ลูกหิน
        Rock rock = BuildRockPrefab();

        // 4. ตัวศัตรู (SkeletonEnemy) + โมเดล + วัสดุ URP
        Material mat = GetOrCreateSkeletonMaterial();
        GameObject root = CreateEnemyObject<SkeletonEnemy>("Skeleton", SkeletonModelPrefab, controller, SkeletonHeight,
            "Hip_L", "Hip_R", mat, out SkeletonEnemy skeleton, out Animator anim);
        skeleton.rockPrefab = rock;
        skeleton.throwHand = FindDeep(root.transform, "Wrist_R");
        SaveEnemyPrefab(root, skeleton, anim, data, SkeletonPrefabPath);
        return data;
    }

    // ลูกหิน = ทรงกลมสีเทาน้ำตาล ไม่มี collider (Rock.cs เช็กระยะโดนเอง)
    private static Rock BuildRockPrefab()
    {
        GameObject rockObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rockObj.name = "Rock";
        Object.DestroyImmediate(rockObj.GetComponent<SphereCollider>());
        rockObj.transform.localScale = new Vector3(0.28f, 0.22f, 0.25f); // บี้นิด ๆ ให้ดูเป็นก้อนหิน

        const string matPath = "Assets/_Game/Materials/Rock.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", new Color(0.45f, 0.42f, 0.38f));
            mat.SetFloat("_Smoothness", 0.1f);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        rockObj.GetComponent<Renderer>().sharedMaterial = mat;
        rockObj.AddComponent<Rock>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rockObj, RockPrefabPath);
        Object.DestroyImmediate(rockObj);
        return prefab.GetComponent<Rock>();
    }

    // วัสดุของ Asset Store ใช้ shader Standard (ขึ้นสีชมพูใน URP) — ทำวัสดุ URP ใหม่จากรูปเดิม ไม่แก้ไฟล์ของ Asset Store
    private static Material GetOrCreateSkeletonMaterial()
    {
        const string texPath = "Assets/Skeleton/Textures/SceletonVersion1.tga";
        const string matPath = "Assets/_Game/Materials/Skeleton.mat";
        TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (ti != null && ti.maxTextureSize > 2048)
        {
            ti.maxTextureSize = 2048; // รูป 4K หนักเกินไป
            ti.SaveAndReimport();
        }
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.2f);
        EditorUtility.SetDirty(mat);
        return mat;
    }
    #endregion

    #region Slime
    [MenuItem("BoatVsBoss/Build Slime Prefabs")]
    public static EnemyData[] BuildSlimes()
    {
        EnsureFolder("Assets/_Game/Art/Enemies/Slime");
        EnsureFolder("Assets/_Game/Prefabs/Enemies");
        EnsureFolder("Assets/_Game/Data/Enemies");
        EnsureFolder("Assets/_Game/Materials");

        // 1. ท่าทาง: ทุกท่าอยู่ใน Slime_Level_1.fbx (ไม่มีท่าโดนตี ใช้ท่ายืนแทน — กะพริบแดงอย่างเดียว)
        AnimatorController controller = EnemyAnimatorBuilder.Build(SlimeController, SlimeFbx,
            "slime_idle", "slime_move", new[] { "slime_attack" }, "slime_idle", "slime_die");

        // 2. สีเขียว: สมดุล / สีฟ้า: เลือดน้อยกว่าแต่เร็วกว่า — ตัวเล็กตายแล้วไม่แตกต่อ
        EnemyData greenSmall = GetOrCreateSlimeData("SlimeGreenSmall", "Green Slime (Small)", 12f, 3f, 4, 1.1f, 3.6f, 1.3f, null);
        EnemyData green = GetOrCreateSlimeData("SlimeGreen", "Green Slime", 30f, 6f, 8, 1.5f, 3.0f, 1.6f, greenSmall);
        EnemyData blueSmall = GetOrCreateSlimeData("SlimeBlueSmall", "Blue Slime (Small)", 10f, 3f, 4, 1.1f, 4.0f, 1.3f, null);
        EnemyData blue = GetOrCreateSlimeData("SlimeBlue", "Blue Slime", 24f, 5f, 8, 1.5f, 3.4f, 1.6f, blueSmall);

        // 3. Prefab สีละ 1 อัน — ตัวใหญ่ (modelScale 1.5) / ตัวเล็ก (1.1) ใช้ Prefab เดียวกัน ต่างกันที่ modelScale ใน EnemyData
        BuildSlimePrefab("SlimeGreen", SlimeModelGreen, "Assets/Level 1 Monster Pack/Images/Slime/SlimeGreen.jpg", controller, green, greenSmall);
        BuildSlimePrefab("SlimeBlue", SlimeModelBlue, "Assets/Level 1 Monster Pack/Images/Slime/SlimeBlue.jpg", controller, blue, blueSmall);
        return new[] { green, blue };
    }

    private static EnemyData GetOrCreateSlimeData(string file, string enemyName, float hp, float damage, int exp,
        float scale, float speed, float range, EnemyData splitInto)
    {
        string path = "Assets/_Game/Data/Enemies/" + file + ".asset";
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (data != null) return data; // มีแล้วไม่ทับ เผื่อปรับค่าใน Inspector ไว้

        data = ScriptableObject.CreateInstance<EnemyData>();
        data.enemyName = enemyName;
        data.maxHP = hp;
        data.damage = damage;
        data.expDrop = exp;
        data.modelScale = scale;
        data.moveSpeed = speed;
        data.detectRange = 12f;
        data.attackRange = range;
        data.attackCooldown = 0.9f;
        data.hitPoint = 0.5f;
        data.attackAnimSpeed = 1f;
        data.attackAnims = new[] { "Attack1" };
        data.canBeStunned = true;
        data.stunTime = 0.3f;
        data.corpseTime = 1.2f;
        data.splitInto = splitInto;
        data.splitCount = 2;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    private static void BuildSlimePrefab(string prefabName, string modelPath, string texPath, AnimatorController controller,
        EnemyData bigData, EnemyData smallData)
    {
        Material mat = GetOrCreateUrpMaterial("Assets/_Game/Materials/" + prefabName + ".mat", texPath, null, 1024);
        // สไลม์ไม่มีขา ใช้กระดูกหางซ้าย/ขวาหาทิศหน้าแทน
        GameObject root = CreateEnemyObject<SlimeEnemy>(prefabName, modelPath, controller, SlimeHeight,
            "SlimeLeftTailJoint", "SlimeRightJoint", mat, out SlimeEnemy slime, out Animator anim);
        root.GetComponent<CharacterController>().radius = 0.4f;
        slime.splitSpread = 1.4f; // ลูกกระเด็นออกไกลพอ ไม่เกิดซ้อนกัน
        string prefabPath = "Assets/_Game/Prefabs/Enemies/" + prefabName + ".prefab";
        SaveEnemyPrefab(root, slime, anim, bigData, prefabPath);
        // ตัวเล็กใช้ Prefab เดียวกัน
        smallData.prefab = bigData.prefab;
        EditorUtility.SetDirty(smallData);
        AssetDatabase.SaveAssets();
    }
    #endregion

    #region Golem
    [MenuItem("BoatVsBoss/Build Golem Prefab")]
    public static EnemyData BuildGolem()
    {
        EnsureFolder("Assets/_Game/Prefabs/Enemies");
        EnsureFolder("Assets/_Game/Data/Enemies");
        EnsureFolder("Assets/_Game/Materials");

        // 1. ท่าทาง (ทำใน Blender ทั้งหมด) — Attack1 = ทุ่มพื้น, Attack2 = ปาหิน
        GolemAnimImportSettings.EnsureImported();
        string f = GolemAnimImportSettings.Folder;
        AnimatorController controller = EnemyAnimatorBuilder.Build(GolemController,
            EnemyAnimatorBuilder.LoadFirstClip(f + "Golem_Idle.fbx"),
            EnemyAnimatorBuilder.LoadFirstClip(f + "Golem_Walk.fbx"),
            new[] { EnemyAnimatorBuilder.LoadFirstClip(f + "Golem_Slam.fbx"), EnemyAnimatorBuilder.LoadFirstClip(f + "Golem_Throw.fbx") },
            EnemyAnimatorBuilder.LoadFirstClip(f + "Golem_Hit.fbx"),
            EnemyAnimatorBuilder.LoadFirstClip(f + "Golem_Death.fbx"));

        // 2. ค่าพลัง (มีแล้วไม่ทับ)
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(GolemDataPath);
        if (data == null)
        {
            // โกเลม: เลือดเยอะมาก เดินช้า ตีแรง โดนตีไม่ชะงัก
            data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Golem";
            data.maxHP = 160f;
            data.damage = 18f;             // ดาเมจท่าทุ่มพื้น
            data.expDrop = 40;
            data.modelScale = 1f;
            data.moveSpeed = 1.6f;
            data.detectRange = 16f;
            data.attackRange = 2.6f;       // เข้าใกล้ขนาดนี้แล้วทุ่มพื้น
            data.attackCooldown = 1.8f;
            data.hitPoint = 0.53f;         // กำปั้นกระทบพื้นเฟรม 24/45
            data.attackAnimSpeed = 1f;
            data.attackAnims = new[] { "Attack1" };   // ตีปกติ = ทุ่มพื้น (ปาหินสั่งแยกใน GolemEnemy)
            data.canBeStunned = false;
            data.stunTime = 0f;
            data.corpseTime = 4f;
            AssetDatabase.CreateAsset(data, GolemDataPath);
        }

        // 3. หินก้อนใหญ่ของโกเลม (โมเดลหินจาก Demo ของ Kevin Iglesias)
        Rock rock = BuildGolemRockPrefab();

        // 4. ตัวโกเลม สีเทา
        Material mat = GetOrCreateUrpMaterial("Assets/_Game/Materials/Golem_Grey.mat", GolemGreyTexture, GolemNormalMap, 2048);
        GameObject root = CreateEnemyObject<GolemEnemy>("Golem", GolemModelPrefab, controller, GolemHeight,
            "B-thigh.L", "B-thigh.R", mat, out GolemEnemy golem, out Animator anim);
        root.GetComponent<CharacterController>().radius = 0.8f;
        golem.rockPrefab = rock;
        golem.heldRockPrefab = rock.gameObject; // หินในมือใช้โมเดลเดียวกัน (ยังไม่ Launch จึงไม่บิน)
        SaveEnemyPrefab(root, golem, anim, data, GolemPrefabPath);
        return data;
    }

    private static Rock BuildGolemRockPrefab()
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(GolemRockModel);
        GameObject rockObj;
        if (asset != null)
        {
            rockObj = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            PrefabUtility.UnpackPrefabInstance(rockObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        else
        {
            rockObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }
        rockObj.name = "GolemRock";
        foreach (Collider c in rockObj.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        // ปรับขนาดให้กว้าง ~0.8 เมตร
        Bounds b = GetBounds(rockObj.transform);
        float size = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (size > 0.001f) rockObj.transform.localScale *= 0.8f / size;
        Rock rock = rockObj.AddComponent<Rock>();
        rock.hitRadius = 1.0f;
        rock.spinSpeed = 240f;

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(rockObj, GolemRockPrefabPath);
        Object.DestroyImmediate(rockObj);
        return prefab.GetComponent<Rock>();
    }
    #endregion

    #region Spawn Panel
    private const string IconFolder = "Assets/_Game/Art/UI/EnemyIcons";

    // แผงปุ่มเกิดศัตรูมุมขวาบน (ใช้ทดสอบ) — ถ่ายรูปไอคอนจาก Prefab ของศัตรูแต่ละตัว แล้ววางแผงในซีนที่เปิดอยู่
    [MenuItem("BoatVsBoss/Build Enemy Spawn Panel")]
    public static void BuildSpawnPanel()
    {
        EnsureFolder(IconFolder);
        string[] dataPaths =
        {
            GoblinDataPath, SkeletonDataPath,
            "Assets/_Game/Data/Enemies/SlimeGreen.asset", "Assets/_Game/Data/Enemies/SlimeBlue.asset",
            GolemDataPath, OrcDataPath
        };

        // 1. โหลด EnemyData + ถ่ายไอคอน (วนลูปทีละตัว)
        var list = new System.Collections.Generic.List<EnemyData>();
        var icons = new System.Collections.Generic.List<Sprite>();
        for (int i = 0; i < dataPaths.Length; i++)
        {
            EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(dataPaths[i]);
            if (data == null || data.prefab == null)
            {
                Debug.LogWarning("ข้าม (ยังไม่ได้สร้าง): " + dataPaths[i]);
                continue;
            }
            list.Add(data);
            icons.Add(RenderEnemyIcon(data.prefab, IconFolder + "/" + data.prefab.name + ".png"));
        }

        // 2. วางแผงในซีน (มีแล้วสร้างใหม่ทับ)
        DestroyIfExists("EnemySpawnPanel");
        GameObject panelObj = new GameObject("EnemySpawnPanel");
        EnemySpawnPanel panel = panelObj.AddComponent<EnemySpawnPanel>();
        var so = new SerializedObject(panel);
        so.FindProperty("enemyManager").objectReferenceValue = Object.FindAnyObjectByType<EnemyManager>();
        so.FindProperty("testSpawner").objectReferenceValue = Object.FindAnyObjectByType<EnemyTestSpawner>();
        SerializedProperty entries = so.FindProperty("entries");
        entries.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++)
        {
            SerializedProperty e = entries.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("data").objectReferenceValue = list[i];
            e.FindPropertyRelative("icon").objectReferenceValue = icons[i];
        }
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(panelObj.scene);
        EditorSceneManager.SaveScene(panelObj.scene);
        Debug.Log("Enemy Spawn Panel built: " + list.Count + " ตัว (กด Play แล้วดูมุมขวาบน)");
    }

    // ถ่ายรูปศัตรูมุม 3/4 ด้านหน้า เป็นไฟล์ PNG แล้วตั้งเป็น Sprite
    private static Sprite RenderEnemyIcon(Enemy prefab, string pngPath)
    {
        const int size = 256;

        // 1. วางศัตรูไว้ไกล ๆ (ไม่ให้ติดพื้น/ตัวอื่นในรูป) ปิดแถบเลือด แล้วเล่นท่ายืน 1 เฟรม (ไม่งั้นติดท่า T)
        GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject);
        obj.transform.position = new Vector3(500f, 0f, 500f);
        Transform bar = FindDeep(obj.transform, "HealthBar");
        if (bar != null) bar.gameObject.SetActive(false);
        Animator anim = obj.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Rebind();
            anim.Update(0.2f);
        }
        Bounds b = GetBounds(obj.transform);

        // 2. กล้องชั่วคราว มองจากหน้าเฉียงขวา ระยะพอดีตัว
        GameObject camObj = new GameObject("IconCamera");
        Camera cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.16f, 0.2f, 1f);
        cam.fieldOfView = 30f;
        cam.nearClipPlane = 0.05f;
        cam.farClipPlane = 200f;
        Vector3 dir = new Vector3(0.45f, 0.3f, 1f).normalized;
        float distance = b.extents.magnitude / Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
        camObj.transform.position = b.center + dir * distance;
        camObj.transform.LookAt(b.center);

        // 3. ถ่ายลง RenderTexture แล้วอ่านเป็นรูป
        RenderTexture rt = new RenderTexture(size, size, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        cam.targetTexture = null;
        System.IO.File.WriteAllBytes(pngPath, tex.EncodeToPNG());

        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(camObj);
        Object.DestroyImmediate(obj);

        // 4. import เป็น Sprite
        AssetDatabase.ImportAsset(pngPath, ImportAssetOptions.ForceUpdate);
        TextureImporter ti = AssetImporter.GetAtPath(pngPath) as TextureImporter;
        if (ti != null)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(pngPath);
    }
    #endregion

    #region Orc Boss
    private const string OrcModelPrefab = "Assets/PolyNext/Low Poly Orc Warrior/Prefabs/Orc_Warrior_Combined2.prefab"; // ตัวเขาเหล็ก + ขวาน + โล่หนาม
    private const string OrcTexture = "Assets/PolyNext/Low Poly Orc Warrior/Textures/Orc_Diffuse.png";
    private const string OrcController = "Assets/_Game/Art/Enemies/Orc/Orc.controller";
    private const string OrcPrefabPath = "Assets/_Game/Prefabs/Enemies/OrcBoss.prefab";
    private const string OrcDataPath = "Assets/_Game/Data/Enemies/OrcBoss.asset";
    private const string IndicatorMaterialPath = "Assets/_Game/Materials/AttackIndicator.mat";
    private const float OrcHeight = 2.4f;

    [MenuItem("BoatVsBoss/Build Orc Boss Prefab")]
    public static EnemyData BuildOrc()
    {
        EnsureFolder("Assets/_Game/Art/Enemies/Orc");
        EnsureFolder("Assets/_Game/Materials");

        // 1. ท่าทาง: ของแพ็ก (ยืน เดิน สับพื้น เหวี่ยงขวาน ตั้งโล่ โดนตี ตาย) + ทำเพิ่มใน Blender (กระโดดจาม)
        OrcImportSettings.EnsureImported();
        AnimationClip slam = EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Slam);
        AnimationClip sweep = EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Sweep);
        AnimationClip jump = EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Jump);
        AnimationClip guard = EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Guard); // ท่ายืนตั้งการ์ดของแพ็ก (ยกโล่บังหน้า)
        AnimatorController controller = EnemyAnimatorBuilder.Build(OrcController,
            EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Idle),        // ยืนปกติ (ไม่ยกโล่) จะได้ต่างจากท่าตั้งโล่
            EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Walk),
            new[] { slam, sweep, jump, guard },      // Attack1 สับพื้น / Attack2 เหวี่ยง / Attack3 กระโดด / Attack4 ตั้งโล่
            EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Hit),
            EnemyAnimatorBuilder.LoadFirstClip(OrcImportSettings.Death));
        // ตั้งโล่ค้างไว้จนกว่า OrcBoss จะสั่งเลิก -> เอาทางออกอัตโนมัติของ Attack4 ออก
        foreach (ChildAnimatorState s in controller.layers[0].stateMachine.states)
        {
            if (s.state.name != "Attack4") continue;
            AnimatorStateTransition[] exits = s.state.transitions;
            for (int i = 0; i < exits.Length; i++) s.state.RemoveTransition(exits[i]);
        }
        EditorUtility.SetDirty(controller);

        // 2. ค่าพลัง (มีแล้วไม่ทับ)
        EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(OrcDataPath);
        if (data == null)
        {
            // บอส: เลือดเยอะมาก เดินช้า โดนตีไม่ชะงัก — ดาเมจของแต่ละสกิลอยู่ใน OrcBoss
            data = ScriptableObject.CreateInstance<EnemyData>();
            data.enemyName = "Orc Warlord";
            data.maxHP = 600f;
            data.damage = 25f;
            data.expDrop = 200;
            data.modelScale = 1.5f;
            data.moveSpeed = 2.4f;
            data.detectRange = 30f;
            data.attackRange = 4f;         // เข้ามาใกล้ขนาดนี้แล้วเริ่มใช้สกิลระยะประชิด
            data.attackCooldown = 0.8f;    // พักระหว่างสกิล
            data.hitPoint = 0.6f;
            data.attackAnimSpeed = 0.85f;  // ท่าหนัก ๆ ช้ากว่าของแพ็กนิดนึง
            data.attackAnims = new[] { "Attack1" };
            data.canBeStunned = false;
            data.stunTime = 0f;
            data.corpseTime = 6f;
            AssetDatabase.CreateAsset(data, OrcDataPath);
        }

        // 3. Material ของพื้นแดง (Sprites/Default ใช้สีจาก Vertex + โปร่งใส)
        Material indicatorMat = AssetDatabase.LoadAssetAtPath<Material>(IndicatorMaterialPath);
        if (indicatorMat == null)
        {
            indicatorMat = new Material(Shader.Find("Sprites/Default"));
            AssetDatabase.CreateAsset(indicatorMat, IndicatorMaterialPath);
        }

        // 4. ตัวบอส (ใช้ Material URP Lit + สีของแพ็ก ให้กะพริบตอนโดนตีได้เหมือนศัตรูตัวอื่น)
        Material mat = GetOrCreateUrpMaterial("Assets/_Game/Materials/Orc.mat", OrcTexture, null, 256);
        GameObject root = CreateEnemyObject<OrcBoss>("OrcBoss", OrcModelPrefab, controller, OrcHeight,
            "thigh_stretch.l", "thigh_stretch.r", mat, out OrcBoss boss, out Animator anim);
        FitModelToBody(root.transform, OrcHeight, "Orc_Warrior_Combined2"); // ขวานห้อยต่ำกว่าเท้า -> วัดจากตัวอย่างเดียว
        // โมเดลของแพ็กมี Collider ติดมา (ตัว/ขวาน/โล่) -> ชนผู้เล่นและบังพื้นแดง ลบทิ้งเหลือแค่ CharacterController
        PrefabUtility.UnpackPrefabInstance(root.transform.GetChild(0).gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
        {
            if (!(c is CharacterController)) Object.DestroyImmediate(c);
        }
        root.GetComponent<CharacterController>().radius = 0.6f;
        boss.slamClip = slam;
        boss.sweepClip = sweep;
        boss.jumpClip = jump;
        boss.indicatorMaterial = indicatorMat;
        SaveEnemyPrefab(root, boss, anim, data, OrcPrefabPath);

        // 5. ซีนทดสอบที่เปิดอยู่: ใส่บอสให้ EnemyTestSpawner (คลิก ⋮ -> Spawn Boss)
        EnemyTestSpawner spawner = Object.FindAnyObjectByType<EnemyTestSpawner>();
        if (spawner != null)
        {
            var so = new SerializedObject(spawner);
            so.FindProperty("boss").objectReferenceValue = data;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(spawner.gameObject.scene);
            EditorSceneManager.SaveScene(spawner.gameObject.scene);
        }
        return data;
    }

    // ปรับขนาด/ตำแหน่งโมเดลจาก Renderer ของตัว (ไม่รวมอาวุธ) — เท้าแตะพื้นพอดี
    private static void FitModelToBody(Transform root, float targetHeight, string bodyName)
    {
        Transform model = root.GetChild(0);
        Transform body = FindDeep(model, bodyName);
        Renderer r = body != null ? body.GetComponent<Renderer>() : null;
        if (r == null) return;

        Bounds b = r.bounds;
        if (b.size.y > 0.01f) model.localScale *= targetHeight / b.size.y;
        b = r.bounds;
        model.position += new Vector3(root.position.x - b.center.x, root.position.y - b.min.y, root.position.z - b.center.z);
    }
    #endregion

    // วัสดุ URP จากรูป (ของ Asset Store หลายตัวใช้ shader เก่า ขึ้นสีชมพูใน URP) — ไม่แก้ไฟล์ของ Asset Store
    private static Material GetOrCreateUrpMaterial(string matPath, string texPath, string normalPath, int maxSize)
    {
        TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (ti != null && ti.maxTextureSize > maxSize)
        {
            ti.maxTextureSize = maxSize;
            ti.SaveAndReimport();
        }
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(texPath));
        mat.SetColor("_BaseColor", Color.white);
        mat.SetFloat("_Smoothness", 0.25f);
        if (!string.IsNullOrEmpty(normalPath))
        {
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.EnableKeyword("_NORMALMAP");
            }
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    #region Shared
    // สร้างตัวศัตรูชนิด T (Enemy หรือคลาสลูก) + โมเดล + CharacterController + แถบเลือด — ใช้ร่วมกันทุกศัตรู
    private static GameObject CreateEnemyObject<T>(string enemyName, string modelPrefabPath, RuntimeAnimatorController controller,
        float height, string legL, string legR, Material overrideMat, out T enemy, out Animator anim) where T : Enemy
    {
        GameObject root = new GameObject(enemyName);
        CharacterController cc = root.AddComponent<CharacterController>();
        enemy = root.AddComponent<T>();
        anim = null;

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPrefabPath);
        if (modelAsset != null)
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.name = enemyName + "Model";
            model.transform.SetParent(root.transform, false);
            anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = controller;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            if (overrideMat != null)
            {
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = overrideMat;
            }
            FitModel(root.transform, model.transform, height, legL, legR);
        }
        else
        {
            Debug.LogWarning("ไม่พบโมเดล ใช้แคปซูลแทน: " + modelPrefabPath);
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
            capsule.transform.SetParent(root.transform, false);
            capsule.transform.localPosition = new Vector3(0f, height / 2f, 0f);
            capsule.transform.localScale = new Vector3(0.7f, height / 2f, 0.7f);
        }

        cc.height = height;
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, height / 2f + cc.skinWidth, 0f);

        BuildHealthBar(root.transform, enemy, height + 0.35f);
        return root;
    }

    // ผูก EnemyData/Animator เข้ากับตัวศัตรู เซฟเป็น Prefab แล้วชี้ EnemyData.prefab กลับมาที่ Prefab
    private static void SaveEnemyPrefab(GameObject root, Enemy enemy, Animator anim, EnemyData data, string prefabPath)
    {
        var so = new SerializedObject(enemy);
        so.FindProperty("data").objectReferenceValue = data;
        so.FindProperty("animator").objectReferenceValue = anim;
        so.FindProperty("characterName").stringValue = data.enemyName;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        data.prefab = prefab.GetComponent<Enemy>();
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log(data.enemyName + " prefab built: " + prefabPath);
    }

    // ปรับโมเดลให้: สูงเท่า targetHeight, เท้าแตะพื้น, หันหน้าไป +Z เท่ากับตัว Enemy
    private static void FitModel(Transform root, Transform model, float targetHeight, string legLName, string legRName)
    {
        // หาทิศหน้าจากต้นขา: ขาขวา - ขาซ้าย = ทิศขวาของตัวละคร, หน้า = ขวา x บน
        // (ห้ามใช้มือ: ท่าตั้งต้นของก็อบลินยื่นมือขวาไปข้างหน้า ทำให้คำนวณทิศเพี้ยนไป ~67 องศา)
        Transform legL = FindDeep(model, legLName);
        Transform legR = FindDeep(model, legRName);
        if (legL != null && legR != null)
        {
            Vector3 right = legR.position - legL.position;
            right.y = 0f;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            if (forward.sqrMagnitude > 0.0001f)
            {
                model.rotation = Quaternion.FromToRotation(forward.normalized, Vector3.forward) * model.rotation;
            }
        }

        Bounds b = GetBounds(model);
        if (b.size.y > 0.01f) model.localScale *= targetHeight / b.size.y;

        b = GetBounds(model);
        model.position += new Vector3(root.position.x - b.center.x, root.position.y - b.min.y, root.position.z - b.center.z);
    }
    #endregion

    #region Helpers
    private static Bounds GetBounds(Transform t)
    {
        Renderer[] rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        foreach (Renderer r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    private static Transform FindDeep(Transform parent, string childName)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName) return child;
        }
        return null;
    }

    // แถบเลือดลอยเหนือหัว: Canvas แบบ World Space กว้าง 1 เมตร
    private static void BuildHealthBar(Transform parent, Character owner, float y)
    {
        GameObject canvasObj = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
        canvasObj.transform.SetParent(parent, false);
        canvasObj.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        RectTransform rt = canvasObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100f, 12f);
        rt.localPosition = new Vector3(0f, y, 0f);
        rt.localScale = Vector3.one * 0.01f;

        CreateBarImage(canvasObj.transform, "BG", new Color(0f, 0f, 0f, 0.65f));
        Image fill = CreateBarImage(canvasObj.transform, "Fill", new Color(0.85f, 0.15f, 0.1f));
        RectTransform fillRt = fill.rectTransform;
        fillRt.offsetMin = new Vector2(1.5f, 1.5f);
        fillRt.offsetMax = new Vector2(-1.5f, -1.5f);
        fillRt.pivot = new Vector2(0f, 0.5f); // ย่อจากขวาเข้าซ้าย

        EnemyHealthBar bar = canvasObj.AddComponent<EnemyHealthBar>();
        var so = new SerializedObject(bar);
        so.FindProperty("owner").objectReferenceValue = owner;
        so.FindProperty("fill").objectReferenceValue = fillRt;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Image CreateBarImage(Transform parent, string objName, Color color)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
    #endregion

    #region Player
    // Boat แบบเดียวกับ SceneBuilder.BuildBoatPlayer แต่ "โหลด" Boat.controller ที่มีอยู่ ไม่สร้างใหม่
    // (สร้างใหม่ GUID จะเปลี่ยน แล้วซีน Test_Player ของเพื่อนจะหา Animator ไม่เจอ)
    private static GameObject BuildPlayer()
    {
        const string modelPath = "Assets/_Game/Art/Boat/Boat.fbx";

        GameObject player = new GameObject("Player");
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.radius = 0.3f;
        Player playerScript = player.AddComponent<Player>();
        playerScript.characterName = "Boat";
        float height = 1.1f;

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        Animator anim = null;
        if (modelAsset == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
            capsule.transform.SetParent(player.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
            height = 2f;
        }
        else
        {
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
            model.name = "BoatModel";
            model.transform.SetParent(player.transform, false);
            anim = model.GetComponent<Animator>();
            if (anim == null) anim = model.AddComponent<Animator>();
            RuntimeAnimatorController boatController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BoatAnimatorBuilder.ControllerPath);
            if (boatController == null) boatController = BoatAnimatorBuilder.Build(); // ยังไม่มีเลยค่อยสร้าง
            anim.runtimeAnimatorController = boatController;
            anim.applyRootMotion = false;

            Material boatMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Game/Art/Boat/Boat.mat");
            if (boatMat != null)
            {
                foreach (Renderer r in model.GetComponentsInChildren<Renderer>()) r.sharedMaterial = boatMat;
            }

            const float targetHeight = 1.5f;
            Bounds b = GetBounds(model.transform);
            if (b.size.y > 0.01f) model.transform.localScale *= targetHeight / b.size.y;
            height = targetHeight * 0.95f;

            Transform leftHand = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
            if (leftHand != null && player.transform.InverseTransformPoint(leftHand.position).x > 0f)
            {
                model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }
        }
        cc.height = height;
        cc.center = new Vector3(0f, height / 2f, 0f);

        // สกิลของ Boat เหมือน Test_Player (โหลด asset ที่เพื่อนสร้างไว้)
        var so = new SerializedObject(playerScript);
        so.FindProperty("animator").objectReferenceValue = anim;
        SerializedProperty skills = so.FindProperty("startingSkills");
        skills.ClearArray();
        foreach (string skillName in new[] { "Dash", "OverPunch", "FinalExecution", "MetalHeart" })
        {
            SkillData sd = AssetDatabase.LoadAssetAtPath<SkillData>("Assets/_Game/Data/Skills/" + skillName + ".asset");
            if (sd == null) continue;
            skills.InsertArrayElementAtIndex(skills.arraySize);
            skills.GetArrayElementAtIndex(skills.arraySize - 1).objectReferenceValue = sd;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }
    #endregion

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) Object.DestroyImmediate(existing);
    }
}
