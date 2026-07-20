using UnityEngine;

/// <summary>
/// 战斗事件桥接器 —— 将战斗系统的事件接入EventCenter
/// 在战斗发生时调用对应静态方法，触发全局事件供祝福系统响应
/// </summary>
public static class CombatEventBridge
{
    /// <summary>玩家攻击命中敌人时调用</summary>
    public static void OnPlayerAttackHit(GameObject player, GameObject enemy,
        int damage, bool isCrit, int comboStage)
    {
        var ctx = new CombatContext
        {
            Attacker = player,
            Target = enemy,
            Damage = damage,
            IsCrit = isCrit,
            ComboStage = comboStage
        };
        EventCenter.Instance.Invoke(EventName.EnemyHurt, ctx);
    }

    /// <summary>敌人攻击命中玩家时调用</summary>
    public static void OnEnemyAttackHit(GameObject enemy, GameObject player,
        int damage, bool isCrit)
    {
        var ctx = new CombatContext
        {
            Attacker = enemy,
            Target = player,
            Damage = damage,
            IsCrit = isCrit,
            ComboStage = 0
        };
        EventCenter.Instance.Invoke(EventName.PlayerHurt, ctx);
    }

    /// <summary>敌人死亡时调用</summary>
    public static void OnEnemyDie(GameObject player, GameObject enemy)
    {
        var ctx = new CombatContext
        {
            Attacker = player,
            Target = enemy,
            Damage = 0,
            IsCrit = false,
            ComboStage = 0
        };
        EventCenter.Instance.Invoke(EventName.EnemyDie, ctx);
    }

    /// <summary>玩家死亡时调用</summary>
    public static void OnPlayerDie(GameObject enemy, GameObject player)
    {
        var ctx = new CombatContext
        {
            Attacker = enemy,
            Target = player,
            Damage = 0,
            IsCrit = false,
            ComboStage = 0
        };
        EventCenter.Instance.Invoke(EventName.PlayerDie, ctx);
    }
}
