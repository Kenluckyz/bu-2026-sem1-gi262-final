using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// เมนู BoatVsBoss > Build Test_Player Scene : สร้างพื้น ผู้เล่น กล้อง ให้พร้อมกด Play
public static class SceneBuilder
{
    private const string ScenePath = "Assets/_Game/Scenes/Test_Player.unity";

    [MenuItem("BoatVsBoss/Build Test_Player Scene")]
    public static void BuildTestPlayerScene()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        DestroyIfExists("Ground");
        DestroyIfExists("Player");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(3f, 1f, 3f);

        GameObject player = BuildBoatPlayer();
        Player playerScript = player.GetComponent<Player>();

        // สกิลตัวอย่าง: 1 = SpinPunch, 2 = FireStorm, 3 = PowerUp (Passive)
        SkillData spin = GetOrCreateSkill("SpinPunch", SkillKind.Active, SkillEffect.SpinPunch, 15f, 5f, 3f, 2.5f, 0, StatType.Damage, true);
        SkillData storm = GetOrCreateSkill("FireStorm", SkillKind.Active, SkillEffect.FireStorm, 25f, 10f, 6f, 8f, 3, StatType.Damage, false);
        SkillData power = GetOrCreateSkill("PowerUp", SkillKind.Passive, SkillEffect.StatBoost, 10f, 5f, 0f, 0f, 0, StatType.Damage, true);
        if (!spin.nextSkills.Contains(storm)) { spin.nextSkills.Add(storm); EditorUtility.SetDirty(spin); }
        var pso = new SerializedObject(playerScript);
        SerializedProperty list = pso.FindProperty("startingSkills");
        list.ClearArray();
        SkillData[] start = { spin, storm, power };
        for (int i = 0; i < start.Length; i++)
        {
            list.InsertArrayElementAtIndex(i);
            list.GetArrayElementAtIndex(i).objectReferenceValue = start[i];
        }
        pso.ApplyModifiedPropertiesWithoutUndo();

        for (int i = 0; i < 3; i++)
        {
            string dummyName = "Dummy_" + i;
            DestroyIfExists(dummyName);
            GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummy.name = dummyName;
            dummy.transform.position = new Vector3(-3f + i * 3f, 0.75f, 4f);
            dummy.transform.localScale = new Vector3(1f, 1.5f, 1f);
            dummy.AddComponent<TrainingDummy>().characterName = dummyName;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        CameraFollow follow = cam.GetComponent<CameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<CameraFollow>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = player.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        BuildHUD(playerScript);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Test_Player scene built");
    }

    // Player = กล่องเปล่าที่มี CharacterController + Player.cs, โมเดล Boat เป็นลูก (ถ้าไม่มีโมเดลใช้แคปซูลแทน)
    private static GameObject BuildBoatPlayer()
    {
        const string modelPath = "Assets/_Game/Art/Boat/Boat.fbx";
        foreach (string p in new[] { modelPath, "Assets/_Game/Art/Boat/Anims/Idle.fbx", "Assets/_Game/Art/Boat/Anims/Run.fbx",
                     "Assets/_Game/Art/Boat/Anims/Punch.fbx", "Assets/_Game/Art/Boat/Anims/HookPunch.fbx",
                     "Assets/_Game/Art/Boat/Anims/Hit.fbx", "Assets/_Game/Art/Boat/Anims/Death.fbx", "Assets/_Game/Art/Boat/Anims/Uppercut.fbx" })
        {
            ModelImporter mi = AssetImporter.GetAtPath(p) as ModelImporter;
            if (mi != null && mi.animationType != ModelImporterAnimationType.Human)
            {
                AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate); // ให้ BoatImportSettings ตั้งเป็น Humanoid
            }
        }

