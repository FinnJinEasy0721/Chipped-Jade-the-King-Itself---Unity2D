using TMPro;
using UnityEngine;

/// <summary>
/// 玩家信息 UI，实时显示玩家的 HP、移速和当前祝福类型
/// 每帧从 PlayerStat 读取数据并更新文本
/// </summary>
public class PlayerInfoUI : MonoBehaviour
{
    private PlayerStat _stat;          // 玩家属性引用

    private TMP_Text _hpText;          // 生命值文本
    private TMP_Text _speedText;       // 移速文本
    private TMP_Text _blessingText;    // 祝福类型文本

    private void Start()
    {
        // 获取玩家属性组件及各文本元素
        _stat = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerStat>();

        _hpText = transform.Find("生命值").GetComponent<TMP_Text>();
        _speedText = transform.Find("当前移速").GetComponent<TMP_Text>();
        _blessingText = transform.Find("当前祝福类型").GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (_stat == null) return;

        // 实时刷新 UI 文本
        _hpText.text = $"HP: {_stat.Curr_HP}/{_stat.GetMaxHP()}";
        _speedText.text = $"移速: {_stat.Base_PlayerSpeed:F1}";
        _blessingText.text = $"祝福: {BlessingDisplayName(_stat.currentBlessing)}";
    }

    /// <summary>
    /// 将祝福枚举值转换为中文显示名
    /// </summary>
    private static string BlessingDisplayName(PlayerStat.BlessingType blessing)
    {
        return blessing switch
        {
            PlayerStat.BlessingType.No_Blessing => "无",
            PlayerStat.BlessingType.AP_Blessing => "攻击祝福",
            PlayerStat.BlessingType.LifeSteal_Blessing => "生命偷取",
            PlayerStat.BlessingType.ThreeHit_Blessing => "三袭三生",
            PlayerStat.BlessingType.LowHurt_Blessing => "禾萎卸攻",
            PlayerStat.BlessingType.SuperHit_Blessing => "碎玉焚心",
            PlayerStat.BlessingType.Invincible_Blessing => "父佑青御",
            PlayerStat.BlessingType.Lightning_Blessing => "惊世先生",
            PlayerStat.BlessingType.Knockback_Blessing => "却敌安邦",
            _ => blessing.ToString()
        };
    }
}
