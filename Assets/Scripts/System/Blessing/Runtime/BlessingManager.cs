using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 祝福核心管理器 —— 挂载于Player
/// 职责：装备/卸下祝福、分发效果到Handler、管理Tick效果、存档、Debug可视化
/// </summary>
[RequireComponent(typeof(PlayerStat))]
public class BlessingManager : MonoBehaviour
{
    [Header("数据库引用")]
    [Tooltip("拖入 BlessingDatabase SO 资产")]
    [SerializeField] private BlessingDatabase _database;

    [Header("调试")]
    [Tooltip("在Scene视图中显示范围Gizmo")]
    [SerializeField] private bool _showDebugGizmos = false;

    /// <summary>当前装备的祝福（null=无祝福）</summary>
    public BlessingData CurrentBlessing { get; private set; }

    // 运行时上下文
    private BlessingRuntimeContext _context;
    /// <summary>属性修改器系统</summary>
    public StatModifierSystem Modifiers { get; private set; }
    /// <summary>技能控制器</summary>
    public SkillController SkillController { get; private set; }

    // 当前祝福的效果处理器列表
    private readonly List<EffectHandlerBase> _activeHandlers = new();
    // 当前注册的事件监听器（用于卸下时反注册）
    private readonly List<(EventName name, Action<CombatContext> listener)> _registeredListeners = new();
    // Tick型效果的计时器列表
    private readonly List<(EffectHandlerBase handler, BlessingEffectConfig config, float timer)> _tickers = new();

    private void Awake()
    {
        Modifiers = new StatModifierSystem();
        _context = new BlessingRuntimeContext
        {
            Player = gameObject,
            PlayerStat = GetComponent<PlayerStat>(),
            Modifiers = Modifiers,
            Database = _database,
            PlayerTransform = transform
        };
        SkillController = new SkillController(_context);
    }

    private void Start()
    {
        // 从存档加载祝福（PlayerStat.Awake已加载playerData）
        LoadBlessingFromSave();
        // 未加载到有效祝福时装备空祝福作为初始状态
        if (CurrentBlessing == null)
            EquipBlessing(BlessingData.None);
    }

    private void Update()
    {
        SkillController?.CustomUpdate();
        UpdateTickEffects();
    }

    // ==================== 装备/卸下 ====================

    /// <summary>装备祝福（替换当前祝福）</summary>
    public void EquipBlessing(BlessingData blessing)
    {
        if (blessing == null) return;

        // 1. 卸下当前祝福
        UnequipCurrent();

        // 2. 装备新祝福
        CurrentBlessing = blessing;
        SetupPassiveEffects(blessing);
        SkillController?.SetupSkill(blessing.skillConfig);

        // 3. 存档
        SaveBlessing();

        Debug.Log($"[祝福系统] 装备祝福：{blessing.blessingName}（ID: {blessing.blessingID}）");
    }

    /// <summary>卸下当前祝福</summary>
    public void UnequipCurrent()
    {
        if (CurrentBlessing == null) return;

        // 移除所有属性修改器
        Modifiers.RemoveAllFromSource(CurrentBlessing);
        // 卸载所有效果处理器
        foreach (var handler in _activeHandlers)
            handler.OnUnequip(_context);
        _activeHandlers.Clear();
        // 反注册所有事件监听
        UnregisterAllListeners();
        // 清除Tick效果
        _tickers.Clear();
        // 停止技能
        SkillController?.DeactivateSkill();
        SkillController?.ClearSkill();

        Debug.Log($"[祝福系统] 卸下祝福：{CurrentBlessing.blessingName}");
        CurrentBlessing = null;
    }

    // ==================== 被动效果设置 ====================

