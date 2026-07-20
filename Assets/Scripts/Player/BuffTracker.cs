using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Buff 追踪器：挂在 Player 上，管理活跃的 Buff/Debuff 条目
/// 由 ItemEffect 子类通过 Register/Unregister 注册和注销
/// PlayerInfoUI 每帧从 GetActiveBuffs/GetActiveDebuffs 读取数据更新 UI
/// </summary>
public class BuffTracker : MonoBehaviour
{
    /// <summary>
    /// 单条 Buff/Debuff 记录
    /// </summary>
    public struct BuffEntry
    {
        public ItemEffect Source;   // 来源效果引用（用于去重和注销）
        public string Description;  // 简要描述
        public EffectCategory Category;
        public float RemainingTime; // 剩余时间（秒）
        public float TotalTime;     // 总持续时间（用于计算进度）
        public bool IsInstant;      // 是否为即时效果（UI 显示"已使用"而非读秒）
    }

    private readonly List<BuffEntry> _activeBuffs = new();

    /// <summary>当前活跃的 Buff/Debuff 列表（只读，供 UI 读取）</summary>
    public IReadOnlyList<BuffEntry> ActiveBuffs => _activeBuffs;

    /// <summary>
    /// 注册一个效果到追踪器
    /// 若同一效果已存在则刷新持续时间
    /// </summary>
    public void Register(ItemEffect effect, float duration, bool isInstant = false)
    {
        if (effect == null || effect.Category == EffectCategory.None) return;

        // 已存在则刷新
        for (int i = 0; i < _activeBuffs.Count; i++)
        {
            if (_activeBuffs[i].Source == effect)
            {
                _activeBuffs[i] = new BuffEntry
                {
                    Source = effect,
                    Description = effect.BriefDescription,
                    Category = effect.Category,
                    RemainingTime = duration,
                    TotalTime = duration,
                    IsInstant = isInstant
                };
                return;
            }
        }

        // 新增条目
        _activeBuffs.Add(new BuffEntry
        {
            Source = effect,
            Description = effect.BriefDescription,
            Category = effect.Category,
            RemainingTime = duration,
            TotalTime = duration,
            IsInstant = isInstant
        });
    }

    /// <summary>
    /// 从追踪器注销一个效果
    /// </summary>
    public void Unregister(ItemEffect effect)
    {
        if (effect == null) return;
        _activeBuffs.RemoveAll(e => e.Source == effect);
    }

    private void Update()
    {
        // 倒计时并移除过期条目
        for (int i = _activeBuffs.Count - 1; i >= 0; i--)
        {
            var entry = _activeBuffs[i];
            entry.RemainingTime -= Time.deltaTime;
            if (entry.RemainingTime <= 0f)
            {
                _activeBuffs.RemoveAt(i);
            }
            else
            {
                _activeBuffs[i] = entry;
            }
        }
    }
}
