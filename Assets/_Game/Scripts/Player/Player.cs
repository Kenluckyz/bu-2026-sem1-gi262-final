using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// ตัวละครที่ผู้เล่นคุม — สืบทอดจาก Character (Inheritance)
[RequireComponent(typeof(CharacterController))]
public class Player : Character
{
    [Header("Player")]
    [SerializeField] private float turnSpeed = 720f; // องศาต่อวินาที
    [SerializeField] private float gravity = -20f;

    [Header("Stats")]
    [SerializeField] private PlayerStats stats = new PlayerStats();
    public PlayerStats Stats => stats;

    [Header("Attack")]
    [SerializeField] private float attackRange = 1.5f;   // ระยะฟันข้างหน้า
    [SerializeField] private float attackRadius = 1.2f;  // ความกว้างของวงฟัน
    // จังหวะฟัน: ง้างช้า -> ฟันเร็ว (โดนตรงนี้) -> ค้าง -> คืนท่า
    [SerializeField] private float windUpTime = 0.15f;
    [SerializeField] private float recoverTime = 0.25f;

    private CharacterController controller;
    private InputAction moveAction;
    private InputAction attackAction;
    private float verticalVelocity;
    private bool isAttacking;

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
        attackAction = InputSystem.actions.FindAction("Attack"); // คลิกซ้าย
        stats.Refresh();
    }

    private void Update()
    {
        if (!isAlive || moveAction == null) return;

        // ระหว่างฟันยืนนิ่ง จะได้ฟันตรงทิศที่หันอยู่
        Vector2 input = isAttacking ? Vector2.zero : moveAction.ReadValue<Vector2>();
        Move(input);

        if (attackAction != null && attackAction.triggered && !isAttacking)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        yield return new WaitForSeconds(windUpTime);
        Attack();
        yield return new WaitForSeconds(recoverTime);
        isAttacking = false;
    }

    // หา Character ทุกตัวในวงหน้าผู้เล่น แล้วสั่ง TakeDamage (Polymorphism: ศัตรูแต่ละแบบรับดาเมจต่างกันได้)
    public void Attack()
    {
        Vector3 center = transform.position + transform.forward * attackRange;
        Collider[] hits = Physics.OverlapSphere(center, attackRadius);
        for (int i = 0; i < hits.Length; i++)
        {
            Character target = hits[i].GetComponent<Character>();
            if (target != null && target != this && target.IsAlive)
            {
                target.TakeDamage(stats.Damage);
                if (!target.IsAlive)
                {
                    stats.AddExp(target.ExpReward); // ตีตายแล้วได้ EXP
                }
            }
        }
    }

    // วาดวงฟันในหน้าต่าง Scene ไว้ปรับระยะ
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attackRange, attackRadius);
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
        // ล้มลงให้เห็นว่าตาย ภายหลัง GameManager จะฟัง OnDied แล้วเปิดหน้า Game Over
        transform.rotation = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
    }
}
