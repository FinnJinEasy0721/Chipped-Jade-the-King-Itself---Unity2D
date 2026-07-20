using System.Collections.Generic;
using UnityEngine;

/// <summary>Buff类型 —— 挂载在敌人身上的持续效果分类</summary>
public enum BuffType
{
    DOT,            // 持续伤害
    Stun,           // 眩晕
    StatReduction,  // 属性削弱（减攻击力等）
    Slow,           // 减速
}

/// <summary>
/// Buff实例 —— 单个持续效果的运行时数据
/// </summary>
public class BuffInstance
{
    /// <summary>Buff类型</summary>
    public BuffType buffType;
    /// <summary>总持续时间（秒）</summary>
    public float duration;
    /// <summary>Tick间隔（秒），0=无Tick</summary>
    public float tickInterval;
    /// <summary>当前Tick计时器</summary>
    public float tickTimer;
    /// <summary>DOT伤害缩放类型</summary>
    public DamageScaleType damageScaleType;
    /// <summary>缩放比例</summary>
    public float damageScale;
    /// <summary>来源对象（PlayerStat等，用于获取玩家攻击力）</summary>
    public object source;
}

/// <summary>
/// 敌人Buff容器 —— 挂载于敌人，管理所有持续效果
/// 自动Tick、到期清理，供外部查询Buff状态
/// </summary>
[RequireComponent(typeof(EnemyStatBase))]
public class BuffContainer : MonoBehaviour
{
    private readonly List<BuffInstance> _buffs = new();
    private EnemyStatBase _stat;

    private void Awake() => _stat = GetComponent<EnemyStatBase>();

    /// <summary>添加Buff</summary>
    public void AddBuff(BuffInstance buff)
    {
        buff.tickTimer = buff.tickInterval;
        _buffs.Add(buff);
    }

    private void Update()
    {
        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            var buff = _buffs[i];
            buff.duration -= Time.deltaTime;

            // Tick
            if (buff.tickInterval > 0f)
            {
                buff.tickTimer -= Time.deltaTime;
                if (buff.tickTimer <= 0f)
                {
                    ApplyTick(buff);
                    buff.tickTimer = buff.tickInterval;
                }
            }

            // 到期移除
            if (buff.duration <= 0f)
                _buffs.RemoveAt(i);
        }
    }

    /// <summary>执行单次Tick效果</summary>
    private void ApplyTick(BuffInstance buff)
    {
        switch (buff.buffType)
        {
            case BuffType.DOT:
                int damage = DamageCalculator.CalculateDamage(
                    buff.damageScaleType, buff.damageScale,
                    buff.source as PlayerStat, _stat);
                _stat.CurHP -= damage;
                // 触发EnemyHurt事件供祝福效果响应
                EventCenter.Instance.Invoke(EventName.EnemyHurt,
                    new CombatContext
                    {
                        Attacker = buff.source as GameObject,
                        Target = gameObject,
                        Damage = damage,
                        IsCrit = false,
                        ComboStage = 0
                    });
                break;
            // Stun/StatReduction/Slow 不需要Tick伤害，在敌人AI中查询HasBuff即可
        }
    }

    /// <summary>是否有指定类型的Buff</summary>
    public bool HasBuff(BuffType type)
    {
        for (int i = 0; i < _buffs.Count; i++)
        {
            if (_buffs[i].buffType == type) return true;
        }
        return false;
    }

    /// <summary>清除所有Buff</summary>
    public void ClearAll() => _buffs.Clear();
}
