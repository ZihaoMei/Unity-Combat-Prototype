using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Speed Settings")]
    public float walkSpeed = 2.0f;
    public float runSpeed = 5.0f;
    public float rollSpeed = 8.0f;
    public float rotationSpeed = 10.0f;
    public float gravity = 9.81f;

    private CharacterController controller;
    private Animator anim;
    private PlayerStamina stamina;
    private PlayerHealth health;
    private Camera mainCamera;

    private Vector3 verticalVelocity;
    private Vector3 rollDirection;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        stamina = GetComponent<PlayerStamina>();
        health = GetComponent<PlayerHealth>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (health != null && health.isDead) return;

        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);
        bool isLocked = stateInfo.IsTag("Lock");
        bool isAttacking = stateInfo.IsTag("Attack");
        bool isRolling = stateInfo.IsTag("Rolling");

        // Lock中は完全停止
        if (isLocked)
        {
            anim.SetFloat("Speed", 0f);
            return;
        }

        // 1. 入力取得
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;

        // 2. 重力
        if (controller.isGrounded && verticalVelocity.y < 0)
        {
            verticalVelocity.y = -2f;
        }
        verticalVelocity.y -= gravity * Time.deltaTime;

        // 3. ローリング
        if (Input.GetKeyDown(KeyCode.Space) && !isRolling && !isAttacking)
        {
            if (stamina.TryConsumeStamina(stamina.rollCost))
            {
                if (inputDir.magnitude >= 0.1f)
                {
                    float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
                    rollDirection = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward;
                }
                else
                {
                    rollDirection = transform.forward;
                }

                transform.rotation = Quaternion.LookRotation(rollDirection);
                anim.SetTrigger("Roll");
            }
        }

        // 4. 移動
        HandleMovement(inputDir, isAttacking, isRolling);
    }

    private void HandleMovement(Vector3 inputDir, bool isAttacking, bool isRolling)
    {
        Vector3 moveVec = Vector3.zero;

        if (isRolling)
        {
            moveVec = rollDirection * rollSpeed;
        }
        else if (!isAttacking && inputDir.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg + mainCamera.transform.eulerAngles.y;
            Quaternion targetRotation = Quaternion.Euler(0, targetAngle, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            bool isRunning = Input.GetKey(KeyCode.LeftShift) && stamina.currentStamina > 0 && !stamina.isExhausted;
            float currentSpeed = isRunning ? runSpeed : walkSpeed;

            if (Input.GetKey(KeyCode.LeftShift))
            {
                stamina.ConsumeDashStamina();
            }

            moveVec = Quaternion.Euler(0, targetAngle, 0) * Vector3.forward * currentSpeed;
        }

        controller.Move((moveVec + verticalVelocity) * Time.deltaTime);

        if (!isRolling && !isAttacking)
        {
            float animSpeed = (inputDir.magnitude < 0.1f) ? 0 : (Input.GetKey(KeyCode.LeftShift) ? 1.0f : 0.5f);
            anim.SetFloat("Speed", animSpeed, 0.1f, Time.deltaTime);
        }
    }
}