    private void SetupPassiveEffects(BlessingData blessing)
    {
        foreach (var config in blessing.passiveEffects)
        {
            var handler = EffectHandlerFactory.Create(config.effectType);
            if (handler == null)
            {
                Debug.LogWarning($"[祝福系统] 未注册的效果类型：{config.effectType}");
                continue;
            }

            handler.OnEquip(config, _context);
            _activeHandlers.Add(handler);

            // 注册事件触发或Tick
            if (config.triggerType == EffectTriggerType.Tick)
            {
                _tickers.Add((handler, config, config.tickInterval));
            }
            else
            {
                RegisterTrigger(config, handler);
            }
        }
    }

    // ==================== 事件注册/反注册 ====================

    private void RegisterTrigger(BlessingEffectConfig config, EffectHandlerBase handler)
    {
        Action<CombatContext> listener = ctx => handler.OnTrigger(config, _context, ctx);

        EventName eventName = config.triggerType switch
        {
            EffectTriggerType.OnAttackHit  => EventName.EnemyHurt,
            EffectTriggerType.OnPlayerHurt => EventName.PlayerHurt,
            EffectTriggerType.OnEnemyKill  => EventName.EnemyDie,
            _ => EventName.EnemyHurt // Permanent/Tick不注册事件
        };

        if (config.triggerType is EffectTriggerType.OnAttackHit
            or EffectTriggerType.OnPlayerHurt
            or EffectTriggerType.OnEnemyKill)
        {
            EventCenter.Instance.AddListener(eventName, listener);
            _registeredListeners.Add((eventName, listener));
        }
    }

    private void UnregisterAllListeners()
    {
        foreach (var (name, listener) in _registeredListeners)
            EventCenter.Instance.RemoveListener(name, listener);
        _registeredListeners.Clear();
    }

    // ==================== Tick效果 ====================

    private void UpdateTickEffects()
    {
        for (int i = 0; i < _tickers.Count; i++)
        {
            var (handler, config, timer) = _tickers[i];
            float newTimer = timer - Time.deltaTime;

            if (newTimer <= 0f)
            {
                handler.OnTrigger(config, _context, default);
                newTimer = config.tickInterval;
            }
            _tickers[i] = (handler, config, newTimer);
        }
    }

    // ==================== 存档 ====================

    private void SaveBlessing()
    {
        var stat = _context.PlayerStat;
        stat.playerData.Current_Blessing = CurrentBlessing?.blessingID ?? "None";
        stat.SaveData();
    }

    private void LoadBlessingFromSave()
    {
        if (_database == null)
        {
            Debug.LogWarning("[祝福系统] 未配置 BlessingDatabase，跳过加载");
            return;
        }

        var stat = _context.PlayerStat;
        string id = stat.playerData.Current_Blessing;

        if (string.IsNullOrEmpty(id) || id == "None" || id == "No_Blessing") return;

        var blessing = _database.FindByID(id);
        if (blessing != null)
            EquipBlessing(blessing);
        else
            Debug.LogWarning($"[祝福系统] 存档中的祝福ID '{id}' 在数据库中未找到");
    }

    // ==================== Debug可视化 ====================

    private void OnDrawGizmosSelected()
    {
        if (!_showDebugGizmos || CurrentBlessing == null) return;

        // 绘制Aura效果范围
        foreach (var config in CurrentBlessing.passiveEffects)
        {
            if (config.effectType != EffectType.AuraDamage) continue;
            DrawAreaGizmo(config.areaShape, config.areaRadius, config.areaWidth, config.areaHeight);
        }

        // 绘制技能范围
        var skill = CurrentBlessing.skillConfig;
        if (skill.skillType != SkillType.None)
            DrawAreaGizmo(skill.areaShape, skill.areaRadius, skill.areaWidth, skill.areaHeight);
    }

    private void DrawAreaGizmo(AreaShape shape, float radius, float width, float height)
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Vector3 center = transform.position;

        if (shape == AreaShape.Circle)
            Gizmos.DrawWireSphere(center, radius);
        else
            Gizmos.DrawWireCube(center, new Vector3(width, height, 0f));
    }
}
