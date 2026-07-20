using System.IO;
using UnityEngine;

/// <summary>
/// 背包系统单例，管理道具的拾取、使用、移除以及存档读写
/// 对外提供增删查用的接口，内部将数据操作委托给 BagData
/// </summary>
public class BagSystem : MonoBehaviour
{
    public static BagSystem Instance { get; private set; }

    public BagData BagData { get; private set; }

    [Tooltip("所有可能的道具数据，用于存档反序列化")]
    public ItemData[] AllItemData;

    // 存档文件路径，位于 persistentDataPath 下
    private static string SavePath => Path.Combine(Application.persistentDataPath, "bag_save.json");

    private void Awake()
    {
        // 单例初始化：确保场景中只有一个 BagSystem 实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BagData = new BagData();
    }

    private void OnEnable()
    {
        // 监听全局的存档/读档事件
        EventCenter.Instance.AddListener(EventName.SaveGame, Save);
        EventCenter.Instance.AddListener(EventName.LoadGame, Load);
    }

    private void OnDisable()
    {
        EventCenter.Instance.RemoveListener(EventName.SaveGame, Save);
        EventCenter.Instance.RemoveListener(EventName.LoadGame, Load);
    }

    private void OnApplicationQuit()
    {
        // 退出应用时自动保存背包
        Save();
    }

    /// <summary>
    /// 将道具添加到背包，成功后广播拾取事件和背包变更事件
    /// </summary>
    public bool AddItem(ItemData itemData, int count = 1)
    {
        if (!BagData.AddItem(itemData, count))
        {
            Debug.Log($"[BagSystem] 拾取失败，{itemData.ItemName} 已达上限");
            return false;
        }

        EventCenter.Instance.Invoke<ItemData>(EventName.ItemPickUp, itemData);
        EventCenter.Instance.Invoke(EventName.BagChanged);
        Debug.Log($"[BagSystem] 拾取 {itemData.ItemName} x{count}");
        return true;
    }

    /// <summary>
    /// 从背包移除道具，成功后广播背包变更事件
    /// </summary>
    public bool RemoveItem(ItemData itemData, int count = 1)
    {
        if (!BagData.RemoveItem(itemData, count))
        {
            Debug.Log($"[BagSystem] 移除失败，{itemData.ItemName} 数量不足");
            return false;
        }

        EventCenter.Instance.Invoke(EventName.BagChanged);
        Debug.Log($"[BagSystem] 移除 {itemData.ItemName} x{count}");
        return true;
    }

    /// <summary>
    /// 使用一个道具：消耗1个，施加所有效果，广播使用和变更事件
    /// </summary>
    public bool UseItem(ItemData itemData, GameObject player)
    {
        if (BagData.GetCount(itemData) <= 0)
        {
            Debug.Log($"[BagSystem] 使用失败，{itemData.ItemName} 数量为0");
            return false;
        }

        BagData.RemoveItem(itemData, 1);
        ApplyEffects(itemData, player);
        EventCenter.Instance.Invoke<ItemData>(EventName.ItemUse, itemData);
        EventCenter.Instance.Invoke(EventName.BagChanged);
        Debug.Log($"[BagSystem] 使用 {itemData.ItemName}");
        return true;
    }

    /// <summary>
    /// 依次对目标施加道具的所有效果
    /// </summary>
    public void ApplyEffects(ItemData itemData, GameObject target)
    {
        if (itemData.Effects == null) return;
        foreach (var effect in itemData.Effects)
        {
            if (effect != null)
                effect.Apply(target);
        }
    }

    public int GetItemCount(ItemData itemData)
    {
        return BagData.GetCount(itemData);
    }

    /// <summary>
    /// 按道具名称在 AllItemData 中查找，用于存档反序列化时还原引用
    /// </summary>
    public ItemData FindItemByName(string itemName)
    {
        if (AllItemData == null) return null;
        foreach (var item in AllItemData)
        {
            if (item != null && item.ItemName == itemName)
                return item;
        }
        return null;
    }

    /// <summary>
    /// 将背包数据序列化为 JSON 写入磁盘
    /// </summary>
    public void Save()
    {
        var allItems = BagData.GetAllItems();
        var saveData = new BagSaveData { entries = new BagEntry[allItems.Count] };
        int i = 0;
        foreach (var kvp in allItems)
        {
            saveData.entries[i++] = new BagEntry
            {
                itemName = kvp.Key.ItemName,
                count = kvp.Value
            };
        }

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[BagSystem] 背包已保存到 {SavePath}");
    }

    /// <summary>
    /// 从磁盘读取 JSON 存档并还原背包内容
    /// 通过道具名称在 AllItemData 中查找对应的 ScriptableObject 引用
    /// </summary>
    public void Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[BagSystem] 未找到存档文件");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var saveData = JsonUtility.FromJson<BagSaveData>(json);
        BagData.Clear();

        if (saveData?.entries != null)
        {
            foreach (var entry in saveData.entries)
            {
                var itemData = FindItemByName(entry.itemName);
                if (itemData != null)
                    BagData.AddItem(itemData, entry.count);
            }
        }

        EventCenter.Instance.Invoke(EventName.BagChanged);
        Debug.Log("[BagSystem] 背包已加载");
    }
}

/// <summary>
/// 背包存档数据结构，可被 JsonUtility 序列化
/// </summary>
[System.Serializable]
public class BagSaveData
{
    public BagEntry[] entries;
}

/// <summary>
/// 单条背包存档条目：道具名 + 数量
/// </summary>
[System.Serializable]
public class BagEntry
{
    public string itemName;
    public int count;
}
