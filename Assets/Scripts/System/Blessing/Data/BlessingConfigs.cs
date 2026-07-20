using System;
using UnityEngine;

/// <summary>
/// 被动效果配置 —— 策划在Inspector中选择效果类型+触发时机，填写参数
/// 不同效果类型使用不同的参数区域，未使用的参数忽略
/// </summary>
[Serializable]
public class BlessingEffectConfig
{
    [Header("效果类型")]
    public EffectType effectType;
    public EffectTriggerType triggerType;

    [Header("属性修改（仅 EffectType=StatModifier）")]
    public StatType statType;
    public ModifierOp modifierOp;
    public float modifierValue;

    [Header("伤害参数（仅伤害类效果：DotOnHit/AuraDamage/ReflectDamage）")]
    public DamageScaleType damageScaleType = DamageScaleType.Flat;
    [Tooltip("缩放比例，如0.2=20%")]
    public float damageScale = 1f;
    [Tooltip("DOT/Debuff持续时间（秒）")]
    public float duration = 3f;
    [Tooltip("Tick间隔（秒），用于DOT和Aura")]
    public float tickInterval = 1f;

    [Header("范围参数（仅 AuraDamage）")]
    public AreaShape areaShape = AreaShape.Circle;
    public float areaRadius = 3f;
    [Tooltip("矩形宽度（仅 AreaShape=Rectangle）")]
    public float areaWidth = 3f;
    [Tooltip("矩形高度（仅 AreaShape=Rectangle）")]
    public float areaHeight = 2f;
    [Tooltip("true=以玩家为中心，false=以敌人为中心")]
    public bool centerOnPlayer = true;
    [Tooltip("最大目标数量，0=无限")]
    public int maxTargets = 0;

    [Header("回血参数（仅 Lifesteal）")]
    [Tooltip("回血量占伤害的比例，如0.1=10%")]
    public float lifestealRatio = 0.1f;
    [Tooltip("回血量占自身最大血量的比例")]
    public float lifestealMaxHPRatio = 0f;
}

/// <summary>
/// 主动技能配置 —— 策划在Inspector中选择技能类型，填写参数
/// </summary>
[Serializable]
public class BlessingSkillConfig
{
    [Header("技能基础")]
    public SkillType skillType = SkillType.None;
    [Tooltip("技能图标，用于UI显示")]
    public Sprite skillIcon;
    public SkillDurationType durationType = SkillDurationType.OneShot;
    [Tooltip("技能持续时间（秒），仅 Timed 类型生效")]
    public float duration = 5f;
    [Tooltip("冷却时间（秒）")]
    public float cooldown = 60f;

    [Header("血量消耗")]
    public HealthCostType healthCostType = HealthCostType.None;
    [Tooltip("消耗数值（百分比或固定值，取决于 HealthCostType）")]
    public float healthCostValue = 0f;

    [Header("技能伤害")]
    public DamageScaleType damageScaleType = DamageScaleType.Flat;
    public float damageScale = 1f;

    [Header("技能范围")]
    public AreaShape areaShape = AreaShape.Circle;
    public float areaRadius = 5f;
    public float areaWidth = 5f;
    public float areaHeight = 3f;
    public bool centerOnPlayer = true;
    [Tooltip("最大目标数量，0=无限")]
    public int maxTargets = 3;

    [Header("附带效果")]
    [Tooltip("眩晕持续时间（秒），0=不眩晕")]
    public float stunDuration = 0f;
    [Tooltip("击退力度")]
    public float knockbackForce = 0f;

    [Header("属性增强（仅 SkillType=StatBoost）")]
    public StatType boostStat = StatType.CritRate;
    public ModifierOp boostOp = ModifierOp.AddFlat;
    public float boostValue = 0.5f;
}
