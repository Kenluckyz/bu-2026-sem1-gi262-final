using UnityEngine;

// กล้องตามผู้เล่น มุมเฉียง
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 7f, -5.5f);
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

    // ใช้ LateUpdate กล้องจะไม่สั่น
    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 wanted = target.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, wanted, ref velocity, smoothTime);
    }
}
