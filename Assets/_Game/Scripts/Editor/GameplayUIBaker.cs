using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// เมนู BoatVsBoss > Bake Gameplay UI (เครื่องมือใน Editor ไม่ได้อยู่ในตัวเกม)
// สร้าง UI ที่เมื่อก่อนสร้างด้วยโค้ดตอนเริ่มเกม ให้กลายเป็น object จริงในซีน Gameplay แล้วลากใส่ช่องของสคริปต์ให้
// หลังกดแล้ว ปรับตำแหน่ง / สี / ขนาด / ข้อความ ใน Hierarchy ได้เลย โค้ดในเกมไม่สร้างทับ
// 1. GameplayHUD  -> Gameplay > GameplayCanvas (การ์ดห้อง, ป้ายกลางจอ, แถบบอส, ปุ่มลัด, หน้าจบ, หน้าหยุด)
// 2. DodgeCooldownUI -> HUD > DodgeSlot (ปิดไว้ ติ๊กเปิดถ้าอยากโชว์)
// 3. UpgradeUI    -> UpgradeSystem > UpgradeCanvas (การ์ดเลเวลอัป 3 ใบ)
// 4. BossCutscene -> BossFX > BossIntroCanvas (แถบดำ, ชื่อบอส, ขอบจอแดง, แฟลช)
// กดซ้ำได้: อันไหนสร้างแล้วจะข้าม (ไม่ทับของที่ปรับไว้) ถ้าอยากสร้างใหม่ให้ลบ Canvas อันนั้นทิ้งก่อน
public static class GameplayUIBaker
{
    private const string ScenePath = "Assets/_Game/Scenes/Gameplay.unity";
    private const string ArtFolder = "Assets/_Game/Art/UI/";
    private const string VignettePath = "Assets/_Game/Art/UI/boss_vignette.asset";

    private static readonly Color Ivory = new Color(0.93f, 0.89f, 0.78f);
    private static readonly Color Gold = new Color(0.9f, 0.62f, 0.28f);
    private static readonly Color Muted = new Color(0.62f, 0.72f, 0.72f);
    private static readonly Color CardColor = new Color(0.03f, 0.05f, 0.07f, 0.85f);
    private static readonly Color BossRed = new Color(0.85f, 0.16f, 0.1f);
    private static readonly Color WoodDark = new Color(0.24f, 0.14f, 0.07f, 0.97f);
    private static readonly Color CardGold = new Color(1f, 0.85f, 0.4f);
    private static readonly Color Cream = new Color(1f, 0.95f, 0.85f);

