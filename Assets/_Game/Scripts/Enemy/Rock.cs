using UnityEngine;

// ลูกหินที่โครงกระดูกปา — บินเป็นวิถีโค้ง (parabola) จากมือไปหาตำแหน่งเป้า
// โดนผู้เล่นระหว่างทาง = ทำดาเมจแล้วหายไป / ถึงพื้นแล้วไม่โดน = หายไปเฉย ๆ
public class Rock : MonoBehaviour
{
    [Header("Rock")]
    public float hitRadius = 0.6f;     // ระยะที่ถือว่าโดนผู้เล่น
    public float spinSpeed = 720f;     // หมุนกลางอากาศ (องศา/วินาที)

    private Vector3 startPos;
    private Vector3 endPos;
    private float flightTime;
    private float arcHeight;
    private float damage;
    private Player target;
    private float timer;
    private bool isFlying;

    // โครงกระดูกเรียกตอนปล่อยหิน
    public void Launch(Vector3 from, Vector3 to, float time, float height, float rockDamage, Player player)
    {
        startPos = from;
        endPos = to;
        flightTime = Mathf.Max(0.1f, time);
        arcHeight = height;
        damage = rockDamage;
        target = player;
        timer = 0f;
        isFlying = true;
    }

    private void Update()
    {
        if (!isFlying) return;

        // 1. คำนวณตำแหน่งบนวิถีโค้ง: เส้นตรงจากต้นไปปลาย + ยกขึ้นเป็นรูปโค้ง 4t(1-t)
        timer += Time.deltaTime;
        float t = timer / flightTime;
        Vector3 pos = Vector3.Lerp(startPos, endPos, t);
        pos.y += arcHeight * 4f * t * (1f - t);
        transform.position = pos;
        transform.Rotate(Vector3.right, spinSpeed * Time.deltaTime);

        // 2. เช็กว่าโดนผู้เล่นหรือยัง (วัดจากกลางตัวผู้เล่น)
        if (target != null && target.IsAlive)
        {
            Vector3 playerCenter = target.transform.position + Vector3.up * 0.8f;
            if (Vector3.Distance(pos, playerCenter) <= hitRadius)
            {
                target.TakeDamage(damage);
                Debug.Log("Rock hit " + target.characterName + " : " + damage);
                Destroy(gameObject);
                return;
            }
        }

        // 3. บินเลยจุดหมายแล้ว (ตกพื้น) ไม่โดนใคร ทำลายทิ้ง
        if (t >= 1.2f)
        {
            Destroy(gameObject);
        }
    }
}
