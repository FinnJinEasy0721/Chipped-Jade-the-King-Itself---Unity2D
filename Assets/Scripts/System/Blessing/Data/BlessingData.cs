using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 单个祝福的完整数据定义 —— 策划在 BlessingDatabase SO 的列表中配置
/// 一个祝福可同时包含多个被动效果 + 一个主动技能
/// </summary>
[Serializable]
public class BlessingData
{
    [Header("基础信息")]
    [Tooltip("唯一标识符，用于存档读取")]
    public string blessingID;
    public string blessingName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    [Tooltip("购买价格（金币）")]
    public int cost;

    [Header("被动效果列表")]
    [Tooltip("可包含多个被动效果，同时生效")]
    public List<BlessingEffectConfig> passiveEffects = new();

    [Header("主动技能配置")]
    public BlessingSkillConfig skillConfig = new();

    /// <summary>空祝福（无任何效果），用作初始默认状态</summary>
    public static BlessingData None { get; } = new()
    {
        blessingID = "None",
        blessingName = "无",
        description = "未装备祝福",
        cost = 0,
    };
}
