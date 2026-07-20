using System;
using UnityEngine;

/// <summary>
/// 单条对话节点：包含说话人名字、头像、对话文本
/// </summary>
[Serializable]
public class DialogueNode
{
    [Header("说话人信息")]
    public string speakerName = "";
    public Sprite speakerPortrait;

    [Header("对话文本")]
    [TextArea(3, 6)]
    public string text = "";
}
