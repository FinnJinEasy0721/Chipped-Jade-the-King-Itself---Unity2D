using System.Collections;
using UnityEngine;

/// <summary>
/// 增益类型枚举：移速增益 / 伤害增益
/// </summary>
public enum BoostType
{
    SpeedBoost,
    DamageBoost
}

/// <summary>
/// 定时增益效果：使用后按倍率增强属性，持续时间结束后自动恢复
/// 通过挂载 TimedBoostRunner 组件在目标上运行协程
/// </summary>
[CreateAssetMenu(fileName = "TimedBoostEffect_", menuName = "Item/TimedBoostEffect")]
public class TimedBoostEffect : ItemEffect
{
    public BoostType BoostType;
    public float BoostValue = 1.5f; // 增益倍率（乘法）
    public float Duration = 5f; // 持续时间（秒）

    public override void Apply(GameObject target)
    {
        var stat = target.GetComponent<PlayerStat>();
        if (stat == null) return;

        // 根据增益类型修改对应属性
        switch (BoostType)
        {
            case BoostType.SpeedBoost:
                stat.Base_PlayerSpeed *= BoostValue;
                Debug.Log($"[TimedBoostEffect] 移速增加 x{BoostValue}，持续 {Duration}秒");
                break;
            case BoostType.DamageBoost:
                Debug.Log($"[TimedBoostEffect] 增伤 x{BoostValue}，持续 {Duration}秒");
                break;
        }

        // 在目标上挂载协程载体组件，用于驱动倒计时协程
        var runner = target.AddComponent<TimedBoostRunner>();
        runner.StartCoroutine(BoostCoroutine(target, runner));
    }

    /// <summary>
    /// 增益到期后恢复原始属性值
    /// </summary>
    public override void Remove(GameObject target)
    {
        var stat = target.GetComponent<PlayerStat>();
        if (stat == null) return;

        switch (BoostType)
        {
            case BoostType.SpeedBoost:
                stat.Base_PlayerSpeed /= BoostValue;
                Debug.Log($"[TimedBoostEffect] 移速增益结束，恢复为 {stat.Base_PlayerSpeed}");
                break;
            case BoostType.DamageBoost:
                Debug.Log($"[TimedBoostEffect] 增伤增益结束");
                break;
        }
    }

    /// <summary>
    /// 增益倒计时协程：等待 Duration 秒后移除效果并清理载体组件
    /// </summary>
    private IEnumerator BoostCoroutine(GameObject target, TimedBoostRunner runner)
    {
        yield return new WaitForSeconds(Duration);
        Remove(target);
        Destroy(runner);
    }

    /// <summary>
    /// 协程载体组件（挂到玩家身上以运行协程）
    /// </summary>
    private class TimedBoostRunner : MonoBehaviour { }
}
