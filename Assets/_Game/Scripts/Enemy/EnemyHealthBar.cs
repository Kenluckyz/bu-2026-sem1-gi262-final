using UnityEngine;

// แถบเลือดลอยเหนือหัวศัตรู — ฟัง OnHPChanged ของ Character แล้วย่อแถบแดง
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Character owner;
    [SerializeField] private RectTransform fill; // pivot ซ้าย ย่อด้วย scale.x

    private Camera cam;

    private void Awake()
    {
        if (owner == null) owner = GetComponentInParent<Character>();
        if (owner == null) return;
        owner.OnHPChanged += UpdateBar;
        owner.OnDied += HandleDied;
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
}
