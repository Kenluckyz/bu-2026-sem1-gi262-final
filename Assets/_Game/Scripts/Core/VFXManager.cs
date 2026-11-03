using System.Collections;
using System.Collections.Generic;
using NightForge;
using UnityEngine;

// เล่นเอฟเฟกต์สกิล 3D (Export 3D จาก Game Mellow VFX Lab) แบบ Object Pool ไม่สร้าง/ลบใหม่ทุกครั้ง เกมจะได้ไม่แลค
// prefab -> คิวเอฟเฟกต์ที่เล่นจบแล้วรอใช้ซ้ำ (Dictionary แบบ Inventory week04 + Queue แบบ ActionHistoryManager)
public class VFXManager : MonoBehaviour
{
    private static VFXManager instance;
    private Dictionary<GameObject, Queue<NFEffect>> pool = new Dictionary<GameObject, Queue<NFEffect>>();

    // มีตัวเดียวทั้งเกม ยังไม่มีก็สร้างเอง ไม่ต้องลากใส่ฉาก
    private static VFXManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new GameObject("VFXManager").AddComponent<VFXManager>();
            }
            return instance;
        }
    }

    // เล่นเอฟเฟกต์ที่ position หันตาม rotation ขนาด scale ให้จบใน playTime วินาที (เร่ง/ชะลอให้พอดีสกิล)
    // follow != null = เอฟเฟกต์ติดตามตัวนั้น (เช่น ตัวผู้เล่นตอนพุ่ง)
    public static void Play(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float playTime, Transform follow = null)
    {
        if (prefab == null) return;
        Instance.Spawn(prefab, position, rotation, scale, playTime, follow);
    }

    // สร้างเอฟเฟกต์เตรียมไว้ในคิวตั้งแต่ได้สกิล กันกระตุกตอนกดครั้งแรก (สร้าง mesh + โหลด shader ครั้งแรกหนัก)
    // เล่นที่เฟรมสุดท้าย (จางหายแล้ว มองไม่เห็น) ตรงจุด near ให้กล้องวาด 1 ครั้ง shader จะพร้อมใช้
    public static void Prewarm(GameObject prefab, Vector3 near)
    {
        if (prefab == null) return;
        VFXManager m = Instance;
        if (m.pool.ContainsKey(prefab)) return;
        m.pool.Add(prefab, new Queue<NFEffect>());
        NFEffect vfx = Instantiate(prefab, near, Quaternion.identity, m.transform).GetComponent<NFEffect>();
        vfx.loop = false;
        vfx.Play(vfx.Duration);
        m.pool[prefab].Enqueue(vfx);
    }

    private void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float playTime, Transform follow)
    {
        // 1. ตรวจสอบว่ามีคิวของเอฟเฟกต์นี้หรือยัง
        if (!pool.ContainsKey(prefab))
        {
            pool.Add(prefab, new Queue<NFEffect>());
        }
        // 2. มีตัวว่างในคิวก็หยิบมาใช้ ไม่มีค่อยสร้างใหม่
        NFEffect vfx;
        if (pool[prefab].Count > 0)
        {
            vfx = pool[prefab].Dequeue();
        }
        else
        {
            vfx = Instantiate(prefab).GetComponent<NFEffect>();
            vfx.loop = false;
        }

        vfx.transform.SetParent(follow, true);
        vfx.transform.SetPositionAndRotation(position, rotation);
        vfx.transform.localScale = Vector3.one * scale;
        // ความยาวเดิมของเอฟเฟกต์ / เวลาที่อยากให้จบ = ความเร็วที่ต้องเล่น
        vfx.speed = playTime > 0f && vfx.Duration > 0f ? vfx.Duration / playTime : 1f;
        vfx.Play();

        StartCoroutine(ReturnToPool(prefab, vfx, playTime > 0f ? playTime : vfx.Duration));
    }

    // เล่นจบแล้ว (ค้างที่เฟรมสุดท้ายซึ่งจางหายแล้ว) คืนเข้าคิว
    // ไม่ปิด GameObject เพราะเปิดใหม่ NFEffect จะสร้าง mesh ใหม่ทั้งหมด = แลค
    private IEnumerator ReturnToPool(GameObject prefab, NFEffect vfx, float time)
    {
        yield return new WaitForSeconds(time);
        vfx.Stop();
        vfx.transform.SetParent(transform, true);
        pool[prefab].Enqueue(vfx);
    }
}
