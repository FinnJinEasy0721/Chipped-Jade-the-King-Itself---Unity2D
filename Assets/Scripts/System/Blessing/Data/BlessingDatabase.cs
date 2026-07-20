using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 祝福集中数据库 —— 策划在此SO中配置所有祝福
/// 创建方式：Project窗口右键 Create > Blessing > BlessingDatabase
/// 使用方式：拖入 BlessingManager 的 _database 字段
/// </summary>
[CreateAssetMenu(fileName = "BlessingDatabase", menuName = "Blessing/BlessingDatabase")]
public class BlessingDatabase : ScriptableObject
{
    [Header("所有祝福列表")]
    [Tooltip("在此添加所有祝福配置，每条对应一个可购买的祝福")]
    public List<BlessingData> blessings = new();

    /// <summary>通过唯一ID查找祝福</summary>
    public BlessingData FindByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return blessings.Find(b => b.blessingID == id);
    }
}
