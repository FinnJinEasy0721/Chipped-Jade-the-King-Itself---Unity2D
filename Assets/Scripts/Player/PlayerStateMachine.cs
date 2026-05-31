using UnityEngine;

/// <summary>
/// PlayerStateMachine.cs
/// 【已修复】连续攻击必须等待当前动画完全播放完毕后才能播放下一段
/// 新增 comboBuffered 缓冲机制，严格遵循设计文档：第一段结束后才能第二段，依此类推
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerStateMachine : MonoBehaviour
{
    public enum PlayerState
    {
        Idle, Run, Jump, Fall, ToGround, Slide,
        Attack1, Attack2, Attack3,
        HeavyAttack, Hurt, Die, UseBlessing, Getup
    }

    public PlayerState currentState = PlayerState.Idle;
    private PlayerState previousState = PlayerState.Idle;

    private Animator animator;
    private PlayerController controller;
    private PlayerStat stat;

    // 状态计时器
    private float stateTimer = 0f;
    private float hurtCooldown = 0f;
    private float heavyChargeTime = 0.2f;
    private float slideDuration = 0.3f; // 滑行持续时间
    private float slideCooldown = 0f; // 滑行冷却

    // 连击缓冲：玩家可在攻击中提前按J，但必须等当前动画播完才切换
    private bool comboBuffered = false;

    // 攻击翻转缓冲：攻击中按反方向键，动画播完后翻转
    private bool flipBuffered = false;

    [Header("攻击前方位移")]
    [Tooltip("攻击1前方位移速度")]
    public float attack1MoveDistance = 1f;
    [Tooltip("攻击2前方位移速度")]
    public float attack2MoveDistance = 1.5f;
    [Tooltip("攻击3前方位移速度")]
    public float attack3MoveDistance = 2f;

    [Header("攻击判定")]
    [Tooltip("玩家攻击框（子对象上的AttackHitbox组件）")]
    public AttackHitbox attackHitbox;

    [Header("动画剪辑（用于基于时长的状态退出）")]
    public AnimationClip hurtClip;
    public AnimationClip dieClip;

    private int comboStep = 0; // 保留以便未来扩展
    private bool isChargingHeavy = false;
    private bool hasDealtDamageThisState = false;
    private bool hasActivatedHitbox = false;
    private bool keepLastJumpFrame = false;
    private bool hasDoubleJumped = false;
    private float slideDirection = 1f;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Initialize(PlayerController ctrl, PlayerStat st)
    {
        controller = ctrl;
        stat = st;
        ChangeState(PlayerState.Idle);
        stat.Curr_HP = stat.GetMaxHP();
        comboBuffered = false;

        if (attackHitbox != null)
            attackHitbox.OnHit += OnPlayerAttackHit;
    }

    public void CustomUpdate() // 由 PlayerController 每帧调用
    {
        stateTimer -= Time.deltaTime;
        hurtCooldown -= Time.deltaTime;
        slideCooldown -= Time.deltaTime;

        HandleStateLogic();
        CheckAutoTransitions();
        HandleSpecialAnimations();

        if (stat.Curr_HP <= 0 && currentState != PlayerState.Die)
            ChangeState(PlayerState.Die);
    }

    public void CustomFixedUpdate()
    {
        switch (currentState)
        {
            case PlayerState.Idle:
            case PlayerState.Run:
                float targetSpeed = controller.GetHorizontalInput() * stat.Base_PlayerSpeed;
                controller.SetVelocityX(targetSpeed);
                break;

            case PlayerState.Jump:
            case PlayerState.Fall:
                float airSpeed = controller.GetHorizontalInput() * stat.Base_PlayerSpeed * 0.85f;
                controller.SetVelocityX(airSpeed);
                break;

            case PlayerState.Slide:
                controller.SetVelocityX(slideDirection * stat.Base_PlayerSpeed * 1.5f);
                break;

            case PlayerState.Attack1:
            case PlayerState.Attack2:
            case PlayerState.Attack3:
                controller.UpdateAttackMove();
                break;
            case PlayerState.UseBlessing:
            case PlayerState.HeavyAttack:
            case PlayerState.Hurt:
            case PlayerState.Die:
            case PlayerState.Getup:
            case PlayerState.ToGround:
                controller.SetVelocityX(0f);
                break;
        }
    }

    // ====================== 内部逻辑 ======================
    private void HandleStateLogic()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);

        switch (currentState)
        {
            case PlayerState.Idle:
                if (Mathf.Abs(controller.GetHorizontalInput()) > 0.1f)
                    ChangeState(PlayerState.Run);
                break;

            case PlayerState.Run:
                if (Mathf.Abs(controller.GetHorizontalInput()) < 0.1f)
                    ChangeState(PlayerState.Idle);
                break;

            case PlayerState.Jump:
                if (controller.GetVelocity().y <= 0f)
                    ChangeState(PlayerState.Fall);
                break;

            case PlayerState.Fall:
                if (controller.IsGrounded())
                    ChangeState(PlayerState.ToGround);
                break;

            case PlayerState.ToGround:
                if (stateTimer <= 0f)
                    ChangeState(Mathf.Abs(controller.GetHorizontalInput()) > 0.1f ? PlayerState.Run : PlayerState.Idle);
                break;

            case PlayerState.Slide:
                if (stateTimer <= 0f)
                    ChangeState(Mathf.Abs(controller.GetHorizontalInput()) > 0.1f ? PlayerState.Run : PlayerState.Idle);
                break;

            case PlayerState.Attack1:
            case PlayerState.Attack2:
            case PlayerState.Attack3:
                // 检测反方向输入，缓冲翻转
                float input = controller.GetHorizontalInput();
                float facing = Mathf.Sign(controller.transform.localScale.x);
                if (input * facing < -0.1f)
                    flipBuffered = true;

                // 动画播放到一半时启用攻击框
                if (!hasActivatedHitbox && info.normalizedTime >= 0.5f)
                {
                    hasActivatedHitbox = true;
                    if (attackHitbox != null) attackHitbox.Activate();
                }

                // 动画完全播完后再检查是否需要连击
                if (info.normalizedTime >= 1f)
                {
                    if (comboBuffered && stat.CanCombo())
                    {
                        comboBuffered = false;
                        PlayerState nextState = currentState switch
                        {
                            PlayerState.Attack1 => PlayerState.Attack2,
                            PlayerState.Attack2 => PlayerState.Attack3,
                            PlayerState.Attack3 => PlayerState.Attack1, // 循环
                            _ => PlayerState.Attack1
                        };
                        ChangeState(nextState);
                    }
                    else
                    {
                        ChangeState(Mathf.Abs(controller.GetHorizontalInput()) > 0.1f ? PlayerState.Run : PlayerState.Idle);
                    }
                }
                break;

            case PlayerState.HeavyAttack:
                if (isChargingHeavy)
                {
                    if (stateTimer <= 0f)
                    {
                        isChargingHeavy = false;
                        animator.speed = 1f;
                    }
                }
                else
                {
                    // 动画播放到一半时启用攻击框
                    if (!hasActivatedHitbox && info.normalizedTime >= 0.5f)
                    {
                        hasActivatedHitbox = true;
                        if (attackHitbox != null) attackHitbox.Activate();
                    }

                    if (info.normalizedTime >= 1f)
                    {
                        stat.StartHeavyCD();
                        ChangeState(Mathf.Abs(controller.GetHorizontalInput()) > 0.1f ? PlayerState.Run : PlayerState.Idle);
                    }
                }
                break;

            case PlayerState.Hurt:
                if (stateTimer <= 0f)
                    ChangeState(PlayerState.Idle);
                break;

            case PlayerState.Die:
                if (stateTimer <= 0f)
                    Debug.Log("【死亡动画结束】");
                break;

            case PlayerState.UseBlessing:
                if (info.normalizedTime >= 1f)
                {
                    stat.UseBlessingSkill();
                    ChangeState(PlayerState.Idle);
                }
                break;

            case PlayerState.Getup:
                if (info.normalizedTime >= 1f)
                    ChangeState(PlayerState.Idle);
                break;
        }
    }

    // 自动状态转换：如从空中落地、从攻击落回待机/跑步等
    private void CheckAutoTransitions() 
    {
        if (currentState != PlayerState.Jump && currentState != PlayerState.Fall &&
            currentState != PlayerState.ToGround && currentState != PlayerState.Getup &&
            currentState != PlayerState.Die && currentState != PlayerState.Hurt &&
            currentState != PlayerState.UseBlessing)
        {
            if (!controller.IsGrounded() && controller.GetVelocity().y < -3f)
            {
                previousState = currentState;
                ChangeState(PlayerState.Fall);
            }
        }
    }

    // 处理特殊动画：跳跃动画
    private void HandleSpecialAnimations()
    {
        if (currentState == PlayerState.Jump)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
            if (info.normalizedTime >= 1f && controller.GetVelocity().y > 0f)
            {
                animator.speed = 0f;
                keepLastJumpFrame = true;
            }
            else if (keepLastJumpFrame && controller.GetVelocity().y <= 0f)
            {
                animator.speed = 1f;
                keepLastJumpFrame = false;
            }
        }
    }

    public void ChangeState(PlayerState newState)
    {
        if (currentState == newState) return;

        ExitState(currentState);
        previousState = currentState;
        currentState = newState;
        EnterState(newState);
    }

    private void EnterState(PlayerState state)
    {
        stateTimer = 0f;
        hasDealtDamageThisState = false;
        hasActivatedHitbox = false;
        isChargingHeavy = false;
        animator.speed = 1f;

        // 攻击期间禁止翻转
        if (state == PlayerState.Attack1 || state == PlayerState.Attack2 || state == PlayerState.Attack3)
            controller.canFlip = false;

        switch (state)
        {
            case PlayerState.Idle: PlayAnimation("Idle"); break;
            case PlayerState.Run: PlayAnimation("Run"); break;

            case PlayerState.Jump:
                PlayAnimation("Jump");
                controller.ApplyJumpForce();
                if (controller.IsGrounded()) hasDoubleJumped = false;
                break;

            case PlayerState.Fall:
                PlayAnimation("Fall");
                break;

            case PlayerState.ToGround:
                PlayAnimation("ToGround");
                stateTimer = 0.2f;
                break;

            case PlayerState.Slide:
                PlayAnimation("Slide");
                stateTimer = slideDuration;
                slideDirection = controller.transform.localScale.x;
                break;

            case PlayerState.Attack1:
                PlayAnimation("Attack1");
                comboStep = 1;
                controller.StartAttackMove(attack1MoveDistance);
                break;
            case PlayerState.Attack2:
                PlayAnimation("Attack2");
                comboStep = 2;
                controller.StartAttackMove(attack2MoveDistance);
                break;
            case PlayerState.Attack3:
                PlayAnimation("Attack3");
                comboStep = 3;
                controller.StartAttackMove(attack3MoveDistance);
                break;

            case PlayerState.HeavyAttack:
                PlayAnimation("HeavyAttack");
                isChargingHeavy = true;
                stateTimer = heavyChargeTime;
                animator.speed = 0.5f;
                break;

            case PlayerState.Hurt:
                PlayAnimation("Hurt");
                stateTimer = hurtClip != null ? hurtClip.length : 0.6f;
                controller.ApplyKnockback(8f);
                break;

            case PlayerState.Die:
                PlayAnimation("Die");
                stateTimer = dieClip != null ? dieClip.length : 1f;
                break;

            case PlayerState.UseBlessing:
                PlayAnimation("UseBlessing");
                break;

            case PlayerState.Getup:
                PlayAnimation("Getup");
                break;
        }
    }

    private void ExitState(PlayerState state)
    {
        if (state == PlayerState.Attack1 || state == PlayerState.Attack2 || state == PlayerState.Attack3)
        {
            comboBuffered = false;
            controller.canFlip = true;

            if (flipBuffered)
            {
                controller.Flip();
                flipBuffered = false;
            }

            if (attackHitbox != null) attackHitbox.Deactivate();
        }
        if (state == PlayerState.HeavyAttack)
        {
            if (attackHitbox != null) attackHitbox.Deactivate();
        }
        switch (state)
        {
            case PlayerState.HeavyAttack:
                animator.speed = 1f;
                break;
            case PlayerState.Jump:
                keepLastJumpFrame = false;
                break;
        }
    }

    private void PlayAnimation(string animName)
    {
        animator.Play(animName, 0, 0f);
    }

    // ====================== 输入接口 ======================
    public void TryJump()
    {
        if (currentState == PlayerState.Die || currentState == PlayerState.Getup ||
            currentState == PlayerState.Hurt || currentState == PlayerState.UseBlessing) return;

        if (controller.IsGrounded())
        {
            ChangeState(PlayerState.Jump);
        }
        else if (stat.CanJumpII() && !hasDoubleJumped)
        {
            hasDoubleJumped = true;
            ChangeState(PlayerState.Jump);
            animator.Play("Jump");
        }
    }

    public void TryAttack()
    {
        // 死亡、受击、起身、祝福、重击时不可攻击
        if (currentState == PlayerState.Die || currentState == PlayerState.Hurt ||
            currentState == PlayerState.Getup || currentState == PlayerState.UseBlessing ||
            currentState == PlayerState.HeavyAttack) return;

        // 攻击状态中只缓冲
        if (currentState == PlayerState.Attack1 || currentState == PlayerState.Attack2 || currentState == PlayerState.Attack3)
        {
            if (stat.CanCombo())
                comboBuffered = true;
            return;
        }

        // 跑步或待机时，直接切换到Attack1
        if (currentState == PlayerState.Run || currentState == PlayerState.Idle)
        {
            ChangeState(PlayerState.Attack1);
            return;
        }

        // 其他状态需在地面才能攻击
        if (!controller.IsGrounded()) return;

        ChangeState(PlayerState.Attack1);
    }

    public void StartHeavyCharge()
    {
        if (!controller.IsGrounded() || !stat.CanHeavyAttack() ||
            currentState == PlayerState.Die || currentState == PlayerState.Hurt ||
            currentState == PlayerState.Getup || currentState == PlayerState.UseBlessing) return;

        ChangeState(PlayerState.HeavyAttack);
    }

    public void ReleaseHeavyAttack()
    {
        if (currentState != PlayerState.HeavyAttack) return;

        if (isChargingHeavy && stateTimer > 0f)
        {
            ChangeState(previousState);
        }
        else
        {
            isChargingHeavy = false;
            animator.speed = 1f;
            stat.StartHeavyCD();
        }
    }

    public void TryUseBlessing()
    {
        if (!controller.IsGrounded() || currentState == PlayerState.Die ||
            currentState == PlayerState.Hurt || currentState == PlayerState.Getup ||
            currentState == PlayerState.UseBlessing) return;

        ChangeState(PlayerState.UseBlessing);
    }

    public void TrySlide()
    {
        if (currentState == PlayerState.Die || currentState == PlayerState.Hurt ||
            currentState == PlayerState.Getup || currentState == PlayerState.UseBlessing ||
            currentState == PlayerState.Jump || currentState == PlayerState.Fall) return;

        if (slideCooldown > 0f) return; // 冷却中不能滑行

        ChangeState(PlayerState.Slide);
        slideCooldown = 1f; // 设置1秒冷却
    }

    // ====================== 攻击框命中回调 ======================

    private void OnPlayerAttackHit(Collider2D target)
    {
        var enemyAI = target.GetComponentInParent<EnemyAIMelee>();
        if (enemyAI == null) return;

        int comboStage = currentState switch
        {
            PlayerState.Attack1 => 1,
            PlayerState.Attack2 => 2,
            PlayerState.Attack3 => 3,
            PlayerState.HeavyAttack => 0,
            _ => 1
        };

        bool isCrit;
        int damage = stat.GetAttackDamage(comboStage, out isCrit);
        Debug.Log($"【玩家攻击敌人】造成 {damage} 伤害{(isCrit ? " 暴击！" : "")}");
        enemyAI.TakeDamage(damage);
    }

    // ====================== 公开接口 ======================
    public void TakeHurt(int damage) // 外部敌人调用 关心游戏逻辑，决定什么时候进入受伤或死亡状态
    {
        stat.TakeDamage(damage); // 先扣血

        if (stat.Curr_HP <= 0)
        {
            ChangeState(PlayerState.Die); // 死亡判定
            return;
        }

        if ((currentState == PlayerState.Idle || currentState == PlayerState.Run) && hurtCooldown <= 0f)
        {
            hurtCooldown = 1f;
            ChangeState(PlayerState.Hurt); // 受伤判定
        }
    }

    public void StartGetup()
    {
        ChangeState(PlayerState.Getup);
    }
}