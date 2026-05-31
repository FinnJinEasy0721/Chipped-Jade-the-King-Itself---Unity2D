using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 背包数据模型，负责道具的增删查逻辑
/// 底层使用字典存储：Key=道具数据，Value=持有数量
/// </summary>
public class BagData
{
    private readonly Dictionary<ItemData, int> _items = new();

    /// <summary>
    /// 添加道具到背包，若超过该道具的持有上限则失败
    /// </summary>
    public bool AddItem(ItemData itemData, int count = 1)
    {
        int current = GetCount(itemData);

        // 检查添加后是否超过背包上限
        if (current + count > itemData.BagLimit)
            return false;

        _items[itemData] = current + count;
        return true;
    }

    /// <summary>
    /// 从背包移除指定数量的道具，数量不足则失败；移至0时自动清除条目
    /// </summary>
    public bool RemoveItem(ItemData itemData, int count = 1)
    {
        int current = GetCount(itemData);

        if (current < count)
            return false;

        int remaining = current - count;
        // 数量归零时移除整个条目，避免字典残留
        if (remaining <= 0)
            _items.Remove(itemData);
        else
            _items[itemData] = remaining;

        return true;
    }

    /// <summary>
    /// 获取某道具的当前持有数量，不存在则返回0
    /// </summary>
    public int GetCount(ItemData itemData)
    {
        return _items.GetValueOrDefault(itemData, 0);
    }

    /// <summary>
    /// 获取背包中所有道具及其数量
    /// </summary>
    public Dictionary<ItemData, int> GetAllItems() => _items;

    /// <summary>
    /// 按道具等级筛选，返回符合条件的道具列表
    /// </summary>
    public List<KeyValuePair<ItemData, int>> GetItemsByLevel(int level)
    {
        return _items.Where(kvp => kvp.Key.ItemLevel == level).ToList();
    }

    /// <summary>
    /// 清空背包
    /// </summary>
    public void Clear() => _items.Clear();
}
