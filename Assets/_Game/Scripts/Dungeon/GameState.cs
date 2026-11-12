// สถานะของเกม (Enumeration ตาม Diagram) — GameManager เก็บไว้ว่าตอนนี้อยู่ช่วงไหน
public enum GameState
{
    Playing,   // กำลังเล่น
    Paused,    // กด Esc หยุดเกม
    Victory,   // ฆ่าบอสได้ = ผ่านดันเจี้ยน
    GameOver   // Boat ตาย
}
