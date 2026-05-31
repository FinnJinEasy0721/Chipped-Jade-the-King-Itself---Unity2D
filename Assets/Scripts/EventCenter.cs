// 事件中心

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全局事件中心，基于观察者模式
/// 泛型版本支持携带参数，非泛型版本支持无参事件
/// </summary>
public class EventCenter
{
    private static EventCenter _instance;
    public static EventCenter Instance => _instance ??= new EventCenter();

    private readonly Dictionary<EventName, Delegate> _eventTable = new();

    // ==================== 注册 ====================

    public void AddListener(EventName name, Action callback)
    {
        _eventTable[name] = Delegate.Combine(_eventTable.GetValueOrDefault(name), callback);
    }

    public void AddListener<T>(EventName name, Action<T> callback)
    {
        _eventTable[name] = Delegate.Combine(_eventTable.GetValueOrDefault(name), callback);
    }

    // ==================== 取消注册 ====================

    public void RemoveListener(EventName name, Action callback)
    {
        if (_eventTable.TryGetValue(name, out var del))
        {
            _eventTable[name] = Delegate.Remove(del, callback);
        }
    }

    public void RemoveListener<T>(EventName name, Action<T> callback)
    {
        if (_eventTable.TryGetValue(name, out var del))
        {
            _eventTable[name] = Delegate.Remove(del, callback);
        }
    }

    // ==================== 触发 ====================

    public void Invoke(EventName name)
    {
        Debug.Log($"[EventCenter] 触发事件: {name}");
        if (_eventTable.TryGetValue(name, out var del) && del is Action action)
        {
            action();
        }
    }

    public void Invoke<T>(EventName name, T arg)
    {
        Debug.Log($"[EventCenter] 触发事件: {name}，参数: {arg}");
        if (_eventTable.TryGetValue(name, out var del) && del is Action<T> action)
        {
            action(arg);
        }
    }

    // ==================== 清理 ====================

    public void Clear()
    {
        _eventTable.Clear();
        Debug.Log("[EventCenter] 已清理所有事件");
    }

    public void Clear(EventName name)
    {
        if (_eventTable.ContainsKey(name))
        {
            _eventTable.Remove(name);
            Debug.Log($"[EventCenter] 已清理事件: {name}");
        }
    }
}

/// <summary>
/// 事件名称枚举
/// </summary>
public enum EventName
{
    EnemyDie,
    PlayerDie,
    PlayerHurt,
    EnemyHurt,
    OpenBag,
    CloseBag,
    PauseGame,
    ResumeGame,
    AllowCompleteGame, // 允许完成游戏（满足条件后触发，触发后会显示完成界面）
    SummonChippedJade,
    SaveGame,
    LoadGame,
    RestartGame,
    CloseGameExit,
    ReturnToMainMenu,
    ItemPickUp,
    ItemUse,
    BagChanged,
    ShowTips,
    HideTips,
    ShowNextLevel,
    ShowGameOver,
}

// 用法示例

// 注册
// EventCenter.Instance.AddListener(EventName.PlayerHurt, OnPlayerHurt);
// EventCenter.Instance.AddListener<int>(EventName.EnemyDie, OnEnemyDieWithExp);

// 触发（会自动 Debug.Log 事件名和参数）
// EventCenter.Instance.Invoke(EventName.PauseGame);
// EventCenter.Instance.Invoke(EventName.EnemyDie, 50);

// 取消注册
// EventCenter.Instance.RemoveListener(EventName.PlayerHurt, OnPlayerHurt);

// 清理
// EventCenter.Instance.Clear();          // 全部
// EventCenter.Instance.Clear(EventName.PlayerHurt);  // 单个