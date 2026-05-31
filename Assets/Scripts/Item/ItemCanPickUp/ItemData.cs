using UnityEngine;

/// <summary>
/// 道具数据 ScriptableObject，定义单个道具的所有配置信息
/// 在 Inspector 中通过右键 Create > Item > ItemData 创建实例
/// </summary>
[CreateAssetMenu(fileName = "ItemData_", menuName = "Item/ItemData")]
public class ItemData : ScriptableObject
{
    public string ItemName; // 道具名称
    public Sprite ItemIcon; // 道具图标
    [TextArea] public string ItemDescription; // 道具描述（多行文本）
    [Range(1, 3)] public int ItemLevel = 1; // 道具等级（1~3）
    public int BagLimit = 99; // 背包中该道具的持有上限
    public ItemEffect[] Effects; // 道具效果列表
}
