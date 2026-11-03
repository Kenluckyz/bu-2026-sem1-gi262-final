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

        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Text levelText = CreateText(canvasObj.transform, "LevelText", font, new Vector2(40f, -30f), 36, "Lv. 1");
        Image hpFill = CreateBar(canvasObj.transform, "HPBar", sprite, new Vector2(40f, -80f), new Vector2(400f, 32f), new Color(0.9f, 0.2f, 0.25f));
        Text hpText = CreateText(hpFill.transform.parent, "HPText", font, new Vector2(10f, 0f), 22, "100 / 100");
        Image expFill = CreateBar(canvasObj.transform, "EXPBar", sprite, new Vector2(40f, -122f), new Vector2(400f, 16f), new Color(0.3f, 0.75f, 1f));

        PlayerHUD hud = canvasObj.AddComponent<PlayerHUD>();
        var so = new SerializedObject(hud);
        so.FindProperty("player").objectReferenceValue = player;
        so.FindProperty("hpFill").objectReferenceValue = hpFill;
        so.FindProperty("expFill").objectReferenceValue = expFill;
        so.FindProperty("hpText").objectReferenceValue = hpText;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.ApplyModifiedPropertiesWithoutUndo();
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
