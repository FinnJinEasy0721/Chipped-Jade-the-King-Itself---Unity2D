/// <summary>
/// 祝福系统所有枚举定义
/// </summary>

/// <summary>被动效果类型 —— 决定效果做什么</summary>
public enum EffectType
{
    None,           // 无效果（空占位，策划可留空不配置）
    StatModifier,   // 属性修改（+伤害/+暴击率/+生命上限/+移速等）
    Lifesteal,      // 攻击回血（命中时按比例回复）
    DotOnHit,       // 命中施加持续伤害（DOT debuff）
    AuraDamage,     // 范围持续伤害光环（每Tick对范围内敌人造成伤害）
    ReflectDamage,  // 反伤（受击时对攻击者造成伤害）
}

/// <summary>效果触发时机 —— 决定效果何时生效</summary>
public enum EffectTriggerType
{
    Permanent,      // 永久生效（装备即生效，卸下即移除）
    OnAttackHit,    // 玩家攻击命中敌人时
    OnPlayerHurt,   // 玩家受击时
    OnEnemyKill,    // 击杀敌人时
    Tick,           // 每帧/每间隔触发（光环等）
}

/// <summary>主动技能类型</summary>
public enum SkillType
{
    None,               // 无主动技能（纯被动祝福）
    DamageArea,         // 范围伤害（可附带眩晕/击退）
    Invincibility,      // 无敌
    StatBoost,          // 属性增强（暴击率/暴击伤害/移速等，持续一段时间）
    EnemyDebuff,        // 敌人减益（减攻击力/减速等）
    Knockback,          // 击退+眩晕
}

/// <summary>技能持续类型</summary>
public enum SkillDurationType
{
    Timed,       // 持续N秒后自动结束
    OneShot,     // 瞬间生效，无持续时间
    Toggle,      // 再次按L键切换开关
    ManualClose, // 需手动关闭（长按等条件）
}

/// <summary>血量消耗类型</summary>
public enum HealthCostType
{
    None,           // 不消耗血量
    CurrentHPPct,   // 当前血量的百分比
    MaxHPPct,       // 最大血量的百分比
    Flat,           // 固定数值
}

/// <summary>伤害缩放类型 —— DOT/Aura/技能的伤害如何计算</summary>
public enum DamageScaleType
{
    Flat,               // 固定数值
    PlayerAttack1Pct,   // 玩家第一段攻击力的百分比
    PlayerAttack2Pct,   // 玩家第二段攻击力的百分比
    PlayerAttack3Pct,   // 玩家第三段攻击力的百分比
    PlayerHeavyPct,     // 玩家蓄力重击攻击力的百分比
    EnemyMaxHPPct,      // 敌人最大血量的百分比
    EnemyCurHPPct,      // 敌人当前血量的百分比
}

/// <summary>可修改的属性类型</summary>
public enum StatType
{
    MaxHP,          // 最大生命值
    MoveSpeed,      // 移动速度
    JumpForce,      // 跳跃力
    CritRate,       // 暴击率
    CritMultiplier, // 暴击伤害倍率
    AttackPower,    // 攻击力百分比加成
    HeavyAttackCD,  // 重击冷却
}

/// <summary>属性修改操作类型</summary>
public enum ModifierOp
{
    AddFlat,       // 加固定值（如 +50 MaxHP）
    AddPercentage, // 加百分比（如 +20% 暴击率，基于基础值）
    Multiply,      // 乘法（如 *1.5 移速）
    Override,      // 覆盖（如 设暴击率=0）
}

/// <summary>范围形状</summary>
public enum AreaShape
{
    Circle,    // 圆形
    Rectangle, // 矩形
}
