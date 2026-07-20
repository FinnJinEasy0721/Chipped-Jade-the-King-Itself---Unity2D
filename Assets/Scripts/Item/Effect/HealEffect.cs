using UnityEngine;

/// <summary>
/// 治疗效果：使用后立即回复目标一定量的 HP
/// 属于即时效果，Remove 无需执行任何逻辑
/// </summary>
[CreateAssetMenu(fileName = "HealEffect_", menuName = "Item/HealEffect")]
public class HealEffect : ItemEffect
{
    public int HealAmount = 10;

    public override void Apply(GameObject target)
    {
        var stat = target.GetComponent<PlayerStat>();
        if (stat == null) return;

        // 回血但不超过 HP 上限
        stat.Curr_HP = Mathf.Min(stat.Curr_HP + HealAmount, stat.GetMaxHP());
        Debug.Log($"[HealEffect] 恢复 {HealAmount} HP，当前 HP: {stat.Curr_HP}/{stat.GetMaxHP()}");

        // 即时效果：在 UI 上短暂闪现 2 秒，显示"已使用"而非读秒
        RegisterBuff(target, 2f, true);
    }

    public override void Remove(GameObject target)
    {
        // 即时效果，无需移除
    }
}
