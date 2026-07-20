using UnityEngine;

/// <summary>
/// 效果分类：无 / 增益 / 减益
/// 策划在 Inspector 中为每个效果配置分类，决定显示在 Buff 栏还是 Debuff 栏
/// </summary>
public enum EffectCategory
{
    None,
    Buff,
    Debuff
}

/// <summary>
/// 道具效果抽象基类（策略模式）
/// 每种效果是一个独立的 ScriptableObject 子类
/// 新增效果只需继承此类，在 ItemData 中拖拽配置即可
/// </summary>
public abstract class ItemEffect : ScriptableObject
{
    [TextArea]
    public string BriefDescription; // 简要效果描述，如"攻击回5%MaxHP"，用于 UI Buff/Debuff 栏显示

    public EffectCategory Category = EffectCategory.None; // 效果分类（Buff/Debuff），策划在 Inspector 配置

    /// <summary>
    /// 对目标施加效果（即时生效或启动定时逻辑）
    /// </summary>
    public abstract void Apply(GameObject target);

    /// <summary>
    /// 移除效果（用于定时增益到期恢复属性等场景）
    /// 即时效果（如回血）可留空
    /// </summary>
    public abstract void Remove(GameObject target);

    /// <summary>
    /// 向 BuffTracker 注册此效果，使其显示在 UI 的 Buff/Debuff 栏
    /// 子类在 Apply 中调用，传入持续时间（即时效果传短暂闪现时间）
    /// </summary>
    /// <param name="isInstant">是否为即时效果（一次性），为 true 时 UI 显示"已使用"而非读秒</param>
    protected void RegisterBuff(GameObject target, float duration, bool isInstant = false)
    {
        var tracker = target.GetComponent<BuffTracker>();
        if (tracker != null)
            tracker.Register(this, duration, isInstant);
    }

    /// <summary>
    /// 从 BuffTracker 注销此效果，使其从 UI 移除
    /// 子类在 Remove 中调用
    /// </summary>
    protected void UnregisterBuff(GameObject target)
    {
        var tracker = target.GetComponent<BuffTracker>();
        if (tracker != null)
            tracker.Unregister(this);
    }
}
