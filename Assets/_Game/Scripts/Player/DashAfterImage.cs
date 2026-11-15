using System.Collections.Generic;
using UnityEngine;

// เงาร่างตอนหลบ ถ่ายท่าตอนนั้นเป็น mesh แล้วค่อยๆ จาง
// ใช้ Queue เก็บเงาไว้ใช้ซ้ำ
public class DashAfterImage : MonoBehaviour
{
    private static Queue<DashAfterImage> pool = new Queue<DashAfterImage>();
    private static Material sharedMaterial;

    private List<MeshFilter> parts = new List<MeshFilter>();
    private MaterialPropertyBlock block;
    private Color color;
    private float lifeTime;
    private float timer;

    // ถ่ายเงา 1 อัน
    public static void Spawn(Transform source, Color tint, float life)
    {
        SkinnedMeshRenderer[] skins = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (skins.Length == 0)
        {
            Debug.LogWarning("AfterImage: ไม่พบ SkinnedMeshRenderer ใต้ " + source.name);
            return;
        }

        DashAfterImage ghost = null;
        while (ghost == null && pool.Count > 0) ghost = pool.Dequeue(); // ข้ามตัวที่หายไปตอนเปลี่ยนฉาก
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

    // material โปร่งใส
    private static Material CreateGhostMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            return new Material(Shader.Find("Sprites/Default"));
        }
        Material mat = new Material(shader);
        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
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
        // ชิ้นไม่พอก็เพิ่ม
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
            skins[i].BakeMesh(parts[i].sharedMesh, false); // false ไม่งั้น scale ซ้ำ เงาเล็กมาก
            parts[i].sharedMesh.RecalculateBounds(); // ไม่ทำแล้วมองไม่เห็น
            parts[i].transform.SetPositionAndRotation(skins[i].transform.position, skins[i].transform.rotation);
            parts[i].transform.localScale = Vector3.one;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / lifeTime;
        if (t >= 1f)
        {
            gameObject.SetActive(false);
            pool.Enqueue(this); // จางหมดแล้วเก็บเข้าคิว
            return;
        }
        ApplyColor(color.a * (1f - t));
    }

    private void ApplyColor(float alpha)
    {
        Color c = new Color(color.r, color.g, color.b, alpha);
        block.SetColor("_BaseColor", c);
        block.SetColor("_Color", c);
        block.SetColor("_RendererColor", Color.white);
        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].GetComponent<MeshRenderer>().SetPropertyBlock(block);
        }
    }
}