        GameObject player = new GameObject("Player");
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 1.1f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.57f, 0f);
        Player playerScript = player.AddComponent<Player>();
        playerScript.characterName = "Boat";

        GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
        if (modelAsset == null)
        {
            GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(capsule.GetComponent<CapsuleCollider>());
            capsule.transform.SetParent(player.transform, false);
            capsule.transform.localPosition = new Vector3(0f, 1f, 0f);
            return player;
        }

        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.name = "BoatModel";
        model.transform.SetParent(player.transform, false);
        Animator anim = model.GetComponent<Animator>();
        if (anim == null) anim = model.AddComponent<Animator>();
        anim.runtimeAnimatorController = BoatAnimatorBuilder.Build();
        anim.applyRootMotion = false;

        // ใส่สี: FBX ฝังรูปไว้แต่ Unity ไม่ดึงออกมา เลยทำ Material เองจาก Boat_BaseColor.png
        Material boatMat = GetOrCreateBoatMaterial();
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            r.sharedMaterial = boatMat;
        }

        // ปรับขนาดให้สูง targetHeight เมตร (หน่วยจาก Blender ทำให้ตัวเล็ก)
        const float targetHeight = 1.5f;
        Bounds b = new Bounds(model.transform.position, Vector3.zero);
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
        if (b.size.y > 0.01f)
        {
            model.transform.localScale *= targetHeight / b.size.y;
        }
        cc.height = targetHeight * 0.95f;
        cc.center = new Vector3(0f, cc.height / 2f, 0f);

        // ให้หน้าโมเดลหันไปทาง +Z เท่ากับ Player: ถ้ามือซ้ายอยู่ฝั่ง +X แปลว่าหันกลับด้าน
        Transform leftHand = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.LeftHand) : null;
        if (leftHand != null && player.transform.InverseTransformPoint(leftHand.position).x > 0f)
        {
            model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        if (!anim.isHuman) Debug.LogWarning("Boat ยังไม่เป็น Humanoid — เช็ก Rig ของ Boat.fbx");

        var so = new SerializedObject(playerScript);
        so.FindProperty("animator").objectReferenceValue = anim;
        so.ApplyModifiedPropertiesWithoutUndo();
        return player;
    }

    private static Material GetOrCreateBoatMaterial()
    {
        const string texPath = "Assets/_Game/Art/Boat/Boat_BaseColor.png";
        const string matPath = "Assets/_Game/Art/Boat/Boat.mat";
        TextureImporter ti = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (ti != null && ti.maxTextureSize != 2048)
        {
            ti.maxTextureSize = 2048; // 4K หนักเกินสำหรับเครื่องเพื่อน
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
        mat.SetFloat("_Smoothness", 0.15f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // Canvas มุมซ้ายบน: Lv. / แถบเลือดแดง / แถบ EXP ฟ้า
    private static void BuildHUD(Player player)
    {
        DestroyIfExists("HUD");

        GameObject canvasObj = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform root = canvasObj.transform;

        // ขนาด = ครึ่งหนึ่งของไฟล์ภาพ, ระยะ fill = ระยะขอบช่องในภาพ x 0.5
        CreateImage(root, "HPFrame", LoadSprite("hp_frame"), new Vector2(175f, -50f), new Vector2(474f, 81f));
        Image hpFill = CreateImage(root, "HPFill", LoadSprite("hp_fill"), new Vector2(175f + 20f, -50f - 19f), new Vector2(432f, 42f));
        MakeFilled(hpFill);
        Text hpText = CreateText(root, "HPText", font, new Vector2(195f, -69f), 24, "100 / 100");
        hpText.rectTransform.sizeDelta = new Vector2(432f, 42f);
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.gameObject.AddComponent<Outline>().effectColor = new Color(0.23f, 0.14f, 0.09f);

        CreateImage(root, "EXPFrame", LoadSprite("exp_frame"), new Vector2(185f, -130f), new Vector2(448f, 34f));
        Image expFill = CreateImage(root, "EXPFill", LoadSprite("exp_fill"), new Vector2(185f + 13.5f, -130f - 10f), new Vector2(419f, 11f));
        MakeFilled(expFill);

        CreateImage(root, "Portrait", LoadSprite("portrait_boat"), new Vector2(30f, -25f), new Vector2(150f, 171f));
        CreateImage(root, "LevelBadge", LoadSprite("level_badge"), new Vector2(125f, -135f), new Vector2(64f, 65f));
        Text levelText = CreateText(root, "LevelText", font, new Vector2(125f, -135f), 30, "1");
        levelText.rectTransform.sizeDelta = new Vector2(64f, 65f);
        levelText.alignment = TextAnchor.MiddleCenter;
        levelText.color = new Color(1f, 0.9f, 0.55f);

        PlayerHUD hud = canvasObj.AddComponent<PlayerHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("hpFill").objectReferenceValue = hpFill;
        so.FindProperty("expFill").objectReferenceValue = expFill;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.ApplyModifiedPropertiesWithoutUndo();

        BuildSkillBar(root, player, font);
    }

    // แถบสกิลกลางล่าง: วงกลม 3 ช่อง (ปุ่ม 1, 2, 3)
    private static void BuildSkillBar(Transform root, Player player, Font font)
    {
        GameObject bar = new GameObject("SkillBar", typeof(RectTransform));
        bar.transform.SetParent(root, false);
        RectTransform barRect = bar.GetComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = new Vector2(0.5f, 0f);
        barRect.pivot = new Vector2(0.5f, 0f);
        barRect.anchoredPosition = new Vector2(0f, 30f);
        barRect.sizeDelta = new Vector2(420f, 150f);

        SkillBarUI ui = root.gameObject.AddComponent<SkillBarUI>();
        var so = new SerializedObject(ui);
        so.FindProperty("player").objectReferenceValue = player;
        SerializedProperty slots = so.FindProperty("slots");
        slots.arraySize = 3;

        string[] keys = { "1", "2", "3" };
        float[] sizes = { 120f, 120f, 120f };
        float[] xs = { -140f, 0f, 140f };
        for (int i = 0; i < 3; i++)
        {
            GameObject slot = new GameObject("Slot_" + (i + 1), typeof(RectTransform));
            slot.transform.SetParent(bar.transform, false);
            RectTransform rt = slot.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(xs[i], 24f);
            rt.sizeDelta = new Vector2(sizes[i], sizes[i]);

            Image bg = CreateFill(slot.transform, "BG", LoadSprite("circle_slot"), 0.86f);
            Image icon = CreateFill(slot.transform, "Icon", null, 0.80f);
            icon.enabled = false;
            Text nameText = CreateCenterText(slot.transform, "Name", font, 15, new Color(1f, 0.9f, 0.6f));
            Image mask = CreateFill(slot.transform, "Cooldown", LoadSprite("circle_white"), 0.86f);
            mask.color = new Color(0f, 0f, 0f, 0.6f);
            mask.type = Image.Type.Filled;
            mask.fillMethod = Image.FillMethod.Radial360;
            mask.fillOrigin = (int)Image.Origin360.Top;
            mask.fillClockwise = false;
            mask.fillAmount = 0f;
            Text cdText = CreateCenterText(slot.transform, "CooldownText", font, 30, Color.white);
            CreateFill(slot.transform, "Ring", LoadSprite("skill_slot_ring"), 1f);

            Text levelText = CreateCenterText(slot.transform, "Level", font, 16, new Color(1f, 0.9f, 0.55f));
            levelText.rectTransform.anchorMin = levelText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            levelText.rectTransform.anchoredPosition = new Vector2(0f, 10f);
            levelText.rectTransform.sizeDelta = new Vector2(100f, 24f);

            {
                Text key = CreateCenterText(slot.transform, "Key", font, 22, Color.white);
                key.text = keys[i];
                key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                key.rectTransform.anchoredPosition = new Vector2(0f, -14f);
                key.rectTransform.sizeDelta = new Vector2(40f, 28f);
            }

            SerializedProperty s = slots.GetArrayElementAtIndex(i);
            s.FindPropertyRelative("icon").objectReferenceValue = icon;
            s.FindPropertyRelative("nameText").objectReferenceValue = nameText;
            s.FindPropertyRelative("cooldownMask").objectReferenceValue = mask;
            s.FindPropertyRelative("cooldownText").objectReferenceValue = cdText;
            s.FindPropertyRelative("levelText").objectReferenceValue = levelText;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // Image ที่อยู่กลางช่อง ขนาดเป็นสัดส่วนของช่อง
    private static Image CreateFill(Transform parent, string objName, Sprite sprite, float scale)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        float m = (1f - scale) / 2f;
        rt.anchorMin = new Vector2(m, m);
        rt.anchorMax = new Vector2(1f - m, 1f - m);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateCenterText(Transform parent, string objName, Font font, int size, Color color)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.raycastTarget = false;
        text.text = "";
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0.23f, 0.14f, 0.09f);
        return text;
    }

    private static SkillData GetOrCreateSkill(string skillName, SkillKind kind, SkillEffect effect, float baseValue, float perLevel,
        float cooldown, float radius, int targetCount, StatType stat, bool startAvailable)
    {
        string path = "Assets/_Game/Data/Skills/" + skillName + ".asset";
        SkillData data = AssetDatabase.LoadAssetAtPath<SkillData>(path);
        if (data != null) return data; // มีแล้วไม่ทับ เผื่อปรับค่าใน Inspector ไว้

        data = ScriptableObject.CreateInstance<SkillData>();
        data.skillName = skillName;
        data.kind = kind;
        data.effect = effect;
        data.baseValue = baseValue;
        data.valuePerLevel = perLevel;
        data.cooldown = cooldown;
        data.radius = radius;
        data.targetCount = targetCount;
        data.statType = stat;
        data.isStartAvailable = startAvailable;
        AssetDatabase.CreateAsset(data, path);
        AssetDatabase.SaveAssets();
        return data;
    }

    // โหลดรูปจาก Art/UI และตั้งให้เป็น Sprite ถ้ายังไม่ได้ตั้ง
    private static Sprite LoadSprite(string fileName)
    {
        string path = "Assets/_Game/Art/UI/" + fileName + ".png";
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("ไม่พบรูป " + path);
            return null;
        }
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static Image CreateImage(Transform parent, string objName, Sprite sprite, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        SetTopLeft(obj.GetComponent<RectTransform>(), pos, size);
        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static void MakeFilled(Image image)
    {
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    // พื้นหลังเข้ม + ตัวเติมสีที่ใช้ fillAmount
    private static Image CreateBar(Transform parent, string barName, Sprite sprite, Vector2 pos, Vector2 size, Color color)
    {
        GameObject bg = new GameObject(barName, typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        SetTopLeft(bg.GetComponent<RectTransform>(), pos, size);
        Image bgImage = bg.GetComponent<Image>();
        bgImage.sprite = sprite;
        bgImage.type = Image.Type.Sliced;
        bgImage.color = new Color(0f, 0f, 0f, 0.6f);

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(bg.transform, false);
        RectTransform rt = fill.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(3f, 3f);
        rt.offsetMax = new Vector2(-3f, -3f);
        Image fillImage = fill.GetComponent<Image>();
        fillImage.sprite = sprite;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.color = color;
        return fillImage;
    }

    private static Text CreateText(Transform parent, string textName, Font font, Vector2 pos, int fontSize, string value)
    {
        GameObject obj = new GameObject(textName, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        SetTopLeft(obj.GetComponent<RectTransform>(), pos, new Vector2(400f, 44f));
        Text text = obj.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = value;
        return text;
    }

    private static void SetTopLeft(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null) Object.DestroyImmediate(existing);
    }
}
