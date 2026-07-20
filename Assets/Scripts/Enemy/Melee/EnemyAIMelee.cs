using System.Collections;
using UnityEngine;

/// <summary>
/// 近战敌人AI控制器
/// 负责控制近战敌人的行为逻辑（移动、检测、攻击、受伤等）
/// 动画由EnemyFSM处理，本脚本是纯逻辑层
///
/// 状态流转：
///   Idle ←→ Patrol（随机时间切换）
///   → Chase（目视检测器检测到玩家）
///   → Attack（攻击启动框检测到玩家，0.2s延迟）
///   → Hurt（受击框检测到玩家攻击 + 击退；Attack状态可被打断）
///   → Die（HP≤0，跳过Hurt）
///   转身检测器检测到玩家 → 0.7s延迟转身 → 判断Attack/Chase/Patrol
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(EnemyStatBase), typeof(EnemyFSM))]
public class EnemyAIMelee : MonoBehaviour
{
    [Header("攻击判定")]
    [SerializeField] private AttackHitbox _attackHitbox;   // 敌人攻击框：DetectionZone.targetTag = "PlayerHurtBox"

    [Header("检测区域引用（子对象上的DetectionZone组件）")]
    [SerializeField] private DetectionZone _visionDetector;  // 目视检测器：targetTag = "Player"
    [SerializeField] private DetectionZone _attackTrigger;   // 攻击启动框：targetTag = "Player"
    [SerializeField] private DetectionZone _turnDetector;     // 转身检测器：targetTag = "Player"

    private EnemyStatBase _stat;
    private EnemyFSM _fsm;
    private Rigidbody2D _rb;
    private Transform _player;

    // 状态计时
    private float _stateTimer;

    // 追逐：记录玩家最后已知位置
    private Vector2 _lastKnownPlayerPos;
    private bool _movingToLastKnown;

    // 协程控制
    private Coroutine _turnAroundCoroutine;
    private Coroutine _attackDelayCoroutine;
    private bool _isTurningAround;
    private bool _isWaitingToAttack;
    private bool _hasActivatedHitbox;

    private void Awake()
    {
        _stat = GetComponent<EnemyStatBase>();
        _fsm = GetComponent<EnemyFSM>();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;

        if (_attackHitbox != null)
            _attackHitbox.OnHit += OnEnemyAttackHit;

        // 初始化为Idle状态
        _fsm.ForceChangeState(EnemyFSM.EnemyState.Idle);
        _stateTimer = Random.Range(_stat.IdleToPatrolTimeRangeMin, _stat.IdleToPatrolTimeRangeMax);
    }

    private void OnDestroy()
    {
        if (_attackHitbox != null)
            _attackHitbox.OnHit -= OnEnemyAttackHit;
    }

    private void Update()
    {
        if (_fsm.CurrentState == EnemyFSM.EnemyState.Die) return;

        switch (_fsm.CurrentState)
        {
            case EnemyFSM.EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyFSM.EnemyState.Patrol:
                UpdatePatrol();
                break;
            case EnemyFSM.EnemyState.Chase:
                UpdateChase();
                break;
            case EnemyFSM.EnemyState.Attack:
                UpdateAttack();
                break;
            case EnemyFSM.EnemyState.AttackCooldown:
                UpdateAttackCooldown();
                break;
            case EnemyFSM.EnemyState.Hurt:
                UpdateHurt();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (_fsm.CurrentState == EnemyFSM.EnemyState.Die) return;

        switch (_fsm.CurrentState)
        {
            case EnemyFSM.EnemyState.Patrol:
                MoveInFacingDirection(_stat.PatrolSpeed);
                break;
            case EnemyFSM.EnemyState.Chase:
                MoveChase();
                break;
            default:
                StopHorizontal();
                break;
        }
    }

    // ==================== 状态更新 ====================

    private void UpdateIdle()
    {
        if (CheckDetectionAndReact()) return;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            SwitchToPatrol();
        }
    }

    private void UpdatePatrol()
    {
        if (CheckDetectionAndReact()) return;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
        {
            SwitchToIdle();
        }
    }

    private void UpdateChase()
    {
        // 攻击启动框优先
        if (_attackTrigger != null && _attackTrigger.TargetInZone && !_isWaitingToAttack)
        {
            StartAttackDelay();
            return;
        }

        // 转身检测器
        if (_turnDetector != null && _turnDetector.TargetInZone && !_isTurningAround)
        {
            StartTurnAround();
            return;
        }

        // 目视检测器：追逐或前往最后已知位置
        if (_visionDetector != null && _visionDetector.TargetInZone)
        {
            _lastKnownPlayerPos = _player.position;
            _movingToLastKnown = false;
            FaceTarget(_player.position);
        }
        else
        {
            // 玩家离开目视范围 → 前往最后已知位置
            _movingToLastKnown = true;
            if (Vector2.Distance(transform.position, _lastKnownPlayerPos) < 0.5f)
            {
                SwitchToPatrol();
                return;
            }
        }
    }

