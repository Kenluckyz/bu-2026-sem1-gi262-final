using UnityEngine;

// ข้อมูลศัตรูแต่ละแบบ (ScriptableObject ตาม Diagram) — ก็อบลิน/โกเลม/สไลม์/โครงกระดูก/บอส ใช้คลาสนี้ร่วมกัน ต่างกันแค่ค่า
// สร้างใหม่: คลิกขวาใน Project > Create > BoatVsBoss > Enemy Data
[CreateAssetMenu(fileName = "NewEnemyData", menuName = "BoatVsBoss/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Info")]
    public string enemyName = "Enemy";
    [Tooltip("Prefab ที่ EnemyManager.SpawnEnemy() จะสร้าง (ต้องมีคอมโพเนนต์ Enemy หรือคลาสลูก)")]
    public Enemy prefab;

    [Header("Stats (ตาม Diagram)")]
    public float maxHP = 50f;
    public float damage = 10f;
    public int expDrop = 10;
    public float modelScale = 1f;

    [Header("Movement")]
    public float moveSpeed = 3f;
    [Tooltip("ผู้เล่นเข้ามาใกล้กว่านี้ถึงจะเริ่มไล่")]
    public float detectRange = 12f;

    [Header("Attack")]
    [Tooltip("ระยะที่หยุดเดินแล้วเริ่มตี")]
    public float attackRange = 1.5f;
    [Tooltip("พักหลังตีจบ ก่อนตีครั้งถัดไป (วินาที)")]
    public float attackCooldown = 1f;
    [Tooltip("จังหวะที่ดาเมจออก เป็นสัดส่วนของความยาวท่าตี (0-1)")]
    [Range(0f, 1f)] public float hitPoint = 0.45f;
    [Tooltip("ความเร็วท่าตี (1 = ปกติ) ยิ่งมากยิ่งตีไว")]
    public float attackAnimSpeed = 1f;
    [Tooltip("ชื่อ State ท่าตีใน Animator — สุ่มเล่นทีละท่า")]
    public string[] attackAnims = { "Attack1" };

    [Header("Hurt / Death")]
    [Tooltip("โดนตีแล้วชะงัก (ยกเลิกท่าตี) ไหม — ศัตรูอึด ๆ อย่างโกเลม/บอสควรปิด")]
    public bool canBeStunned = true;
    public float stunTime = 0.35f;
    [Tooltip("ศพค้างกี่วินาทีก่อนหายไป")]
    public float corpseTime = 3f;

    [Header("Split (สไลม์)")]
    [Tooltip("ตายแล้วแตกเป็นศัตรูแบบไหน (ว่าง = ไม่แตก) — สไลม์ใหญ่แตกเป็นสไลม์เล็ก")]
    public EnemyData splitInto;
    [Tooltip("แตกออกมากี่ตัว")]
    public int splitCount = 2;
}
