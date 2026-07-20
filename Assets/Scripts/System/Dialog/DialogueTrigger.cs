using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话触发器：挂在NPC上，玩家靠近后按E键开始对话
/// 支持多对话配置，通过ID切换当前对话
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DialogueTrigger : MonoBehaviour
{
    [Header("提示对象")]
    [Tooltip("玩家靠近时显示的提示（如\"按E对话\"）")]
    [SerializeField] private GameObject buttonTips;

    [Header("对话配置")]
    [Tooltip("当前默认播放的对话")]
    [SerializeField] private DialogueData currentDialogue;

    [Tooltip("该NPC所有可用的对话（通过ID切换）")]
    [SerializeField] private List<DialogueData> allDialogues = new List<DialogueData>();

    private bool _playerInRange;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = true;
        if (buttonTips != null) buttonTips.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        _playerInRange = false;
        if (buttonTips != null) buttonTips.SetActive(false);
    }

    private void Update()
    {
        if (!_playerInRange) return;
        if (!Input.GetKeyDown(KeyCode.E)) return;
        if (DialogueManager.Instance == null || DialogueManager.Instance.IsActive) return;

        if (currentDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(currentDialogue);
        }
        else
        {
            Debug.LogWarning($"[DialogueTrigger] {gameObject.name} 的 currentDialogue 为空");
        }
    }

    /// <summary>
    /// 通过对话ID切换当前对话
    /// </summary>
    public void SetDialogueById(string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        foreach (var d in allDialogues)
        {
            if (d != null && d.dialogueId == id)
            {
                currentDialogue = d;
                return;
            }
        }
        Debug.LogWarning($"[DialogueTrigger] {gameObject.name} 未找到对话ID: {id}");
    }

    /// <summary>
    /// 直接设置当前对话
    /// </summary>
    public void SetDialogue(DialogueData data)
    {
        currentDialogue = data;
    }
}
