using UnityEngine;

// พื้นสีแดงเตือนก่อนศัตรูโจมตี (วงกลม / รูปพัด)
// - พื้นที่ทั้งหมด: แดงจาง + ขอบแดงเข้ม -> ผู้เล่นรู้ว่าต้องหนีออกไปตรงไหน
// - สีแดงเข้มค่อย ๆ ขยายจากกลางจนเต็มวง -> เต็มวงเมื่อไร = โดนตอนนั้น
// สร้าง Mesh เองในโค้ด (ไม่ต้องมี Prefab) ใช้ Material ที่รองรับสีของ Vertex เช่น Sprites/Default
public class AttackIndicator : MonoBehaviour
{
    public static readonly Color Red = new Color(1f, 0.08f, 0.04f, 1f);
    public static readonly Color Blue = new Color(0.3f, 0.65f, 1f, 1f);

    private Transform fill;
    private float duration;
    private float timer;

    // วงกลม (Overload: ไม่ต้องส่งทิศกับมุม)
    public static AttackIndicator Create(Vector3 center, float radius, float duration, Material material)
    {
        return Create(center, Vector3.forward, radius, 360f, duration, material);
    }

    // รูปพัดสีแดง: หันไปทาง forward กว้าง angle องศา
    public static AttackIndicator Create(Vector3 center, Vector3 forward, float radius, float angle, float duration, Material material)
    {
        return Create(center, forward, radius, angle, duration, material, Red);
    }

    // Overload: เลือกสีเองได้ (บอสตั้งโล่ = สีฟ้า บอกว่าด้านไหนตีไม่เข้า)
    public static AttackIndicator Create(Vector3 center, Vector3 forward, float radius, float angle, float duration,
        Material material, Color color)
    {
        // 1. ตัวหลัก วางบนพื้นจริง หันตามทิศโจมตี
        GameObject obj = new GameObject("AttackIndicator");
        obj.transform.position = GroundPoint(center);
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) obj.transform.rotation = Quaternion.LookRotation(forward);

        AttackIndicator indicator = obj.AddComponent<AttackIndicator>();
        indicator.duration = Mathf.Max(duration, 0.01f);

        // 2. พื้นที่ทั้งหมด (จาง) + ขอบ (เข้ม) + ส่วนที่ขยาย (ซ้อนบนสุด)
        CreatePart(obj.transform, "Area", BuildSector(radius, angle, WithAlpha(color, 0.22f)), material, 0f);
        CreatePart(obj.transform, "Edge", BuildEdge(radius, angle, 0.14f, WithAlpha(color, 0.9f)), material, 0.004f);
        indicator.fill = CreatePart(obj.transform, "Fill", BuildSector(radius, angle, WithAlpha(color, 0.45f)), material, 0.008f).transform;
        indicator.fill.localScale = new Vector3(0f, 1f, 0f);
        return indicator;
    }

    // ย้ายวงตามเป้า (ท่ากระโดด: วงตามผู้เล่นจนกว่าบอสจะกระโดด)
    public void MoveTo(Vector3 center)
    {
        transform.position = GroundPoint(center);
    }

    // หมุนตามทิศ (วงตั้งโล่หมุนตามบอส)
    public void FaceTo(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(forward);
    }

    // ยกขึ้นจากพื้นนิดนึง กันจมพื้น/กะพริบซ้อนกับพื้น (center.y = ระดับพื้น ผู้สร้างเป็นคนหาให้)
    private static Vector3 GroundPoint(Vector3 center)
    {
        return new Vector3(center.x, center.y + 0.03f, center.z);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        return new Color(color.r, color.g, color.b, alpha);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        fill.localScale = new Vector3(t, 1f, t);
    }

    private static GameObject CreatePart(Transform parent, string partName, Mesh mesh, Material material, float height)
    {
        GameObject part = new GameObject(partName);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = new Vector3(0f, height, 0f);
        part.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = part.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return part;
    }

    // รูปพัด/วงกลมแบบพัดลม: จุดกลาง 1 จุด + จุดบนขอบโค้ง
    private static Mesh BuildSector(float radius, float angle, Color color)
    {
        int segments = Mathf.Max(8, Mathf.CeilToInt(angle / 6f));
        Vector3[] vertices = new Vector3[segments + 2];
        Color[] colors = new Color[segments + 2];
        int[] triangles = new int[segments * 3];

        vertices[0] = Vector3.zero;
        colors[0] = color;
        for (int i = 0; i <= segments; i++)
        {
            float a = (-angle / 2f + angle * i / segments) * Mathf.Deg2Rad;
            vertices[i + 1] = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
            colors[i + 1] = color;
        }
        for (int i = 0; i < segments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    // ขอบ: แถบโค้งด้านนอก (+ เส้นข้างสองเส้นถ้าเป็นรูปพัด)
    private static Mesh BuildEdge(float radius, float angle, float width, Color color)
    {
        int segments = Mathf.Max(8, Mathf.CeilToInt(angle / 6f));
        bool isSector = angle < 359f;
        int arcVerts = (segments + 1) * 2;
        int vertexCount = arcVerts + (isSector ? 8 : 0);
        Vector3[] vertices = new Vector3[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] triangles = new int[segments * 6 + (isSector ? 12 : 0)];

        // 1. แถบโค้ง (รัศมีใน - รัศมีนอก)
        for (int i = 0; i <= segments; i++)
        {
            float a = (-angle / 2f + angle * i / segments) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            vertices[i * 2] = dir * (radius - width);
            vertices[i * 2 + 1] = dir * radius;
        }
        for (int i = 0; i < segments; i++)
        {
            int v = i * 2;
            int t = i * 6;
            triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 3;
            triangles[t + 3] = v; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2;
        }

        // 2. เส้นข้างของรูปพัด (จากกลางออกไปถึงขอบ)
        if (isSector)
        {
            for (int s = 0; s < 2; s++)
            {
                float a = (s == 0 ? -angle / 2f : angle / 2f) * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                Vector3 side = new Vector3(dir.z, 0f, -dir.x) * (width / 2f);
                int v = arcVerts + s * 4;
                vertices[v] = -side;
                vertices[v + 1] = side;
                vertices[v + 2] = dir * radius + side;
                vertices[v + 3] = dir * radius - side;
                int t = segments * 6 + s * 6;
                triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2;
                triangles[t + 3] = v; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }
        }

        for (int i = 0; i < vertexCount; i++) colors[i] = color;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }
}
