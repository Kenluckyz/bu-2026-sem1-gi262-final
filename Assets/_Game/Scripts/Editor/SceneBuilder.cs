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

        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        Player playerScript = player.AddComponent<Player>();
        playerScript.characterName = "Hero";

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
