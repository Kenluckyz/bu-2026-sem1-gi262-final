using UnityEngine;
using UnityEngine.InputSystem;

// ตัวละครที่ผู้เล่นคุม — สืบทอดจาก Character (Inheritance)
[RequireComponent(typeof(CharacterController))]
public class Player : Character
{
    [Header("Player")]
    [SerializeField] private float turnSpeed = 720f; // องศาต่อวินาที
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private InputAction moveAction;
    private float verticalVelocity;

    public override void SetUP()
    {
        base.SetUP();
        controller = GetComponent<CharacterController>();
        // ดึง action จาก InputSystem_Actions แบบเดียวกับ OOPPlayer ในคลาส
        moveAction = InputSystem.actions.FindAction("Move");
        if (moveAction == null)
        {
            Debug.LogError("ไม่พบ Input Action ชื่อ Move");
        }
    }

    private void Update()
    {
        if (!isAlive || moveAction == null) return;

        Vector2 input = moveAction.ReadValue<Vector2>();
        Move(input);
    }

    private void Move(Vector2 input)
    {
        // WASD เป็น 2D (x,y) แต่เกม 3D เดินบนพื้น (x,z)
        Vector3 direction = new Vector3(input.x, 0f, input.y);
        if (direction.sqrMagnitude > 1f) direction.Normalize();

        if (direction.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        // แรงโน้มถ่วง ให้ติดพื้นเสมอ
        if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = direction * moveSpeed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);
    }

    protected override void Die()
    {
        base.Die();
        // ภายหลัง GameManager จะฟัง OnDied แล้วเปิดหน้า Game Over
    }
}
