using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话数据：一个完整的对话由多个 DialogueNode 组成
/// 策划在 Project 窗口右键 → 创建 → 对话 → DialogueData 来新建对话资产
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "对话/DialogueData")]
public class DialogueData : ScriptableObject
{
    [Header("对话标识")]
    [Tooltip("对话唯一ID，用于代码/事件切换对话")]
    public string dialogueId = "";

    [Tooltip("对话名称，策划备注用")]
    public string dialogueName = "";

    [Header("对话内容")]
    [Tooltip("对话节点列表，按顺序播放")]
    public List<DialogueNode> nodes = new List<DialogueNode>();
}
