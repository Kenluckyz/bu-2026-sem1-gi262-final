using UnityEngine;
using UnityEngine.UI;

// แถบเลือดลอยเหนือหัวศัตรู — ฟัง OnHPChanged ของ Character แล้วย่อแถบแดง
// เหนือแถบมีป้าย "Lv.2 Goblin" บอกเลเวลกับชื่อ (สร้างในโค้ด ไม่ต้องแก้ Prefab)
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Character owner;
    [SerializeField] private RectTransform fill; // pivot ซ้าย ย่อด้วย scale.x

    [Header("ป้ายเลเวล + ชื่อ")]
    [SerializeField] private bool showName = true;
    [SerializeField] private int fontSize = 40;
    [SerializeField] private Color levelColor = new Color(1f, 0.82f, 0.29f); // สีทอง
    [SerializeField] private Color nameColor = Color.white;

    private Camera cam;
    private Text nameLabel;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<Character>();
        if (owner == null) return;
        owner.OnHPChanged += UpdateBar;
        owner.OnDied += HandleDied;
    }

    // Start: ทำหลัง EnemyManager เรียก Init (ได้ชื่อกับเลเวลแล้ว)
    private void Start()
    {
        if (showName) CreateNameLabel();
    }

    private void OnDestroy()
    {
        if (owner == null) return;
        owner.OnHPChanged -= UpdateBar;
        owner.OnDied -= HandleDied;
    }

    private void UpdateBar(float current, float max)
    {
        if (fill == null) return;
        float ratio = max > 0f ? current / max : 0f;
        fill.localScale = new Vector3(ratio, 1f, 1f);
    }

    private void HandleDied(Character c)
    {
        gameObject.SetActive(false);
    }

    // หันแถบเข้าหากล้องเสมอ
    private void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;
    }

    #region Name Label
    // ข้อความที่จะโชว์: ศัตรู = "Lv.เลเวล ชื่อ" / Character อื่น (เช่น หุ่นซ้อม) = ชื่ออย่างเดียว
    public string GetLabelText()
    {
        if (owner == null) return "";
        string nameText = "<color=#" + ColorUtility.ToHtmlStringRGB(nameColor) + ">" + owner.characterName + "</color>";
        Enemy enemy = owner as Enemy;
        if (enemy == null) return nameText;
        string levelText = "<color=#" + ColorUtility.ToHtmlStringRGB(levelColor) + ">Lv." + enemy.Level + "</color>";
        return levelText + " " + nameText;
    }

    // 1. สร้าง Text เป็นลูกของแถบเลือด วางเหนือแถบ
    // 2. ใช้ตัวหนังสือใหญ่แล้วย่อ scale ลง ตัวอักษรใน World Space จะคมกว่าใช้ fontSize เล็ก ๆ
    private void CreateNameLabel()
    {
        RectTransform bar = (RectTransform)transform;
        GameObject obj = new GameObject("NameLabel", typeof(RectTransform));
        obj.transform.SetParent(transform, false);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f); // กลางขอบบนของแถบ
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 2f);
        rt.sizeDelta = new Vector2(bar.sizeDelta.x * 4f, fontSize * 1.4f);
        rt.localScale = Vector3.one * 0.3f;

        nameLabel = obj.AddComponent<Text>();
        nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameLabel.fontSize = fontSize;
        nameLabel.fontStyle = FontStyle.Bold;
        nameLabel.alignment = TextAnchor.LowerCenter;
        nameLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
        nameLabel.supportRichText = true;
        nameLabel.raycastTarget = false;
        nameLabel.text = GetLabelText();

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(2f, -2f);
    }
    #endregion
}
