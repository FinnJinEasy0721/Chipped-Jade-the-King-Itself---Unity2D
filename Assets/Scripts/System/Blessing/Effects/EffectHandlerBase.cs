using System;
using System.Collections.Generic;

/// <summary>
/// 效果处理器抽象基类（策略模式）
/// 每种 EffectType 对应一个子类，实现三个生命周期方法
/// 新增效果类型：1) 添加 EffectType 枚举值 2) 继承本类 3) 在工厂注册
/// </summary>
public abstract class EffectHandlerBase
{
    /// <summary>装备时调用（永久效果在此应用修改器）</summary>
    public abstract void OnEquip(BlessingEffectConfig config, BlessingRuntimeContext context);

    /// <summary>卸下时调用（永久效果在此移除修改器）</summary>
    public abstract void OnUnequip(BlessingRuntimeContext context);

    /// <summary>事件触发时调用（仅 TriggerType != Permanent 的效果）</summary>
    public abstract void OnTrigger(BlessingEffectConfig config, BlessingRuntimeContext context, CombatContext combat);
}

/// <summary>
/// 效果处理器工厂 —— EffectType枚举→处理器实例映射
/// 新增效果类型时在此注册
/// </summary>
public static class EffectHandlerFactory
{
    private static readonly Dictionary<EffectType, Func<EffectHandlerBase>> _registry = new()
    {
        { EffectType.StatModifier,   () => new StatModifierHandler() },
        { EffectType.Lifesteal,      () => new LifestealHandler() },
        { EffectType.DotOnHit,      () => new DotOnHitHandler() },
        { EffectType.AuraDamage,    () => new AuraDamageHandler() },
        { EffectType.ReflectDamage, () => new ReflectDamageHandler() },
    };

    /// <summary>根据效果类型创建处理器实例</summary>
    public static EffectHandlerBase Create(EffectType type)
    {
        if (type == EffectType.None) return null;
        return _registry.TryGetValue(type, out var factory) ? factory() : null;
    }

    /// <summary>注册新效果类型（运行时扩展用）</summary>
    public static void Register(EffectType type, Func<EffectHandlerBase> factory)
    {
        _registry[type] = factory;
    }
}
