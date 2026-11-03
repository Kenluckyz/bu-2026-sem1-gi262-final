using UnityEngine;
using UnityEngine.UI;

// แถบสกิลด้านล่าง 3 ช่อง ตามปุ่ม 1 / 2 / 3
public class SkillBarUI : MonoBehaviour
{
    [System.Serializable]
    public class SlotUI
    {
        public Image icon;
        public Text nameText;      // ใช้แทนไอคอนตอนยังไม่มีรูป
        public Image cooldownMask; // Image Type = Filled, Radial 360
        public Text cooldownText;
        public Text levelText;
    }

    [SerializeField] private Player player;
    [SerializeField] private SlotUI[] slots = new SlotUI[3];

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("SkillBarUI: ยังไม่ได้ลาก Player ใส่ช่อง Player");
            return;
        }
        player.OnSkillsChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (player != null) player.OnSkillsChanged -= Refresh;
    }

    // Cooldown เปลี่ยนทุกเฟรม เลยต้องอัปเดตใน Update (ส่วนชื่อ/ไอคอนอัปเดตผ่าน event)
    private void Update()
    {
        if (player == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            ActiveSkill skill = i < player.SkillSlots.Count ? player.SkillSlots[i] as ActiveSkill : null;
            SlotUI slot = slots[i];
            if (slot == null || slot.cooldownMask == null) continue;

            float remain = skill != null ? skill.CooldownRemaining : 0f;
            float total = skill != null && skill.Data.cooldown > 0f ? skill.Data.cooldown : 1f;
            slot.cooldownMask.fillAmount = remain / total;
            if (slot.cooldownText != null) slot.cooldownText.text = remain > 0f ? remain.ToString("0.0") : "";
        }
    }

    private void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            SetSlot(slots[i], i < player.SkillSlots.Count ? player.SkillSlots[i] : null);
        }
    }

    private void SetSlot(SlotUI slot, Skill skill)
    {
        if (slot == null) return;
        bool hasIcon = skill != null && skill.Data.icon != null;
        if (slot.icon != null)
        {
            slot.icon.enabled = hasIcon;
            if (hasIcon) slot.icon.sprite = skill.Data.icon;
        }
        if (slot.nameText != null) slot.nameText.text = skill != null && !hasIcon ? skill.Name : "";
        if (slot.levelText != null) slot.levelText.text = skill != null ? "Lv." + skill.Level : "";
        if (slot.cooldownMask != null) slot.cooldownMask.fillAmount = 0f;
    }
}
