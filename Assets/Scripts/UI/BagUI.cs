using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包 UI 面板，负责背包界面的显示/隐藏、格子生成与刷新、道具提示和道具使用
/// 道具按等级分为三行显示（1/2/3级）
/// </summary>
public class BagUI : MonoBehaviour
{
    [Header("面板")]
    [SerializeField] private GameObject _panel;

    [Header("等级区域格子容器")]
    [SerializeField] private Transform _level1Container; // 1级道具容器
    [SerializeField] private Transform _level2Container; // 2级道具容器
    [SerializeField] private Transform _level3Container; // 3级道具容器

    [Header("格子模板")]
    [SerializeField] private GameObject _slotTemplate; // 格子预制体模板，用于实例化

    [Header("悬停提示")]
    [SerializeField] private GameObject _tooltipPanel; // 提示面板根节点
    [SerializeField] private GameObject _tooltipTitleGo; // 提示标题 GameObject
    [SerializeField] private GameObject _tooltipDescGo; // 提示描述 GameObject
    [SerializeField] private TMP_Text _tooltipTitle; // 提示标题文本
    [SerializeField] private TMP_Text _tooltipDesc; // 提示描述文本

    // 当前已创建的格子实例列表，刷新时先全部销毁
    private readonly List<GameObject> _slotInstances = new();

    private void Start()
    {
        // 初始状态：面板、提示、模板全部隐藏
        _panel.SetActive(false);
        _tooltipTitleGo.SetActive(false);
        _tooltipDescGo.SetActive(false);
        _slotTemplate.SetActive(false);

        // 监听背包相关事件
        EventCenter.Instance.AddListener(EventName.OpenBag, OnOpenBag);
        EventCenter.Instance.AddListener(EventName.CloseBag, OnCloseBag);
        EventCenter.Instance.AddListener(EventName.BagChanged, Refresh);
    }

    private void OnDestroy()
    {
        EventCenter.Instance.RemoveListener(EventName.OpenBag, OnOpenBag);
        EventCenter.Instance.RemoveListener(EventName.CloseBag, OnCloseBag);
        EventCenter.Instance.RemoveListener(EventName.BagChanged, Refresh);
    }

    private void Update()
    {
        // Tab 键切换背包面板的开关
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (_panel.activeSelf)
                EventCenter.Instance.Invoke(EventName.CloseBag);
            else
                EventCenter.Instance.Invoke(EventName.OpenBag);
        }
    }

    /// <summary>
    /// 打开背包面板并刷新内容
    /// </summary>
    private void OnOpenBag()
    {
        _panel.SetActive(true);
        Refresh();
    }

    /// <summary>
    /// 关闭背包面板并隐藏提示
    /// </summary>
    private void OnCloseBag()
    {
        _panel.SetActive(false);
        HideTooltip();
    }

    /// <summary>
    /// 刷新背包面板：先清除所有格子，再按等级重新生成
    /// </summary>
    private void Refresh()
    {
        HideTooltip();
        ClearSlots();

        if (BagSystem.Instance == null) return;

        // 分别为三个等级创建道具格子
        CreateSlotsForLevel(1, _level1Container);
        CreateSlotsForLevel(2, _level2Container);
        CreateSlotsForLevel(3, _level3Container);
    }

    /// <summary>
    /// 根据等级从 BagData 中获取道具，为每个道具实例化一个格子
    /// </summary>
    private void CreateSlotsForLevel(int level, Transform container)
    {
        var items = BagSystem.Instance.BagData.GetItemsByLevel(level);
        foreach (var kvp in items)
        {
            var slotObj = Instantiate(_slotTemplate, container);
            slotObj.SetActive(true);
            var slot = slotObj.GetComponent<BagSlot>();
            slot.Initialize(kvp.Key, kvp.Value, this);
            _slotInstances.Add(slotObj);
        }
    }

    /// <summary>
    /// 销毁所有已创建的格子实例
    /// </summary>
    private void ClearSlots()
    {
        foreach (var slot in _slotInstances)
            Destroy(slot);
        _slotInstances.Clear();
    }

    /// <summary>
    /// 显示道具悬停提示（名称 + 描述）
    /// </summary>
    public void ShowTooltip(ItemData itemData)
    {
        _tooltipTitle.text = itemData.ItemName;
        _tooltipDesc.text = itemData.ItemDescription;
        _tooltipTitleGo.SetActive(true);
        _tooltipDescGo.SetActive(true);
    }

    /// <summary>
    /// 隐藏道具悬停提示
    /// </summary>
    public void HideTooltip()
    {
        _tooltipTitleGo.SetActive(false);
        _tooltipDescGo.SetActive(false);
    }

    /// <summary>
    /// 使用道具：查找玩家对象并调用 BagSystem.UseItem
    /// </summary>
    public void UseItem(ItemData itemData)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        BagSystem.Instance.UseItem(itemData, player);
    }
}
