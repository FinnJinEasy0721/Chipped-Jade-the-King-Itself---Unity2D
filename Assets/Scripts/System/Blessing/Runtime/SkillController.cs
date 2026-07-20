using UnityEngine;

/// <summary>
/// 主动技能控制器 —— 管理CD、血量消耗、技能持续生命周期
/// 由BlessingManager创建并驱动，不直接读取输入（由PlayerStateMachine驱动）
/// </summary>
public class SkillController
{
    private readonly BlessingRuntimeContext _context;
    private BlessingSkillConfig _config;
    private SkillHandlerBase _handler;

    // CD计时
    private float _cooldownTimer;

    /// <summary>CD剩余时间（秒）</summary>
    public float CooldownRemaining => _cooldownTimer;
    /// <summary>CD总时间（秒）</summary>
    public float CooldownTotal => _config?.cooldown ?? 0f;
    /// <summary>技能是否冷却完毕</summary>
    public bool IsReady => _cooldownTimer <= 0f;
    /// <summary>技能是否处于激活状态</summary>
    public bool IsActive { get; private set; }
    /// <summary>是否有技能配置</summary>
    public bool HasSkill => _config != null && _config.skillType != SkillType.None;

    // Timed型持续时间
    private float _durationTimer;

    public SkillController(BlessingRuntimeContext context)
    {
        _context = context;
    }

    /// <summary>设置当前技能配置（切换祝福时调用）</summary>
    public void SetupSkill(BlessingSkillConfig config)
    {
        ClearSkill();
        _config = config;
        if (_config != null && _config.skillType != SkillType.None)
        {
            _handler = SkillHandlerFactory.Create(_config.skillType);
        }
        _cooldownTimer = 0f;
        IsActive = false;
    }

    /// <summary>清除技能配置（卸下祝福时调用）</summary>
    public void ClearSkill()
    {
        if (IsActive) DeactivateSkill();
        _config = null;
        _handler = null;
    }

    /// <summary>
    /// 尝试激活技能（由PlayerStateMachine.TryUseBlessing调用）
    /// 返回true表示可以进入UseBlessing动画状态
    /// </summary>
    public bool TryActivate()
    {
        if (!HasSkill || _handler == null) return false;

        // Toggle型：已激活时再次按下→关闭
        if (_config.durationType == SkillDurationType.Toggle && IsActive)
        {
            DeactivateSkill();
            return false; // 不进入UseBlessing动画
        }

        // CD检查
        if (!IsReady) return false;

        // 血量消耗检查
        if (!TryPayHealthCost()) return false;

        return true;
    }

    /// <summary>技能动画播放完毕后调用（实际激活效果）</summary>
    public void OnSkillAnimationComplete()
    {
        if (!HasSkill || _handler == null) return;

        _handler.Activate(_config, _context);
        IsActive = true;
        _cooldownTimer = _config.cooldown;

        // OneShot型：立即结束
        if (_config.durationType == SkillDurationType.OneShot)
        {
            _handler.Deactivate(_context);
            IsActive = false;
        }
        // Timed型：启动持续时间计时
        else if (_config.durationType == SkillDurationType.Timed)
        {
            _durationTimer = _config.duration;
        }
        // Toggle / ManualClose：保持激活，等待关闭条件
    }

    /// <summary>停用技能</summary>
    public void DeactivateSkill()
    {
        if (!IsActive || _handler == null) return;
        _handler.Deactivate(_context);
        IsActive = false;
    }

    /// <summary>每帧更新（由BlessingManager.Update调用）</summary>
    public void CustomUpdate()
    {
        // CD倒计时
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;

        if (!IsActive || _config == null) return;

        // Timed型：持续时间倒计时
        if (_config.durationType == SkillDurationType.Timed)
        {
            _durationTimer -= Time.deltaTime;
            _handler.Tick(_config, _context);

            if (_durationTimer <= 0f)
                DeactivateSkill();
        }
        else
        {
            // Toggle / ManualClose：持续Tick
            _handler.Tick(_config, _context);
        }
    }

    /// <summary>检查并支付血量消耗</summary>
    private bool TryPayHealthCost()
    {
        if (_config.healthCostType == HealthCostType.None) return true;

        var stat = _context.PlayerStat;
        float cost = _config.healthCostType switch
        {
            HealthCostType.CurrentHPPct => stat.Curr_HP * _config.healthCostValue,
            HealthCostType.MaxHPPct    => stat.GetMaxHP() * _config.healthCostValue,
            HealthCostType.Flat        => _config.healthCostValue,
            _ => 0f
        };

        // 血量不足则无法释放
        if (stat.Curr_HP <= cost) return false;
        stat.TakeDamage(Mathf.RoundToInt(cost));
        return true;
    }
}
