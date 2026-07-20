using UnityEngine;

/// <summary>
/// 祝福运行时上下文 —— 传递给所有Handler，避免Handler直接查找组件
/// 由BlessingManager在Awake时构造，整个生命周期内复用
/// </summary>
public class BlessingRuntimeContext
{
    /// <summary>玩家GameObject</summary>
    public GameObject Player;
    /// <summary>玩家属性组件</summary>
    public PlayerStat PlayerStat;
    /// <summary>属性修改器系统</summary>
    public StatModifierSystem Modifiers;
    /// <summary>祝福数据库</summary>
    public BlessingDatabase Database;
    /// <summary>玩家Transform（缓存，避免每帧GetComponent）</summary>
    public Transform PlayerTransform;
}
