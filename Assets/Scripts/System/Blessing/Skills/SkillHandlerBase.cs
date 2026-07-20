using System;
using System.Collections.Generic;

/// <summary>
/// 技能处理器抽象基类（策略模式）
/// 每种 SkillType 对应一个子类，实现 Activate/Deactivate/Tick
/// 新增技能类型：1) 添加 SkillType 枚举值 2) 继承本类 3) 在工厂注册
/// </summary>
public abstract class SkillHandlerBase
{
    /// <summary>激活技能（动画播放完毕后调用）</summary>
    public abstract void Activate(BlessingSkillConfig config, BlessingRuntimeContext context);

    /// <summary>停用技能（持续型技能到期/切换/手动关闭时调用）</summary>
    public abstract void Deactivate(BlessingRuntimeContext context);

    /// <summary>每帧Tick（持续型技能的持续效果，如每帧造成范围伤害）</summary>
    public virtual void Tick(BlessingSkillConfig config, BlessingRuntimeContext context) { }
}

/// <summary>
/// 技能处理器工厂 —— SkillType枚举→处理器实例映射
/// </summary>
public static class SkillHandlerFactory
{
    private static readonly Dictionary<SkillType, Func<SkillHandlerBase>> _registry = new()
    {
        { SkillType.DamageArea,   () => new DamageAreaSkillHandler() },
        { SkillType.Invincibility, () => new InvincibilitySkillHandler() },
        { SkillType.StatBoost,    () => new StatBoostSkillHandler() },
        { SkillType.EnemyDebuff,  () => new EnemyDebuffSkillHandler() },
        { SkillType.Knockback,    () => new KnockbackSkillHandler() },
    };

    public static SkillHandlerBase Create(SkillType type)
    {
        return _registry.TryGetValue(type, out var factory) ? factory() : null;
    }

    /// <summary>注册新技能类型</summary>
    public static void Register(SkillType type, Func<SkillHandlerBase> factory)
    {
        _registry[type] = factory;
    }
}
