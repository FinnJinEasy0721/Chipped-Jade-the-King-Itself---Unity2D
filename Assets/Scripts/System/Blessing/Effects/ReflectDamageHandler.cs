using UnityEngine;

/// <summary>
/// 反伤效果处理器 —— 玩家受击时对攻击者造成伤害
/// 触发时机：OnPlayerHurt
/// </summary>
public class ReflectDamageHandler : EffectHandlerBase
{
    public override void OnEquip(BlessingEffectConfig config, BlessingRuntimeContext context) { }
    public override void OnUnequip(BlessingRuntimeContext context) { }

    public override void OnTrigger(BlessingEffectConfig config, BlessingRuntimeContext context, CombatContext combat)
    {
        if (combat.Attacker == null) return;

        var enemyStat = combat.Attacker.GetComponent<EnemyStatBase>();
        if (enemyStat == null) return;

        int damage = DamageCalculator.CalculateDamage(
            config.damageScaleType, config.damageScale,
            context.PlayerStat, enemyStat);

        enemyStat.CurHP -= damage;
    }
}
