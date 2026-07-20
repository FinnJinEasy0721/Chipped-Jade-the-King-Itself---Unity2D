using System.Collections.Generic;

/// <summary>
/// 属性修改器 —— 单条修改记录
/// </summary>
public struct StatModifier
{
    /// <summary>修改的属性</summary>
    public StatType Stat;
    /// <summary>操作类型</summary>
    public ModifierOp Op;
    /// <summary>数值</summary>
    public float Value;
    /// <summary>来源对象（用于按来源批量移除）</summary>
    public object Source;
}

/// <summary>
/// 属性修改器系统 —— 管理所有属性修饰器，计算最终属性值
/// 计算顺序：基础值 → AddFlat → AddPercentage → Multiply → Override
/// 同类修饰器按添加顺序依次应用
/// </summary>
public class StatModifierSystem
{
    private readonly List<StatModifier> _modifiers = new();

    /// <summary>添加修改器</summary>
    /// <param name="stat">修改的属性</param>
    /// <param name="op">操作类型</param>
    /// <param name="value">数值</param>
    /// <param name="source">来源对象（卸下祝福时按来源批量移除）</param>
    public void Add(StatType stat, ModifierOp op, float value, object source)
    {
        _modifiers.Add(new StatModifier
        {
            Stat = stat,
            Op = op,
            Value = value,
            Source = source
        });
    }

    /// <summary>移除指定来源的所有修改器</summary>
    public void RemoveAllFromSource(object source)
    {
        _modifiers.RemoveAll(m => m.Source == source);
    }

    /// <summary>
    /// 计算最终属性值
    /// 顺序：基础值 → AddFlat(累加) → AddPercentage(累加，基于基础值) → Multiply(叠乘) → Override(最后生效)
    /// </summary>
    /// <param name="stat">目标属性</param>
    /// <param name="baseValue">基础值</param>
    public float GetFinalValue(StatType stat, float baseValue)
    {
        float result = baseValue;

        // 1. 加法修饰（固定值累加）
        for (int i = 0; i < _modifiers.Count; i++)
        {
            var mod = _modifiers[i];
            if (mod.Stat == stat && mod.Op == ModifierOp.AddFlat)
                result += mod.Value;
        }

        // 2. 百分比修饰（基于基础值的百分比，累加）
        for (int i = 0; i < _modifiers.Count; i++)
        {
            var mod = _modifiers[i];
            if (mod.Stat == stat && mod.Op == ModifierOp.AddPercentage)
                result += baseValue * mod.Value;
        }

        // 3. 乘法修饰（叠乘）
        for (int i = 0; i < _modifiers.Count; i++)
        {
            var mod = _modifiers[i];
            if (mod.Stat == stat && mod.Op == ModifierOp.Multiply)
                result *= mod.Value;
        }

        // 4. 覆盖修饰（最后一个生效）
        for (int i = 0; i < _modifiers.Count; i++)
        {
            var mod = _modifiers[i];
            if (mod.Stat == stat && mod.Op == ModifierOp.Override)
                result = mod.Value;
        }

        return result;
    }

    /// <summary>是否有覆盖修饰</summary>
    public bool HasOverride(StatType stat)
    {
        for (int i = 0; i < _modifiers.Count; i++)
        {
            if (_modifiers[i].Stat == stat && _modifiers[i].Op == ModifierOp.Override)
                return true;
        }
        return false;
    }

    /// <summary>清空所有修改器</summary>
    public void Clear() => _modifiers.Clear();
}
