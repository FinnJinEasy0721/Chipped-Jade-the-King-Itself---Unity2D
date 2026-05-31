using UnityEngine;

/// <summary>
/// PlayerController.cs
/// 【已修复】跳跃后永远卡在Jump状态的问题 + 地面检测已优化
/// 新增：每帧调用 stateMachine.CustomUpdate()，让状态机正常切换
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("组件引用")]
    private Rigidbody2D rb;
    private PlayerStateMachine stateMachine;
    private PlayerStat stat;

    // 攻击期间禁止翻转
    public bool canFlip = true;

    [Header("移动设置")]
    public float currentMoveSpeed;

    [Header("地面检测")]
    public Transform groundCheck;
    [Range(0.1f, 1f)]
    public float groundCheckDistance = 0.35f;
    public LayerMask groundLayer;

    [Header("其他")]
    private bool facingRight = true;
    private bool isGrounded;
    private float horizontalInput;

    // 输入缓存
    private bool jumpInput, attackInput, heavyInputDown, heavyInputUp;
    private bool blessingInput, slideInput, bagInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stateMachine = GetComponent<PlayerStateMachine>();
        stat = GetComponent<PlayerStat>();
    }

    private void Start()
    {
        currentMoveSpeed = stat.Base_PlayerSpeed;
        stateMachine.Initialize(this, stat);
    }

    private void Update()
    {
        // === 地面检测 ===
        isGrounded = Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, groundLayer);

        // === 输入读取 ===
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.Space)) jumpInput = true;
        if (Input.GetKeyDown(KeyCode.J)) attackInput = true;
        if (Input.GetKeyDown(KeyCode.K)) heavyInputDown = true;
        if (Input.GetKeyUp(KeyCode.K)) heavyInputUp = true;
        if (Input.GetKeyDown(KeyCode.L)) blessingInput = true;

        // 滑行
        if ((Input.GetKey(KeyCode.S) && (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.D))) ||
            (Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D))))
            slideInput = true;

        if (Input.GetKeyDown(KeyCode.Tab)) bagInput = true;
        if (Input.GetKeyDown(KeyCode.Escape))
            Debug.Log("【游戏暂停】ESC");

        // 翻转
        if (canFlip) {
            if (horizontalInput > 0.1f && !facingRight) Flip();
            if (horizontalInput < -0.1f && facingRight) Flip();
        }

        // === 每帧驱动状态机逻辑 ===
        HandlePlayerInput();
        stateMachine.CustomUpdate();//解决跳跃卡住
        stat.UpdateTimer();

        // 全局死亡检测
        if (stat.Curr_HP <= 0 && stateMachine.currentState != PlayerStateMachine.PlayerState.Die)
            stateMachine.ChangeState(PlayerStateMachine.PlayerState.Die);
    }

    private void FixedUpdate()
    {
        stateMachine.CustomFixedUpdate();
    }

    private void HandlePlayerInput()
    {
        if (jumpInput) { stateMachine.TryJump(); jumpInput = false; }
        if (attackInput) { stateMachine.TryAttack(); attackInput = false; }
        if (heavyInputDown) { stateMachine.StartHeavyCharge(); heavyInputDown = false; }
        if (heavyInputUp) { stateMachine.ReleaseHeavyAttack(); heavyInputUp = false; }
        if (blessingInput) { stateMachine.TryUseBlessing(); blessingInput = false; }
        if (slideInput) { stateMachine.TrySlide(); slideInput = false; }
        if (bagInput)
        {
            Debug.Log("【打开背包】");
            bagInput = false;
        }
    }

    public void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1f;
        transform.localScale = scale;
    }

    // ====================== 公开接口 ======================
    public bool IsGrounded() => isGrounded;
    public float GetHorizontalInput() => horizontalInput;
    public Vector2 GetVelocity() => rb.velocity;

    public void SetVelocityX(float targetX)
    {
        float smoothed = Mathf.MoveTowards(rb.velocity.x, targetX, 35f * Time.fixedDeltaTime);
        rb.velocity = new Vector2(smoothed, rb.velocity.y);
    }

    public void ApplyJumpForce()
    {
        rb.velocity = new Vector2(rb.velocity.x, stat.Base_JumpForce);
    }

    // 攻击突进剩余距离
    private float attackMoveRemaining;

    public void StartAttackMove(float distance)
    {
        attackMoveRemaining = distance;
    }

    public void UpdateAttackMove()
    {
        if (attackMoveRemaining > 0.001f)
        {
            float dir = facingRight ? 1f : -1f;
            // 每帧移动剩余距离的30%，起步猛、收尾快
            float moveThisFrame = Mathf.Max(attackMoveRemaining * 0.3f, 0.02f);
            moveThisFrame = Mathf.Min(moveThisFrame, attackMoveRemaining);
            rb.MovePosition(rb.position + new Vector2(dir * moveThisFrame, 0f));
            attackMoveRemaining -= moveThisFrame;
        }
        else
        {
            attackMoveRemaining = 0f;
            rb.velocity = new Vector2(0f, rb.velocity.y);
        }
    }

    public void ApplyKnockback(float force = 8f)
    {
        rb.AddForce(new Vector2(facingRight ? -force : force, 2f));
    }

    public void TriggerGetup()
    {
        stateMachine.ChangeState(PlayerStateMachine.PlayerState.Getup);
    }

    //====================== 调试可视化 ======================
    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 end = groundCheck.position + Vector3.down * groundCheckDistance;
        Gizmos.DrawLine(groundCheck.position, end);
        Gizmos.DrawSphere(end, 0.05f);
    }
}