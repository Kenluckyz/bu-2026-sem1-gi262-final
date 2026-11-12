using UnityEngine;
using UnityEngine.EventSystems;

// ท่าทางของการ์ดอัปเกรด: เลื่อนลงมาตอนเปิด, ชี้แล้วเด้งขึ้น+เรืองแสง
// ใช้ unscaledDeltaTime เพราะตอนเลือกการ์ดเกมหยุด (timeScale = 0)
public class UpgradeCardMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private float hoverLift = 24f;
    [SerializeField] private float hoverScale = 1.06f;
    [SerializeField] private float introDrop = 260f;
    [SerializeField] private float speed = 12f;

    private RectTransform rect;
    private UnityEngine.UI.Image glow;
    private Vector2 basePos;
    private bool hovered;
    private float introTimer;   // ติดลบ = รอคิว (ผืนถัดไปมาช้ากว่า)
    private float offsetY;
    private float glowAlpha;

    public void SetUP(UnityEngine.UI.Image glowImage)
    {
        rect = GetComponent<RectTransform>();
        basePos = rect.anchoredPosition;
        glow = glowImage;
    }

    public void PlayIntro(float delay)
    {
        hovered = false;
        introTimer = -delay;
        offsetY = introDrop;
        glowAlpha = 0f;
        rect.localScale = Vector3.one;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData) { hovered = true; }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; }

    private void Update()
    {
        if (rect == null) return;
        float dt = Time.unscaledDeltaTime;
        introTimer += dt;
        if (introTimer < 0f) { Apply(); return; }

        float targetY = hovered ? hoverLift : 0f;
        offsetY = Mathf.Lerp(offsetY, targetY, 1f - Mathf.Exp(-speed * dt));
        float s = Mathf.Lerp(rect.localScale.x, hovered ? hoverScale : 1f, 1f - Mathf.Exp(-speed * dt));
        rect.localScale = new Vector3(s, s, 1f);
        glowAlpha = Mathf.MoveTowards(glowAlpha, hovered ? 0.85f : 0f, dt * 5f);
        Apply();
    }

    private void Apply()
    {
        rect.anchoredPosition = basePos + new Vector2(0f, offsetY);
        if (glow != null)
        {
            Color c = glow.color;
            c.a = glowAlpha;
            glow.color = c;
        }
    }
}
