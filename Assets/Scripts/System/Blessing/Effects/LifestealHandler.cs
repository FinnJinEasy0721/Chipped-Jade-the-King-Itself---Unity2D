using UnityEngine;

/// <summary>
/// 攻击回血效果处理器 —— 命中敌人时按伤害比例和/或最大血量比例回血
/// 触发时机：OnAttackHit
/// </summary>
public class LifestealHandler : EffectHandlerBase
{
    public override void OnEquip(BlessingEffectConfig config, BlessingRuntimeContext context) { }
    public override void OnUnequip(BlessingRuntimeContext context) { }

    public override void OnTrigger(BlessingEffectConfig config, BlessingRuntimeContext context, CombatContext combat)
    {
        int heal = 0;

        // 按伤害比例回血
        if (config.lifestealRatio > 0f)
            heal += Mathf.RoundToInt(combat.Damage * config.lifestealRatio);

        // 按自身最大血量比例回血
        if (config.lifestealMaxHPRatio > 0f)
            heal += Mathf.RoundToInt(context.PlayerStat.GetMaxHP() * config.lifestealMaxHPRatio);

        if (heal > 0)
            context.PlayerStat.Heal(heal);
    }
}
