using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害计算工具 —— 根据缩放类型计算最终伤害
/// 支持：固定值 / 各段攻击力% / 敌人最大血量% / 敌人当前血量%
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// 根据缩放类型计算伤害
    /// </summary>
    /// <param name="scaleType">缩放类型</param>
    /// <param name="scale">缩放比例（如0.2=20%）</param>
    /// <param name="playerStat">玩家属性（用于获取各段攻击力）</param>
    /// <param name="enemyStat">敌人属性（用于获取血量百分比）</param>
    public static int CalculateDamage(DamageScaleType scaleType, float scale,
        PlayerStat playerStat, EnemyStatBase enemyStat)
    {
        if (playerStat == null || enemyStat == null) return 0;

        return scaleType switch
        {
            DamageScaleType.Flat            => Mathf.RoundToInt(scale),
            DamageScaleType.PlayerAttack1Pct => Mathf.RoundToInt(playerStat.GetBaseAttackPower(1) * scale),
            DamageScaleType.PlayerAttack2Pct => Mathf.RoundToInt(playerStat.GetBaseAttackPower(2) * scale),
            DamageScaleType.PlayerAttack3Pct => Mathf.RoundToInt(playerStat.GetBaseAttackPower(3) * scale),
            DamageScaleType.PlayerHeavyPct   => Mathf.RoundToInt(playerStat.GetBaseAttackPower(0) * scale),
            DamageScaleType.EnemyMaxHPPct     => Mathf.RoundToInt(enemyStat.MaxHP * scale),
            DamageScaleType.EnemyCurHPPct    => Mathf.RoundToInt(enemyStat.CurHP * scale),
            _ => 0
        };
    }
}

/// <summary>
/// 范围检测工具 —— 根据形状检测范围内的敌人
/// </summary>
public static class AreaUtility
{
    /// <summary>
    /// 获取范围内的敌人列表
    /// </summary>
    /// <param name="center">范围中心</param>
    /// <param name="shape">形状（圆形/矩形）</param>
    /// <param name="radius">圆形半径</param>
    /// <param name="width">矩形宽度</param>
    /// <param name="height">矩形高度</param>
    /// <param name="maxTargets">最大目标数，0=无限</param>
    public static List<GameObject> GetTargetsInArea(Vector3 center, AreaShape shape,
        float radius, float width, float height, int maxTargets)
    {
        Collider2D[] hits = shape switch
        {
            AreaShape.Circle => Physics2D.OverlapCircleAll(center, radius),
            AreaShape.Rectangle => Physics2D.OverlapAreaAll(
                center - new Vector3(width / 2f, height / 2f, 0f),
                center + new Vector3(width / 2f, height / 2f, 0f)),
            _ => System.Array.Empty<Collider2D>()
        };

        var result = new List<GameObject>();
        foreach (var hit in hits)
        {
            // 通过EnemyStatBase组件判断是否为敌人
            if (hit.GetComponentInParent<EnemyStatBase>() == null) continue;
            var root = hit.transform.root.gameObject;
            if (!result.Contains(root))
                result.Add(root);
        }

        // 限制数量：取距离中心最近的N个
        if (maxTargets > 0 && result.Count > maxTargets)
        {
            result.Sort((a, b) =>
                Vector3.Distance(a.transform.position, center)
                .CompareTo(Vector3.Distance(b.transform.position, center)));
            result.RemoveRange(maxTargets, result.Count - maxTargets);
        }

        return result;
    }
}