    [MenuItem("BoatVsBoss/Bake Gameplay UI")]
    public static void BakeAll()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogError("กด Stop ก่อน แล้วค่อยกดเมนูนี้");
            return;
        }
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // 1. หาสคริปต์ทั้ง 4 ในซีน แล้วสร้าง UI ให้แต่ละตัว
        GameplayHUD hud = Object.FindAnyObjectByType<GameplayHUD>(FindObjectsInactive.Include);
        DodgeCooldownUI dodge = Object.FindAnyObjectByType<DodgeCooldownUI>(FindObjectsInactive.Include);
        UpgradeUI upgrade = Object.FindAnyObjectByType<UpgradeUI>(FindObjectsInactive.Include);
        BossCutscene cutscene = Object.FindAnyObjectByType<BossCutscene>(FindObjectsInactive.Include);
        if (hud != null) BakeHUD(hud); else Debug.LogWarning("ไม่พบ GameplayHUD ในซีน");
        if (dodge != null) BakeDodge(dodge); else Debug.LogWarning("ไม่พบ DodgeCooldownUI ในซีน");
        if (upgrade != null) BakeUpgrade(upgrade); else Debug.LogWarning("ไม่พบ UpgradeUI ในซีน");
        if (cutscene != null) BakeBossCutscene(cutscene); else Debug.LogWarning("ไม่พบ BossCutscene ในซีน");

        // 2. บันทึก
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GameplayUIBaker] สร้าง UI ลงซีน Gameplay แล้ว ปรับต่อใน Hierarchy ได้เลย");
    }

    #region 1. GameplayHUD
    public static void BakeHUD(GameplayHUD hud)
    {
        SerializedObject so = new SerializedObject(hud);
        if (so.FindProperty("roomText").objectReferenceValue != null)
        {
            Debug.Log("GameplayHUD มี UI แล้ว ข้าม");
            return;
        }
        GameManager game = Object.FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
        Transform root = CreateCanvas("GameplayCanvas", hud.transform, 20, true);

        // 1. การ์ดห้อง มุมขวาบน
        RectTransform card = Panel(root, "RoomCard", new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(430f, 118f), CardColor);
        card.pivot = new Vector2(1f, 1f);
        Panel(card, "GoldRule", new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 118f), Gold).pivot = new Vector2(0f, 0.5f);
        Set(so, "roomText", Label(card, "Room", new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(400f, 26f), "ROOM 1", 17, Gold));
        Set(so, "waveText", Label(card, "Wave", new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(400f, 40f), "WAVE  1 / 4", 32, Ivory));
        Set(so, "countText", Label(card, "Count", new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(400f, 24f), "3 ENEMIES LEFT", 17, Muted));

        // 2. ป้ายกลางจอ (ข้อความตัวอย่าง ตอนเล่นจะใสไว้จนมีประกาศ)
        Text bannerTitle = Label(root, "BannerTitle", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(1400f, 70f), "ROOM 1", 54, Ivory);
        Text bannerSub = Label(root, "BannerSub", new Vector2(0.5f, 0.72f), new Vector2(0f, -52f), new Vector2(1400f, 34f), "4 waves - survive them all", 24, Gold);
        AddShadow(bannerTitle);
        AddShadow(bannerSub);
        Set(so, "bannerTitle", bannerTitle);
        Set(so, "bannerSub", bannerSub);

        // 3. แถบเลือดบอส บนกลาง (ความกว้างของ Fill = ความกว้างเต็มของแถบ)
        const float barWidth = 640f;
        RectTransform bar = Panel(root, "BossBar", new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(barWidth + 16f, 62f), CardColor);
        Set(so, "bossBar", bar.gameObject);
        Set(so, "bossName", Label(bar, "BossName", new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(barWidth, 24f), "ORC WARLORD   LV.1", 19, Gold));
        Panel(bar, "Track", new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(barWidth, 16f), new Color(0.15f, 0.08f, 0.08f));
        RectTransform trail = Panel(bar, "Trail", new Vector2(0.5f, 0f), new Vector2(-barWidth / 2f, 18f), new Vector2(barWidth, 16f), new Color(1f, 0.9f, 0.75f));
        trail.pivot = new Vector2(0f, 0.5f);
        RectTransform fill = Panel(bar, "Fill", new Vector2(0.5f, 0f), new Vector2(-barWidth / 2f, 18f), new Vector2(barWidth, 16f), BossRed);
        fill.pivot = new Vector2(0f, 0.5f);
        Set(so, "bossTrail", trail);
        Set(so, "bossFill", fill);
        bar.gameObject.SetActive(false);

        // 4. ปุ่มที่ใช้ได้ มุมซ้ายล่าง
        Text controls = Label(root, "Controls", new Vector2(0f, 0f), new Vector2(200f, 40f), new Vector2(360f, 50f),
            "WASD  Move     LMB  Attack\n1 2 3  Skills     Esc  Pause     Shift  Dash", 16, Muted);
        controls.alignment = TextAnchor.MiddleLeft;
        controls.horizontalOverflow = HorizontalWrapMode.Overflow;

        // 5. หน้าจบเกม (เต็มจอ มืด ๆ)
        RectTransform end = Panel(root, "EndPanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 3000f), new Color(0f, 0f, 0f, 0.72f));
        end.GetComponent<Image>().raycastTarget = true;
        Text endTitle = Label(end, "Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f), new Vector2(1200f, 100f), "VICTORY", 86, Gold);
        AddShadow(endTitle);
        Set(so, "endPanel", end.gameObject);
        Set(so, "endTitle", endTitle);
        Set(so, "endStats", Label(end, "Stats", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1200f, 110f),
            "The Orc Warlord is slain. The dungeon is cleared!\n\nTime  07:30      Enemies defeated  80", 26, Ivory));
        Button again = CreateButton(end, "PlayAgain", "PLAY AGAIN  [R]", new Vector2(0f, -120f));
        if (game != null) UnityEventTools.AddPersistentListener(again.onClick, game.Restart);
        end.gameObject.SetActive(false);

        // 6. หน้าหยุดเกม
        RectTransform pause = Panel(root, "PausePanel", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 3000f), new Color(0f, 0f, 0f, 0.6f));
        pause.GetComponent<Image>().raycastTarget = true;
        Label(pause, "Title", new Vector2(0.5f, 0.5f), new Vector2(0f, 90f), new Vector2(800f, 90f), "PAUSED", 72, Ivory);
        Button resume = CreateButton(pause, "Resume", "RESUME  [Esc]", new Vector2(0f, -20f));
        Button restart = CreateButton(pause, "Restart", "RESTART", new Vector2(0f, -100f));
        if (game != null)
        {
            UnityEventTools.AddPersistentListener(resume.onClick, game.TogglePause);
            UnityEventTools.AddPersistentListener(restart.onClick, game.Restart);
        }
        Set(so, "pausePanel", pause.gameObject);
        pause.gameObject.SetActive(false);

        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("สร้าง GameplayCanvas แล้ว");
    }
    #endregion

    #region 2. DodgeCooldownUI
    public static void BakeDodge(DodgeCooldownUI dodge)
    {
        SerializedObject so = new SerializedObject(dodge);
        if (so.FindProperty("mask").objectReferenceValue != null)
        {
            Debug.Log("DodgeCooldownUI มีช่องแล้ว ข้าม");
            return;
        }
        GameObject hud = GameObject.Find("HUD");
        if (hud == null)
        {
            Debug.LogWarning("ไม่พบ Canvas ชื่อ HUD ข้ามช่อง Dodge");
            return;
        }
        Transform oldSlot = hud.transform.Find("DodgeSlot");
        if (oldSlot != null) Object.DestroyImmediate(oldSlot.gameObject); // ช่องเก่าที่ไม่ได้ผูกกับสคริปต์

        // 1. ช่องวงกลม ถัดจากช่องสกิล 3
        const float size = 96f;
        RectTransform slot = Rect(hud.transform, "DodgeSlot", new Vector2(0.5f, 0f), new Vector2(250f, 54f), new Vector2(size, size));
        slot.pivot = new Vector2(0.5f, 0f);
        slot.anchoredPosition = new Vector2(250f, 54f);

        // 2. พื้น > คำว่า DODGE > วงมืดคูลดาวน์ > ตัวเลข > ขอบ > ปุ่ม Shift
        Image bg = SlotImage(slot, "BG", LoadSprite("circle_slot"), size * 0.86f);
        bg.color = new Color(0.2f, 0.35f, 0.5f);
        Text label = SlotText(slot, "Label", 18, new Color(0.75f, 0.92f, 1f), "DODGE", size);
        Image mask = SlotImage(slot, "Cooldown", LoadSprite("circle_white"), size * 0.86f);
        mask.color = new Color(0f, 0f, 0f, 0.6f);
        mask.type = Image.Type.Filled;
        mask.fillMethod = Image.FillMethod.Radial360;
        mask.fillOrigin = (int)Image.Origin360.Top;
        mask.fillClockwise = false;
        mask.fillAmount = 0f;
        Text cooldownText = SlotText(slot, "CooldownText", 26, Color.white, "", size);
        SlotImage(slot, "Ring", LoadSprite("skill_slot_ring"), size);
        Text key = SlotText(slot, "Key", 18, Color.white, "Shift", size);
        key.rectTransform.anchorMin = key.rectTransform.anchorMax = new Vector2(0.5f, 0f);
        key.rectTransform.anchoredPosition = new Vector2(0f, -14f);
        key.rectTransform.sizeDelta = new Vector2(80f, 26f);

        Set(so, "mask", mask);
        Set(so, "cooldownText", cooldownText);
        Set(so, "label", label);
        so.ApplyModifiedPropertiesWithoutUndo();
        slot.gameObject.SetActive(false); // เดิมปิดไว้ (showSlotOnScreen = false) ติ๊กเปิดใน Hierarchy ได้
        Debug.Log("สร้าง HUD > DodgeSlot แล้ว (ปิดไว้)");
    }

    private static Image SlotImage(Transform parent, string objName, Sprite sprite, float size)
    {
        RectTransform rt = Rect(parent, objName, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.raycastTarget = false;
        return img;
    }

    private static Text SlotText(Transform parent, string objName, int fontSize, Color color, string value, float size)
    {
        Text text = Label(parent, objName, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size), value, fontSize, color);
        text.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        return text;
    }
    #endregion

    #region 3. UpgradeUI
    public static void BakeUpgrade(UpgradeUI ui)
    {
        SerializedObject so = new SerializedObject(ui);
        if (so.FindProperty("panel").objectReferenceValue != null)
        {
            Debug.Log("UpgradeUI มี UI แล้ว ข้าม");
            return;
        }
        Transform root = CreateCanvas("UpgradeCanvas", ui.transform, 50, true);

        // 1. พื้นมืดเต็มจอ + หัวข้อ
        RectTransform panel = Rect(root, "UpgradePanel", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(panel);
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);
        Text title = CardText(panel, "Title", 64, CardGold, new Vector2(0f, 420f), new Vector2(1200f, 100f), "LEVEL UP!  Lv.2");
        title.gameObject.AddComponent<Outline>().effectColor = WoodDark;
        CardText(panel, "Hint", 26, Cream, new Vector2(0f, 355f), new Vector2(1200f, 50f), "เลือกอัปเกรด 1 อย่าง (คลิก หรือกด 1 / 2 / 3)");
        Set(so, "panel", panel.gameObject);
        Set(so, "titleText", title);

        // 2. การ์ด 3 ใบ เรียงกลางจอ
        const int count = 3;
        SerializedProperty cards = so.FindProperty("cards");
        cards.arraySize = count;
        for (int i = 0; i < count; i++)
        {
            float x = (i - (count - 1) / 2f) * 440f;
            BuildCard(ui, panel, i, new Vector2(x, -90f), cards.GetArrayElementAtIndex(i));
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        panel.gameObject.SetActive(false); // ปิดไว้ ติ๊กเปิดตอนจะปรับหน้าตา
        Debug.Log("สร้าง UpgradeCanvas แล้ว (UpgradePanel ปิดไว้)");
    }

    // การ์ด: เงา > ผ้า > ขอบ > ไอคอน > ตัวหนังสือ
    private static void BuildCard(UpgradeUI ui, Transform parent, int index, Vector2 pos, SerializedProperty card)
    {
        Vector2 size = new Vector2(344f, 640f); // สัดส่วนเท่ารูปธง
        Sprite glowSprite = LoadSprite("upgrade_banner_glow");
        RectTransform root = Rect(parent, "Card" + (index + 1), new Vector2(0.5f, 0.5f), pos, size);
        Image hit = root.gameObject.AddComponent<Image>();
        hit.sprite = glowSprite;
        hit.color = new Color(1f, 1f, 1f, 0f); // ใสแต่กดได้
        Button button = root.gameObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None; // เด้งเองใน UpgradeCardMotion
        UnityEventTools.AddIntPersistentListener(button.onClick, ui.Pick, index); // กดการ์ดใบที่ i = Pick(i)

        Image glow = CardImage(root, "Glow", glowSprite, Vector2.zero, size * 1.07f);
        Image cloth = CardImage(root, "Cloth", LoadSprite("upgrade_banner_cloth"), Vector2.zero, size);
        cloth.color = new Color(0.3f, 0.55f, 1f);
        CardImage(root, "Trim", LoadSprite("upgrade_banner_trim"), Vector2.zero, size);

        // ไอคอนวงกลมบนไม้แขวน
        Vector2 iconPos = new Vector2(0f, 262f);
        Image bg = CardImage(root, "IconBG", LoadSprite("circle_slot"), iconPos, new Vector2(150f, 150f));
        bg.color = new Color(0.15f, 0.1f, 0.06f);
        Image icon = CardImage(root, "Icon", null, iconPos, new Vector2(132f, 132f));
        Text iconLabel = CardText(root, "IconLabel", 44, CardGold, iconPos, new Vector2(140f, 140f), "");
        CardImage(root, "Ring", LoadSprite("skill_slot_ring"), iconPos, new Vector2(176f, 176f));

        Text key = CardText(root, "Key", 28, Cream, new Vector2(-120f, 200f), new Vector2(50f, 50f), (index + 1).ToString());
        key.gameObject.AddComponent<Outline>().effectColor = WoodDark;

        Text nameText = CardText(root, "Name", 40, Color.white, new Vector2(0f, 135f), new Vector2(270f, 56f), "Skill Name");
        nameText.resizeTextForBestFit = true; // ชื่อยาวย่อเอง
        nameText.resizeTextMinSize = 22;
        nameText.resizeTextMaxSize = 40;
        nameText.horizontalOverflow = HorizontalWrapMode.Overflow;
        CardShadow(nameText);
        Text descText = CardText(root, "Desc", 23, Color.white, new Vector2(0f, 20f), new Vector2(260f, 150f), "คำอธิบายสกิล");
        descText.fontStyle = FontStyle.Normal;
        descText.lineSpacing = 1.15f;
        descText.alignment = TextAnchor.UpperCenter;
        CardShadow(descText);
        Text statText = CardText(root, "Stat", 26, Color.white, new Vector2(0f, -80f), new Vector2(280f, 80f), "ดาเมจ 20");
        CardShadow(statText);
        Text levelText = CardText(root, "Level", 30, CardGold, new Vector2(0f, -185f), new Vector2(300f, 80f), "NEW!");
        CardShadow(levelText);

        UpgradeCardMotion motion = root.gameObject.AddComponent<UpgradeCardMotion>();

        // ลากของในการ์ดใส่ช่อง cards[i] ของ UpgradeUI
        card.FindPropertyRelative("root").objectReferenceValue = root.gameObject;
        card.FindPropertyRelative("cloth").objectReferenceValue = cloth;
        card.FindPropertyRelative("glow").objectReferenceValue = glow;
        card.FindPropertyRelative("motion").objectReferenceValue = motion;
        card.FindPropertyRelative("icon").objectReferenceValue = icon;
        card.FindPropertyRelative("iconLabel").objectReferenceValue = iconLabel;
        card.FindPropertyRelative("nameText").objectReferenceValue = nameText;
        card.FindPropertyRelative("levelText").objectReferenceValue = levelText;
        card.FindPropertyRelative("statText").objectReferenceValue = statText;
        card.FindPropertyRelative("descText").objectReferenceValue = descText;
    }

    private static Image CardImage(Transform parent, string objName, Sprite sprite, Vector2 pos, Vector2 size)
    {
        RectTransform rt = Rect(parent, objName, new Vector2(0.5f, 0.5f), pos, size);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = false;
        if (sprite == null) img.enabled = false; // ไม่มีรูปก็ซ่อน
        return img;
    }

    private static Text CardText(Transform parent, string objName, int fontSize, Color color, Vector2 pos, Vector2 boxSize, string value)
    {
        Text text = Label(parent, objName, new Vector2(0.5f, 0.5f), pos, boxSize, value, fontSize, color);
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static void CardShadow(Text text)
    {
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.15f, 0.07f, 0.02f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);
    }
    #endregion

    #region 4. BossCutscene
    public static void BakeBossCutscene(BossCutscene cutscene)
    {
        SerializedObject so = new SerializedObject(cutscene);
        if (so.FindProperty("introCanvas").objectReferenceValue != null)
        {
            Debug.Log("BossCutscene มี UI แล้ว ข้าม");
            return;
        }
        Transform root = CreateCanvas("BossIntroCanvas", cutscene.transform, 60, false);

        // 1. ขอบจอมืดแดง (รูปเซฟเป็นไฟล์ไว้ ไม่ต้องสร้างใหม่ทุกครั้งที่เล่น)
        RectTransform vrt = Rect(root, "Vignette", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(vrt);
        RawImage vignette = vrt.gameObject.AddComponent<RawImage>();
        vignette.texture = LoadOrCreateVignette();
        vignette.color = new Color(0.12f, 0f, 0f, 0.85f);
        vignette.raycastTarget = false;

        // 2. แฟลช (ใส ตอนเล่นโค้ดเปลี่ยนสีเอง)
        RectTransform frt = Rect(root, "Flash", new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Stretch(frt);
        Image flash = frt.gameObject.AddComponent<Image>();
        flash.color = new Color(1f, 1f, 1f, 0f);
        flash.raycastTarget = false;

        // 3. แถบดำบน/ล่าง (สูง 0 ตอนเริ่ม โค้ดยืดขึ้นตามช่อง Bar Height)
        RectTransform top = Bar(root, "TopBar", 1f);
        RectTransform bottom = Bar(root, "BottomBar", 0f);

        // 4. ชื่อบอส ชิดล่างไม่บังหน้าบอส (สีที่ตั้งตรงนี้ = สีตอนเล่น)
        Text title = BossText(root, "Title", 96, new Vector2(0f, 260f), "ORC WARLORD", new Color(1f, 0.85f, 0.4f));
        Text subtitle = BossText(root, "Subtitle", 36, new Vector2(0f, 185f), "เจ้าแห่งบัลลังก์โลหิต", new Color(1f, 0.6f, 0.55f));

        Set(so, "introCanvas", root.gameObject);
        Set(so, "topBar", top);
        Set(so, "bottomBar", bottom);
        Set(so, "title", title);
        Set(so, "subtitle", subtitle);
        Set(so, "vignette", vignette);
        Set(so, "flash", flash);
        so.ApplyModifiedPropertiesWithoutUndo();
        root.gameObject.SetActive(false); // เปิดเองตอนบอสเกิด
        Debug.Log("สร้าง BossIntroCanvas แล้ว (ปิดไว้)");
    }

    private static RectTransform Bar(Transform parent, string objName, float anchorY)
    {
        RectTransform rt = Rect(parent, objName, new Vector2(0.5f, anchorY), Vector2.zero, Vector2.zero);
        rt.anchorMin = new Vector2(0f, anchorY);
        rt.anchorMax = new Vector2(1f, anchorY);
        rt.pivot = new Vector2(0.5f, anchorY);
        rt.sizeDelta = Vector2.zero;
        rt.gameObject.AddComponent<Image>().color = Color.black;
        return rt;
    }

    private static Text BossText(Transform parent, string objName, int fontSize, Vector2 pos, string value, Color color)
    {
        Text text = Label(parent, objName, new Vector2(0.5f, 0f), pos, new Vector2(1400f, 130f), value, fontSize, color);
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.25f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(3f, -3f);
        return text;
    }

    // รูปขอบจอมืด: ตรงกลางใส ขอบทึบ (วาดด้วยโค้ดครั้งเดียวแล้วเซฟเป็นไฟล์)
    private static Texture2D LoadOrCreateVignette()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(VignettePath);
        if (tex != null) return tex;
        const int size = 128;
        tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2f - 1f;
                float dy = (y + 0.5f) / size * 2f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.2f, d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        AssetDatabase.CreateAsset(tex, VignettePath);
        return tex;
    }
    #endregion

    #region Helpers
    private static Transform CreateCanvas(string objName, Transform parent, int sortingOrder, bool raycaster)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        if (raycaster) obj.AddComponent<GraphicRaycaster>(); // มีปุ่มให้กด
        obj.transform.SetParent(parent, false);
        Canvas canvas = obj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        CanvasScaler scaler = obj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return obj.transform;
    }

    private static RectTransform Rect(Transform parent, string objName, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(objName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform Panel(Transform parent, string objName, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
    {
        RectTransform rt = Rect(parent, objName, anchor, pos, size);
        Image image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rt;
    }

    private static Text Label(Transform parent, string objName, Vector2 anchor, Vector2 pos, Vector2 size, string value, int fontSize, Color color)
    {
        RectTransform rt = Rect(parent, objName, anchor, pos, size);
        Text text = rt.gameObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string objName, string label, Vector2 pos)
    {
        RectTransform rt = Panel(parent, objName, new Vector2(0.5f, 0.5f), pos, new Vector2(360f, 62f), new Color(0.16f, 0.12f, 0.08f, 0.95f));
        rt.GetComponent<Image>().raycastTarget = true;
        Button button = rt.gameObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.6f);
        button.colors = colors;
        Label(rt, "Label", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 62f), label, 24, Ivory);
        return button;
    }

    private static void AddShadow(Text text)
    {
        Shadow shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
        shadow.effectDistance = new Vector2(2f, -3f);
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void Set(SerializedObject so, string field, Object value)
    {
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("ไม่พบช่อง " + field + " ใน " + so.targetObject.GetType().Name);
            return;
        }
        prop.objectReferenceValue = value;
    }

    private static Sprite LoadSprite(string fileName)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArtFolder + fileName + ".png");
        if (sprite == null) Debug.LogWarning("ไม่พบรูป " + ArtFolder + fileName + ".png");
        return sprite;
    }
    #endregion
}
