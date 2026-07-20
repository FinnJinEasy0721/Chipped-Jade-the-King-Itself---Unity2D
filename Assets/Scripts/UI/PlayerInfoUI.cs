using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 玩家信息 UI，实时显示玩家的基础属性、祝福和冷却信息
/// 每帧从 PlayerStat / BlessingManager 读取数据并更新文本与进度条
/// 同时从 BuffTracker 读取活跃 Buff/Debuff 更新对应文本槽位
/// </summary>
public class PlayerInfoUI : MonoBehaviour
{
    private PlayerStat _stat;                // 玩家属性引用
    private BlessingManager _blessingManager; // 祝福管理器引用
    private BuffTracker _buffTracker;        // Buff追踪器引用

    // 基础信息文本
    private TMP_Text _hpText;          // 当前生命值
    private TMP_Text _speedText;        // 当前移速
    private TMP_Text _levelText;       // 玩家等级
    private TMP_Text _maxHpText;       // 最大生命值
    private TMP_Text _coinText;        // 玩家金币
    private TMP_Text _expText;          // 玩家经验值

    // 祝福信息
    private TMP_Text _blessingNameText; // 祝福名字
    private Image _blessingIcon;        // 祝福图标

    // 技能冷却
    private Slider _skillSlider;        // 技能冷却进度条
    private TMP_Text _skillCooldownText; // 技能冷却数字

    // 蓄力重击冷却
    private Slider _heavyAttackSlider;        // 蓄力重击冷却进度条
    private TMP_Text _heavyAttackCooldownText; // 蓄力重击冷却数字

    // Buff/Debuff 文本槽位（各3个）
    private TMP_Text[] _buffTexts = new TMP_Text[3];
    private TMP_Text[] _debuffTexts = new TMP_Text[3];

    private void Start()
    {
        // 获取玩家属性组件
        var player = GameObject.FindGameObjectWithTag("Player");
        _stat = player.GetComponent<PlayerStat>();
        _blessingManager = player.GetComponent<BlessingManager>();
        _buffTracker = player.GetComponent<BuffTracker>();

        // 基础信息面板
        var basePanel = transform.Find("玩家基础信息面板");
        _hpText = basePanel.Find("当前生命值").GetComponent<TMP_Text>();
        _speedText = basePanel.Find("当前移速").GetComponent<TMP_Text>();
        _levelText = basePanel.Find("玩家等级").GetComponent<TMP_Text>();
        _maxHpText = basePanel.Find("最大生命值").GetComponent<TMP_Text>();
        _coinText = basePanel.Find("玩家金币").GetComponent<TMP_Text>();
        _expText = basePanel.Find("玩家经验值").GetComponent<TMP_Text>();

        // 玩家数据信息面板
        var dataPanel = transform.Find("玩家数据信息");
        _blessingIcon = dataPanel.Find("祝福icon").GetComponent<Image>();
        _blessingNameText = dataPanel.Find("祝福icon/祝福名字").GetComponent<TMP_Text>();

        // 技能冷却
        var skillIcon = dataPanel.Find("技能icon");
        _skillSlider = skillIcon.GetComponent<Slider>();
        _skillCooldownText = skillIcon.Find("冷却数字").GetComponent<TMP_Text>();

        // 蓄力重击冷却
        var heavyIcon = dataPanel.Find("蓄力重击icon");
        _heavyAttackSlider = heavyIcon.GetComponent<Slider>();
        _heavyAttackCooldownText = heavyIcon.Find("冷却数字").GetComponent<TMP_Text>();

        // Buff/Debuff 文本槽位
        var buffPanel = dataPanel.Find("Buff");
        var debuffPanel = dataPanel.Find("Debuff");
        for (int i = 0; i < 3; i++)
        {
            _buffTexts[i] = buffPanel.Find((i + 1).ToString()).GetComponent<TMP_Text>();
            _debuffTexts[i] = debuffPanel.Find((i + 1).ToString()).GetComponent<TMP_Text>();
        }
    }

