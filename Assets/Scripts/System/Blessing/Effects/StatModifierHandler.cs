/// <summary>
/// 属性修改效果处理器 —— 装备时添加修改器，卸下时由BlessingManager统一移除
/// 支持所有 StatType + ModifierOp 组合（+伤害/+暴击/+生命上限/+移速等）
/// </summary>
public class StatModifierHandler : EffectHandlerBase
{
    public override void OnEquip(BlessingEffectConfig config, BlessingRuntimeContext context)
    {
        // 以config自身为来源标记，卸下时BlessingManager.RemoveAllFromSource统一移除
        context.Modifiers.Add(config.statType, config.modifierOp, config.modifierValue, config);
    }

    public override void OnUnequip(BlessingRuntimeContext context)
    {
        // 移除由 BlessingManager.UnequipCurrent 中的 RemoveAllFromSource 统一处理
    }

    public override void OnTrigger(BlessingEffectConfig config, BlessingRuntimeContext context, CombatContext combat)
    {
        // 永久效果，不响应事件触发
    }
}
