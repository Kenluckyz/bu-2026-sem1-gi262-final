using System.Collections;
using System.Collections.Generic;
using NightForge;
using UnityEngine;

// เล่นเอฟเฟกต์สกิล ใช้ซ้ำได้ไม่ต้องสร้างใหม่ทุกครั้ง (object pool)
// Dictionary + Queue เก็บเอฟเฟกต์ที่เล่นจบแล้ว
public class VFXManager : MonoBehaviour
{
    private static VFXManager instance;
    private Dictionary<GameObject, Queue<NFEffect>> pool = new Dictionary<GameObject, Queue<NFEffect>>();

    // ไม่มีในฉากก็สร้างเอง
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

    // เล่นเอฟเฟกต์ ปรับความเร็วให้จบใน playTime
    // follow = ให้ตามตัว
    public static void Play(GameObject prefab, Vector3 position, Quaternion rotation, float scale, float playTime, Transform follow = null)
    {
        if (prefab == null) return;
        Instance.Spawn(prefab, position, rotation, scale, playTime, follow);
    }

    // สร้างรอไว้ก่อน กดครั้งแรกจะได้ไม่กระตุก
    // เล่นเฟรมสุดท้ายที่มองไม่เห็นไปก่อน 1 ที
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
        // 1. มีคิวยัง
        if (!pool.ContainsKey(prefab))
        {
            pool.Add(prefab, new Queue<NFEffect>());
        }
        // 2. มีในคิวก็เอามาใช้ ไม่มีค่อยสร้าง
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
        // ปรับ speed ให้จบพอดี
        vfx.speed = playTime > 0f && vfx.Duration > 0f ? vfx.Duration / playTime : 1f;
        vfx.Play();

        StartCoroutine(ReturnToPool(prefab, vfx, playTime > 0f ? playTime : vfx.Duration));
    }

    // เล่นจบแล้วเก็บคืนคิว
    // ไม่ปิด object เพราะเปิดใหม่มันสร้าง mesh ใหม่ แลค
    private IEnumerator ReturnToPool(GameObject prefab, NFEffect vfx, float time)
    {
        yield return new WaitForSeconds(time);
        vfx.Stop();
        vfx.transform.SetParent(transform, true);
        pool[prefab].Enqueue(vfx);
    }
}
