using TMPro;
using UnityEngine;

/// <summary>
/// 游戏内提示 UI，管理两种提示面板：
/// 1. 通用提示面板：显示道具拾取提示（名称、描述、操作指引）
/// 2. 通关提示面板：触发通关条件后显示
/// </summary>
public class TipsUI : MonoBehaviour
{
    private GameObject _generalPanel;    // 通用提示面板
    private TMP_Text _titleText;         // 提示标题
    private TMP_Text _contentText1;       // 提示内容1（道具描述）

    private GameObject _completePanel;    // 通关提示面板

    private void Start()
    {
        // 通过子物体名称查找通用提示面板及其子元素
        _generalPanel = transform.Find("通用提示面板").gameObject;
        _titleText = _generalPanel.transform.Find("提示标题").GetComponent<TMP_Text>();
        _contentText1 = _generalPanel.transform.Find("提示内容1").GetComponent<TMP_Text>();

        _completePanel = transform.Find("通关提示面板").gameObject;

        // 初始状态隐藏所有面板
        _generalPanel.SetActive(false);
        _completePanel.SetActive(false);

        // 监听提示相关事件
        EventCenter.Instance.AddListener<ItemData>(EventName.ShowTips, OnShowTips);
        EventCenter.Instance.AddListener(EventName.HideTips, OnHideTips);
        EventCenter.Instance.AddListener(EventName.AllowCompleteGame, OnAllowCompleteGame);
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveListener<ItemData>(EventName.ShowTips, OnShowTips);
        EventCenter.Instance.RemoveListener(EventName.HideTips, OnHideTips);
        EventCenter.Instance.RemoveListener(EventName.AllowCompleteGame, OnAllowCompleteGame);
    }

    /// <summary>
    /// 显示通用提示面板：道具名称、描述、操作按键（F拾取/E使用）
    /// </summary>
    private void OnShowTips(ItemData itemData)
    {
        _titleText.text = itemData.ItemName;
        _contentText1.text = itemData.ItemDescription + "\n按F放入背包，E直接使用";
        _generalPanel.SetActive(true);
    }

    /// <summary>
    /// 隐藏通用提示面板
    /// </summary>
    private void OnHideTips()
    {
        _generalPanel.SetActive(false);
    }

    /// <summary>
    /// 满足通关条件后显示通关提示面板
    /// </summary>
    private void OnAllowCompleteGame()
    {
        _completePanel.SetActive(true);
    }
}
