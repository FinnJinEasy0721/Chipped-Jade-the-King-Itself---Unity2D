using UnityEngine;

/// <summary>
/// 商店NPC组件：配置NPC名字和名言，复用NPCInteractionZone的触发器检测玩家范围
/// 挂在NPC上，与NPCInteractionZone共用同一个Collider2D
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ShopNPC : MonoBehaviour
{
    [Header("NPC信息")]
    [SerializeField] private string _npcName = "NPC";
    [SerializeField, TextArea] private string _npcSlogen = "";

    public string NPCName => _npcName;
    public string NPCSlogen => _npcSlogen;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        ShopManager.Instance.RegisterNPC(this);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        ShopManager.Instance.UnregisterNPC(this);
    }
}
