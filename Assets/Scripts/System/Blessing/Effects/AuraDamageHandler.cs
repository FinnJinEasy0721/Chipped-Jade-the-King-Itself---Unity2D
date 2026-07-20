using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 范围光环效果处理器 —— 装备后在玩家周围创建持续伤害区域
/// 触发方式：由BlessingManager的Tick系统每tickInterval调用OnTrigger
/// 每Tick检测范围内敌人并造成伤害，伤害由DamageCalculator根据缩放类型计算
/// </summary>
public class AuraDamageHandler : EffectHandlerBase
{
    public override void OnEquip(BlessingEffectConfig config, BlessingRuntimeContext context) { }
    public override void OnUnequip(BlessingRuntimeContext context) { }

    public override void OnTrigger(BlessingEffectConfig config, BlessingRuntimeContext context, CombatContext combat)
    {
        if (context.PlayerTransform == null) return;

        // 检测范围内的敌人
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
        }
    }
}
