using UnityEngine;

/// <summary>
/// 战斗上下文 —— 作为战斗事件的参数传递
/// 包含一次战斗交互所需的全部信息
/// </summary>
public struct CombatContext
{
    /// <summary>攻击者GameObject</summary>
    public GameObject Attacker;
    /// <summary>被攻击者GameObject</summary>
    public GameObject Target;
    /// <summary>造成的伤害值</summary>
    public int Damage;
    /// <summary>是否暴击</summary>
    public bool IsCrit;
    /// <summary>攻击段数（0=重击, 1-3=连击段）</summary>
    public int ComboStage;
}
