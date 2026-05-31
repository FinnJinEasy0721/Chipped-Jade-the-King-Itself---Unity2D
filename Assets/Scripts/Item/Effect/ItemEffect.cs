using UnityEngine;

/// <summary>
/// 道具效果抽象基类（策略模式）
/// 每种效果是一个独立的 ScriptableObject 子类
/// 新增效果只需继承此类，在 ItemData 中拖拽配置即可
/// </summary>
public abstract class ItemEffect : ScriptableObject
{
    /// <summary>
    /// 对目标施加效果（即时生效或启动定时逻辑）
    /// </summary>
    public abstract void Apply(GameObject target);

    /// <summary>
    /// 移除效果（用于定时增益到期恢复属性等场景）
    /// 即时效果（如回血）可留空
    /// </summary>
    public abstract void Remove(GameObject target);
}
