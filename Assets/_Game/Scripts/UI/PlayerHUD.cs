using UnityEngine;
using UnityEngine.UI;

// แถบเลือด แถบ EXP และเลเวลของผู้เล่น — อัปเดตเมื่อมี event เท่านั้น ไม่เช็กทุกเฟรม
public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private Image hpFill;   // Image Type = Filled
    [SerializeField] private Image expFill;
    [SerializeField] private Text hpText;
    [SerializeField] private Text levelText;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogWarning("PlayerHUD: ยังไม่ได้ลาก Player ใส่ช่อง Player");
            return;
        }

        player.OnHPChanged += UpdateHP;
        player.Stats.OnExpChanged += UpdateExp;
        player.Stats.OnLevelUp += UpdateLevel;

        // ค่าเริ่มต้น
        UpdateHP(player.CurrentHP, player.MaxHP);
        UpdateExp(player.Stats.Exp, player.Stats.ExpToNextLevel);
        UpdateLevel(player.Stats.Level);
    }

    private void OnDestroy()
    {
        if (player == null) return;
        player.OnHPChanged -= UpdateHP;
        player.Stats.OnExpChanged -= UpdateExp;
        player.Stats.OnLevelUp -= UpdateLevel;
    }

    private void UpdateHP(float current, float max)
    {
        if (hpFill != null) hpFill.fillAmount = max > 0f ? current / max : 0f;
        if (hpText != null) hpText.text = Mathf.CeilToInt(current) + " / " + Mathf.CeilToInt(max);
    }

    private void UpdateExp(int current, int needed)
    {
        if (expFill != null) expFill.fillAmount = needed > 0 ? (float)current / needed : 0f;
    }

    private void UpdateLevel(int level)
    {
        if (levelText != null) levelText.text = "Lv. " + level;
    }
}
