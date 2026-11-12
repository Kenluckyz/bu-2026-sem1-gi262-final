using System.Collections.Generic;
using UnityEngine;

// เงาร่างตัวเอง (afterimage) ตอนกด Shift หลบ: ถ่ายท่าปัจจุบันของโมเดลเป็น mesh นิ่ง ๆ แล้วค่อย ๆ จางหาย
// ใช้ Queue เก็บเงาที่จางหมดแล้วไว้ใช้ซ้ำ (Object Pool แบบ VFXManager) ไม่ต้องสร้าง/ลบทุกครั้ง
public class DashAfterImage : MonoBehaviour
{
    private static Queue<DashAfterImage> pool = new Queue<DashAfterImage>();
    private static Material sharedMaterial;

    private List<MeshFilter> parts = new List<MeshFilter>();
    private MaterialPropertyBlock block;
    private Color color;
    private float lifeTime;
    private float timer;

    // ถ่ายเงา 1 ร่างจากทุก SkinnedMeshRenderer ใต้ source
    public static void Spawn(Transform source, Color tint, float life)
    {
        SkinnedMeshRenderer[] skins = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skins.Length == 0)
        {
            Debug.LogWarning("AfterImage: ไม่พบ SkinnedMeshRenderer ใต้ " + source.name);
            return;
        }

        DashAfterImage ghost = null;
        while (ghost == null && pool.Count > 0) ghost = pool.Dequeue(); // ข้ามตัวที่หายไปตอนโหลดซีนใหม่
        if (ghost == null) ghost = Create();
        ghost.gameObject.SetActive(true);
        ghost.Bake(skins);
        ghost.color = tint;
        ghost.lifeTime = life;
        ghost.timer = 0f;
        ghost.ApplyColor(tint.a);
    }


    private static DashAfterImage Create()
    {
        if (sharedMaterial == null)
        {
            sharedMaterial = CreateGhostMaterial();
        }
        GameObject obj = new GameObject("DashAfterImage");
        DashAfterImage ghost = obj.AddComponent<DashAfterImage>();
        ghost.block = new MaterialPropertyBlock();
        return ghost;
    }

    // วัสดุโปร่งใสไม่รับแสง: ใช้ URP Unlit ตั้งเป็น Transparent (ถ้าไม่มีใช้ Sprites/Default แทน)
    private static Material CreateGhostMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            return new Material(Shader.Find("Sprites/Default"));
        }
        Material mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);                 // 1 = Transparent
        mat.SetFloat("_Blend", 0f);                   // Alpha blend
        mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0f);
        mat.SetFloat("_Cull", 0f);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return mat;
    }

    private void Bake(SkinnedMeshRenderer[] skins)
    {
        // เพิ่มชิ้นให้พอกับจำนวนชิ้นของโมเดล
        while (parts.Count < skins.Length)
        {
            GameObject part = new GameObject("Part", typeof(MeshFilter), typeof(MeshRenderer));
            part.transform.SetParent(transform, false);
            MeshFilter filter = part.GetComponent<MeshFilter>();
            filter.sharedMesh = new Mesh();
            MeshRenderer renderer = part.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = sharedMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            parts.Add(filter);
        }
        for (int i = 0; i < parts.Count; i++)
        {
            bool used = i < skins.Length;
            parts[i].gameObject.SetActive(used);
            if (!used) continue;
            skins[i].BakeMesh(parts[i].sharedMesh, false); // false: ไม่คูณ scale ซ้ำ (โมเดล FBX scale 0.01 คูณแล้วเงาเล็กจิ๋ว)
            parts[i].sharedMesh.RecalculateBounds();      // ไม่คำนวณกรอบใหม่ = กรอบขนาด 0 กล้องตัดทิ้ง มองไม่เห็น
            parts[i].transform.SetPositionAndRotation(skins[i].transform.position, skins[i].transform.rotation);
            parts[i].transform.localScale = Vector3.one; // BakeMesh(false) ได้ขนาดจริงอยู่แล้ว
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifeTime;
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            pool.Enqueue(this); // จางหมดแล้ว เก็บไว้ใช้รอบหน้า
            return;
        }
        ApplyColor(color.a * (1f - t));
    }

    private void ApplyColor(float alpha)
    {
        Color c = new Color(color.r, color.g, color.b, alpha);
        block.SetColor("_BaseColor", c);      // URP Unlit
        block.SetColor("_Color", c);          // Sprites/Default
        block.SetColor("_RendererColor", Color.white);
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
    }
}
