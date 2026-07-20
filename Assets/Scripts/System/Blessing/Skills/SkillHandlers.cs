using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 范围伤害技能处理器（如闪电：对范围内N名敌人造成伤害+附带眩晕/击退）
/// 支持持续型（Timed/Toggle/ManualClose）每Tick重复造成伤害
/// </summary>
public class DamageAreaSkillHandler : SkillHandlerBase
{
    private BlessingSkillConfig _config;
    private BlessingRuntimeContext _ctx;

    public override void Activate(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        _config = config;
        _ctx = context;
        ApplyDamage(config, context);
    }

    public override void Deactivate(BlessingRuntimeContext context)
    {
        _config = null;
        _ctx = null;
    }

    public override void Tick(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        // 持续型技能每帧Tick造成范围伤害
        ApplyDamage(config, context);
    }

    private void ApplyDamage(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        var targets = AreaUtility.GetTargetsInArea(
            context.PlayerTransform.position,
            config.areaShape,
            config.areaRadius,
            config.areaWidth,
            config.areaHeight,
            config.maxTargets);

        foreach (var target in targets)
        {
            var enemyStat = target.GetComponent<EnemyStatBase>();
            if (enemyStat == null) continue;

            int damage = DamageCalculator.CalculateDamage(
                config.damageScaleType, config.damageScale,
                context.PlayerStat, enemyStat);
            enemyStat.CurHP -= damage;

            // 附带眩晕
            if (config.stunDuration > 0f)
            {
                var buff = target.GetComponent<BuffContainer>();
                buff?.AddBuff(new BuffInstance
                {
                    buffType = BuffType.Stun,
                    duration = config.stunDuration,
                    tickInterval = 0f
                });
            }

            // 附带击退
            if (config.knockbackForce > 0f)
            {
                var rb = target.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 dir = (target.transform.position - context.PlayerTransform.position).normalized;
                    rb.AddForce(dir * config.knockbackForce, ForceMode2D.Impulse);
                }
            }
        }
    }
}

/// <summary>
/// 无敌技能处理器 —— 激活后玩家无敌，停用后恢复
/// </summary>
public class InvincibilitySkillHandler : SkillHandlerBase
{
    public override void Activate(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        context.PlayerStat.SetInvincible(true);
    }

    public override void Deactivate(BlessingRuntimeContext context)
    {
        context.PlayerStat.SetInvincible(false);
    }
}

/// <summary>
/// 属性增强技能处理器（如暴击率+50%持续30秒）
/// 激活时添加属性修改器，停用时移除
/// </summary>
public class StatBoostSkillHandler : SkillHandlerBase
{
    public override void Activate(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        // 以this为来源，停用时按来源移除
        context.Modifiers.Add(config.boostStat, config.boostOp, config.boostValue, this);
    }

    public override void Deactivate(BlessingRuntimeContext context)
    {
        context.Modifiers.RemoveAllFromSource(this);
    }
}

/// <summary>
/// 敌人减益技能处理器 —— 对范围内敌人施加减益Buff（减攻击力/减速等）
/// </summary>
public class EnemyDebuffSkillHandler : SkillHandlerBase
{
    public override void Activate(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        var targets = AreaUtility.GetTargetsInArea(
            context.PlayerTransform.position,
            config.areaShape,
            config.areaRadius,
            config.areaWidth,
            config.areaHeight,
            config.maxTargets);

        foreach (var target in targets)
        {
            var buff = target.GetComponent<BuffContainer>();
            buff?.AddBuff(new BuffInstance
            {
                buffType = BuffType.StatReduction,
                duration = config.duration,
                tickInterval = 0f,
                damageScaleType = config.damageScaleType,
                damageScale = config.damageScale
            });
        }
    }

    public override void Deactivate(BlessingRuntimeContext context) { }
}

/// <summary>
/// 击退+眩晕技能处理器 —— 对范围内敌人施加击退力和眩晕
/// </summary>
public class KnockbackSkillHandler : SkillHandlerBase
{
    public override void Activate(BlessingSkillConfig config, BlessingRuntimeContext context)
    {
        var targets = AreaUtility.GetTargetsInArea(
            context.PlayerTransform.position,
            config.areaShape,
            config.areaRadius,
            config.areaWidth,
            config.areaHeight,
            config.maxTargets);

        foreach (var target in targets)
        {
            // 击退
            var rb = target.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = (target.transform.position - context.PlayerTransform.position).normalized;
                rb.AddForce(dir * config.knockbackForce, ForceMode2D.Impulse);
            }

            // 眩晕
            if (config.stunDuration > 0f)
            {
                var buff = target.GetComponent<BuffContainer>();
                buff?.AddBuff(new BuffInstance
                {
                    buffType = BuffType.Stun,
                    duration = config.stunDuration,
                    tickInterval = 0f
                });
            }
        }
    }

    public override void Deactivate(BlessingRuntimeContext context) { }
}
