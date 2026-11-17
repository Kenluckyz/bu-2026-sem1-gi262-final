using UnityEngine;
using UnityEngine.UI;

// ช่องคูลดาวน์ Shift หน้าตาเหมือนช่องสกิล
// ตัวช่องอยู่ในซีนแล้ว (HUD > DodgeSlot) ปิดอยู่ ถ้าอยากโชว์ให้ติ๊ก DodgeSlot ใน Hierarchy
// สคริปต์นี้แค่หมุนวงคูลดาวน์กับตัวเลข
public class DodgeCooldownUI : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image mask;        // วงมืดแบบ Filled Radial 360
    [SerializeField] private Text cooldownText; // ตัวเลขวินาทีที่เหลือ
    [SerializeField] private Text label;        // คำว่า DODGE ตอนพร้อมกด

    private void Start()
    {
        if (player == null) player = FindAnyObjectByType<Player>();
        if (player == null) Debug.LogWarning("DodgeCooldownUI: ไม่พบ Player");
    }

    private void Update()
    {
        // ไม่มีช่อง / ช่องปิดอยู่ = ไม่ต้องทำอะไร
        if (player == null || mask == null || !mask.gameObject.activeInHierarchy) return;
        float remaining = player.DodgeCooldownRemaining;
        mask.fillAmount = player.DodgeCooldown > 0f ? remaining / player.DodgeCooldown : 0f;
        cooldownText.text = remaining > 0f ? remaining.ToString("0.0") : "";
        label.enabled = remaining <= 0f; // พร้อมกดโชว์ DODGE
    }
}
