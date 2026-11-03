using UnityEngine;

// กล้องมุมเฉียงจากด้านบน ตามผู้เล่นแบบนุ่ม ๆ
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 12f, -9f);
    [SerializeField] private float smoothTime = 0.15f;

    private Vector3 velocity;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: ยังไม่ได้ลาก Player ใส่ช่อง Target");
            return;
        }
        transform.position = target.position + offset;
        transform.LookAt(target);
    }

    // LateUpdate ทำงานหลัง Player เดินเสร็จในเฟรมนั้น กล้องจะไม่สั่น
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 wanted = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, wanted, ref velocity, smoothTime);
    }
}
