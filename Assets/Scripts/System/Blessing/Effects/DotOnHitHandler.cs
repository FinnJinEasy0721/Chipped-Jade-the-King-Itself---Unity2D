using UnityEngine;

/// <summary>
/// 命中施加DOT效果处理器 —— 攻击命中时给敌人BuffContainer添加持续伤害Buff
/// 触发时机：OnAttackHit
/// 伤害计算支持多种缩放类型（各段攻击力%/敌人血量%等），由DamageCalculator在Tick时实时计算
/// </summary>
public class DotOnHitHandler : EffectHandlerBase
{
    public override void OnEquip(BlessingEffectConfig config, BlessingRuntimeContext context) { }
    public override void OnUnequip(BlessingRuntimeContext context) { }

    public override void OnTrigger(BlessingEffectConfig config, BlessingRuntimeContext context, CombatContext combat)
    {
        if (combat.Target == null) return;

        var buffContainer = combat.Target.GetComponent<BuffContainer>();
        if (buffContainer == null) return;

        // 创建DOT Buff实例
        var dotBuff = new BuffInstance
        {
            buffType = BuffType.DOT,
            duration = config.duration,
            tickInterval = config.tickInterval,
            damageScaleType = config.damageScaleType,
            damageScale = config.damageScale,
            source = context.PlayerStat,
        };
        buffContainer.AddBuff(dotBuff);
    }
}