    private void UpdateAttack()
    {
        _stateTimer -= Time.deltaTime;

        // 攻击动画播放到一半时启用攻击框
        float halfTime = _fsm.AttackClipLength * 0.5f;
        if (!_hasActivatedHitbox && _stateTimer <= halfTime)
        {
            _hasActivatedHitbox = true;
            if (_attackHitbox != null) _attackHitbox.Activate();
        }

        if (_stateTimer > 0f) return;

        // 攻击动画播完，判断下一步
        if (_attackTrigger != null && _attackTrigger.TargetInZone)
        {
            // 玩家仍在攻击范围内 → 冷却后再攻击
            SwitchToAttackCooldown();
        }
        else if (_visionDetector != null && _visionDetector.TargetInZone)
        {
            // 玩家离开攻击范围但在视野内 → 立即追逐
            SwitchToChase();
        }
        else
        {
            SwitchToPatrol();
        }
    }

    private void UpdateAttackCooldown()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer > 0f) return;

        // 冷却结束，判断下一步
        if (_attackTrigger != null && _attackTrigger.TargetInZone)
        {
            SwitchToAttack();
        }
        else if (_visionDetector != null && _visionDetector.TargetInZone)
        {
            SwitchToChase();
        }
        else
        {
            SwitchToPatrol();
        }
    }

    private void UpdateHurt()
    {
        _stateTimer -= Time.deltaTime;
        if (_stateTimer > 0f) return;

        if (_visionDetector != null && _visionDetector.TargetInZone)
        {
            SwitchToChase();
        }
        else
        {
            SwitchToIdle();
        }
    }

    // ==================== 检测逻辑（Idle/Patrol共用）====================

    /// <summary>
    /// 在Idle/Patrol状态下检测玩家，返回true表示已触发状态切换
    /// 优先级：攻击启动框 > 目视检测器 > 转身检测器
    /// </summary>
    private bool CheckDetectionAndReact()
    {
        // 攻击启动框 → 0.2s延迟后Attack
        if (_attackTrigger != null && _attackTrigger.TargetInZone && !_isWaitingToAttack)
        {
            StartAttackDelay();
            return true;
        }

        // 目视检测器 → Chase
        if (_visionDetector != null && _visionDetector.TargetInZone)
        {
            SwitchToChase();
            return true;
        }

        // 转身检测器 → 0.7s延迟转身
        if (_turnDetector != null && _turnDetector.TargetInZone && !_isTurningAround)
        {
            StartTurnAround();
            return true;
        }

        return false;
    }

    // ==================== 状态切换 ====================

    private void SwitchToIdle()
    {
        CancelAllCoroutines();
        _fsm.ChangeState(EnemyFSM.EnemyState.Idle);
        StopHorizontal();
        _stateTimer = Random.Range(_stat.IdleToPatrolTimeRangeMin, _stat.IdleToPatrolTimeRangeMax);
    }

    private void SwitchToPatrol()
    {
        CancelAllCoroutines();
        _fsm.ChangeState(EnemyFSM.EnemyState.Patrol);
        _stateTimer = Random.Range(_stat.PatrolToIdleTimeRangeMin, _stat.PatrolToIdleTimeRangeMax);
    }

    private void SwitchToChase()
    {
        CancelAllCoroutines();
        _fsm.ChangeState(EnemyFSM.EnemyState.Chase);
        _lastKnownPlayerPos = _player.position;
        _movingToLastKnown = false;
    }

    private void SwitchToAttack()
    {
        _fsm.ChangeState(EnemyFSM.EnemyState.Attack);
        StopHorizontal();
        _stateTimer = _fsm.AttackClipLength;
        _hasActivatedHitbox = false;
    }

    private void SwitchToAttackCooldown()
    {
        CancelAllCoroutines();
        if (_attackHitbox != null) _attackHitbox.Deactivate();
        _fsm.ChangeState(EnemyFSM.EnemyState.AttackCooldown);
        StopHorizontal();
        _stateTimer = _stat.AttackDelay;
    }

    private void SwitchToHurt()
    {
        CancelAllCoroutines();
        if (_attackHitbox != null) _attackHitbox.Deactivate();
        _fsm.ChangeState(EnemyFSM.EnemyState.Hurt);
        ApplyKnockback();
        _stateTimer = _fsm.HurtClipLength;
    }

    private void SwitchToDie()
    {
        CancelAllCoroutines();
        _fsm.ChangeState(EnemyFSM.EnemyState.Die);
        _rb.velocity = Vector2.zero;
        StartCoroutine(DieAfterAnimation());
    }

    // ==================== 移动 ====================

    private void MoveInFacingDirection(float speed)
    {
        float dir = _stat.FacingRight ? 1f : -1f;
        _rb.velocity = new Vector2(dir * speed, _rb.velocity.y);
    }

    private void MoveChase()
    {
        Vector2 target = _movingToLastKnown ? _lastKnownPlayerPos : (Vector2)_player.position;
        float dx = target.x - transform.position.x;

        if (Mathf.Abs(dx) < 0.1f)
        {
            StopHorizontal();
            return;
        }

        float moveDir = Mathf.Sign(dx);
        _rb.velocity = new Vector2(moveDir * _stat.ChaseSpeed, _rb.velocity.y);
        FaceDirection(moveDir);
    }

    private void FaceTarget(Vector2 target)
    {
        float dx = target.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.1f)
        {
            FaceDirection(Mathf.Sign(dx));
        }
    }

    private void FaceDirection(float direction)
    {
        if ((_stat.FacingRight && direction < 0) || (!_stat.FacingRight && direction > 0))
        {
            Flip();
        }
    }

    private void Flip()
    {
        _stat.FacingRight = !_stat.FacingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void StopHorizontal()
    {
        _rb.velocity = new Vector2(0f, _rb.velocity.y);
    }

    // ==================== 击退 ====================

    private void ApplyKnockback()
    {
        // 朝远离玩家的方向击退
        float knockDir = _player.position.x > transform.position.x ? -1f : 1f;
        _rb.velocity = new Vector2(knockDir * _stat.KnockbackDistance, _rb.velocity.y);
    }

    // ==================== 协程 ====================

    private void StartTurnAround()
    {
        if (_isTurningAround) return;
        _isTurningAround = true;
        if (_turnAroundCoroutine != null) StopCoroutine(_turnAroundCoroutine);
        _turnAroundCoroutine = StartCoroutine(TurnAroundCoroutine());
    }

    /// <summary>
    /// 转身检测器触发后：延迟0.7秒转身，然后判断Attack/Chase/Patrol
    /// </summary>
    private IEnumerator TurnAroundCoroutine()
    {
        yield return new WaitForSeconds(0.7f);

        Flip();
        _isTurningAround = false;
        _turnAroundCoroutine = null;

        // 转身后判断
        if (_attackTrigger != null && _attackTrigger.TargetInZone)
        {
            StartAttackDelay();
        }
        else if (_visionDetector != null && _visionDetector.TargetInZone)
        {
            SwitchToChase();
        }
        else
        {
            SwitchToPatrol();
        }
    }

    private void StartAttackDelay()
    {
        if (_isWaitingToAttack) return;
        _isWaitingToAttack = true;
        if (_attackDelayCoroutine != null) StopCoroutine(_attackDelayCoroutine);
        _attackDelayCoroutine = StartCoroutine(AttackDelayCoroutine());
    }

    /// <summary>
    /// 攻击启动框触发后：延迟0.2秒切换为Attack状态
    /// </summary>
    private IEnumerator AttackDelayCoroutine()
    {
        yield return new WaitForSeconds(0.2f);
        _isWaitingToAttack = false;
        _attackDelayCoroutine = null;
        SwitchToAttack();
    }

    private IEnumerator DieAfterAnimation()
    {
        yield return new WaitForSeconds(_fsm.DieClipLength);
        // 掉落物品等逻辑可在此扩展
        Destroy(gameObject);
    }

    private void CancelAllCoroutines()
    {
        if (_turnAroundCoroutine != null)
        {
            StopCoroutine(_turnAroundCoroutine);
            _turnAroundCoroutine = null;
        }
        if (_attackDelayCoroutine != null)
        {
            StopCoroutine(_attackDelayCoroutine);
            _attackDelayCoroutine = null;
        }
        _isTurningAround = false;
        _isWaitingToAttack = false;
    }

    // ==================== 伤害 ====================

    /// <summary>
    /// 敌人攻击框命中玩家回调（由AttackHitbox.OnHit触发）
    /// </summary>
    private void OnEnemyAttackHit(Collider2D target)
    {
        var playerSM = target.GetComponentInParent<PlayerStateMachine>();
        if (playerSM == null) return;

        bool isCrit = Random.value < _stat.CriticalRate / 100f;
        int damage = isCrit ? Mathf.RoundToInt(_stat.AttackPower * 1.5f) : _stat.AttackPower;
        Debug.Log($"【敌人攻击玩家】造成 {damage} 伤害{(isCrit ? " 暴击！" : "")}");
        playerSM.TakeHurt(damage);

        // 触发战斗事件供祝福系统响应（反伤等）
        CombatEventBridge.OnEnemyAttackHit(gameObject, playerSM.gameObject, damage, isCrit);
    }

    /// <summary>
    /// 外部调用接口：对敌人造成指定伤害（由玩家攻击框调用）
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (_fsm.CurrentState == EnemyFSM.EnemyState.Die) return;
        ApplyDamage(damage);
    }

    private void ApplyDamage(int damage)
    {
        _stat.CurHP -= damage;

        if (_stat.CurHP <= 0)
        {
            _stat.CurHP = 0;
            // 触发敌人死亡事件供祝福系统响应（击杀回血等）
            var player = GameObject.FindGameObjectWithTag("Player");
            CombatEventBridge.OnEnemyDie(player, gameObject);
            SwitchToDie();
            return;
        }

        SwitchToHurt();
    }
}
