using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包格子 UI 组件，代表背包面板中的单个道具槽位
/// 处理点击使用、悬停提示等交互逻辑
/// </summary>
public class BagSlot : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Image _iconImage; // 道具图标
    private TMP_Text _countText; // 数量文本
    private ItemData _itemData; // 该槽位对应的道具数据
    private BagUI _bagUI; // 所属的背包 UI 引用，用于回调

    /// <summary>
    /// 初始化格子：设置道具图标、数量显示，并记录数据引用
    /// </summary>
    public void Initialize(ItemData itemData, int count, BagUI bagUI)
    {
        _itemData = itemData;
        _bagUI = bagUI;

        _iconImage = transform.Find("图标").GetComponent<Image>();
        _countText = transform.Find("数量").GetComponent<TMP_Text>();

        if (_iconImage != null)
        {
            _iconImage.sprite = itemData.ItemIcon;
            _iconImage.enabled = itemData.ItemIcon != null;
        }

        if (_countText != null)
            _countText.text = $"{count}/{itemData.BagLimit}";
    }

    /// <summary>
    /// 左键点击：使用该道具
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            _bagUI.UseItem(_itemData);
    }

    /// <summary>
    /// 鼠标移入：显示道具提示信息
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        _bagUI.ShowTooltip(_itemData);
    }

    /// <summary>
    /// 鼠标移出：隐藏道具提示信息
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        _bagUI.HideTooltip();
    }
}
