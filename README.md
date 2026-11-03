# BOAT VS BOSS

Action RPG เล่นคนเดียว บน PC · 3D Stylized Chibi Fantasy · กล้องมุมเฉียงจากด้านบน · ดันเจี้ยนแบบแบ่งห้อง
Final Project GI262 — กลุ่ม รถขนฝัน

- Unity **6000.5.5f1** (URP) — ต้องเปิดด้วยเวอร์ชันนี้เท่านั้น
- Input System 1.19

## สมาชิกและงาน
| คน | ระบบ | คลาสหลัก | ฉากทดสอบ |
|---|---|---|---|
| 1 Keerati | Player & Skill, HUD (HP/EXP/Cooldown) | Character, Player, PlayerStats, Skill | `Test_Player` |
| 2 Natthaphon | Enemy & Combat, Boss, Boss HP Bar | Enemy, FlyEnemy, SpeedEnemy, FatEnemy, Boss, EnemyManager, EnemyData | `Test_Enemy` |
| 3 Chanatip | Dungeon, Wave, Upgrade, Menu/GameOver/Victory, รวมงาน | GameManager, DungeonManager, Room, UpgradeManager, UpgradeData | `Test_Dungeon`, `Main` |

**กฎฉาก:** แก้เฉพาะฉากของตัวเอง ฉาก `Main` คนที่ 3 แก้คนเดียว ส่งงานกันเป็น Prefab

## ชื่อที่ตกลงร่วมกัน (ห้ามเปลี่ยนโดยไม่บอกกลุ่ม)
```csharp
// Character (คลาสแม่)
public virtual void TakeDamage(float damage);
protected virtual void Die();
public event System.Action<Character> OnDied;

// PlayerStats
public void AddExp(int amount);
public event System.Action<int> OnLevelUp;   // ส่ง level ใหม่

// EnemyManager
public event System.Action OnAllDefeated;
public void SpawnEnemy(EnemyData data, Vector3 position);

// UpgradeManager
public List<UpgradeData> GetOptions(Player player, int count);
public void ApplyUpgrade(UpgradeData upgrade, Player player);
```

## โครงสร้างโฟลเดอร์
```
Assets/_Game/
  Scripts/Core  Player  Enemy  Dungeon  Upgrade  UI
  Prefabs/  Scenes/  Data/Enemies  Data/Upgrades  Data/Skills
  Materials/  Art/  Audio/
```

## Git ประจำวัน
```bash
git pull              # ก่อนเริ่มงานทุกครั้ง
git add .
git commit -m "Add Player Movement"
git push
```
