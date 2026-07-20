using UnityEngine;

/// <summary>
/// NPC交互区域：检测玩家进入/离开，控制ButtonTips的显示和隐藏
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NPCInteractionZone : MonoBehaviour
{
    [Header("提示对象")]
    [Tooltip("玩家靠近时显示的提示Sprite")]
    [SerializeField] private GameObject _buttonTips;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_buttonTips != null) _buttonTips.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (_buttonTips != null) _buttonTips.SetActive(false);
    }
}