    private void Update()
    {
        if (_stat == null) return;

        // ---- 基础信息 ----
        int maxHp = _stat.GetMaxHP();
        if (_stat.Curr_ExtraHealth > 0)
            _hpText.text = $"生命值：{_stat.Curr_HP}<color=#00FF00>（+{_stat.Curr_ExtraHealth}）</color>";
        else
            _hpText.text = $"生命值：{_stat.Curr_HP}";

        float speed = _blessingManager?.Modifiers?.GetFinalValue(StatType.MoveSpeed, _stat.Base_PlayerSpeed)
                      ?? _stat.Base_PlayerSpeed;
        _speedText.text = $"移速: {speed:F1}";

        _levelText.text = $"等级: {_stat.playerData.Player_Level}";
        _maxHpText.text = $"最大生命: {maxHp}";
        _coinText.text = $"口袋金币：{_stat.playerData.Player_Coin}";

        int nextExp = _stat.GetNextLevelExpRequirement();
        _expText.text = nextExp > 0 ? $"升级还需要：{nextExp}XP" : "已满级";

        // ---- 祝福 ----
        var blessing = _blessingManager?.CurrentBlessing;
        _blessingNameText.text = blessing?.blessingName ?? "无";
        if (blessing != null && blessing.icon != null)
            _blessingIcon.sprite = blessing.icon;

        // ---- 技能冷却 ----
        var skill = _blessingManager?.SkillController;
        if (skill != null && skill.HasSkill)
        {
            float total = skill.CooldownTotal;
            float remaining = skill.CooldownRemaining;
            if (total > 0f && remaining > 0f)
            {
                _skillSlider.value = 1f - remaining / total;
                _skillCooldownText.text = $"{remaining:F1}s";
            }
            else
            {
                _skillSlider.value = 1f;
                _skillCooldownText.text = "就绪";
            }
        }
        else
        {
            _skillSlider.value = 0f;
            _skillCooldownText.text = "无技能";
        }

        // ---- 蓄力重击冷却 ----
        if (_stat.CanHeavy())
        {
            float total = _stat.HeavyAttackCD;
            float remaining = _stat.GetHeavyAttackCooldownRemaining();
            if (remaining > 0f)
            {
                _heavyAttackSlider.value = 1f - remaining / total;
                _heavyAttackCooldownText.text = $"{remaining:F1}s";
            }
            else
            {
                _heavyAttackSlider.value = 1f;
                _heavyAttackCooldownText.text = "就绪";
            }
        }
        else
        {
            _heavyAttackSlider.value = 0f;
            _heavyAttackCooldownText.text = "未解锁";
        }

        // ---- Buff/Debuff ----
        UpdateBuffDebuffUI();
    }

    /// <summary>
    /// 从 BuffTracker 读取活跃条目，更新 Buff/Debuff 文本槽位
    /// 每个槽位显示"描述 剩余时间s"，空槽位显示空字符串
    /// </summary>
    private void UpdateBuffDebuffUI()
    {
        // 清空所有槽位
        for (int i = 0; i < 3; i++)
        {
            _buffTexts[i].text = "";
            _debuffTexts[i].text = "";
        }

        if (_buffTracker == null) return;

        int buffIndex = 0;
        int debuffIndex = 0;

        foreach (var entry in _buffTracker.ActiveBuffs)
        {
            string text = entry.IsInstant
                ? $"{entry.Description} 已使用"
                : $"{entry.Description} {entry.RemainingTime:F1}s";

            if (entry.Category == EffectCategory.Buff && buffIndex < 3)
            {
                _buffTexts[buffIndex].text = text;
                buffIndex++;
            }
            else if (entry.Category == EffectCategory.Debuff && debuffIndex < 3)
            {
                _debuffTexts[debuffIndex].text = text;
                debuffIndex++;
            }
        }
    }
}
