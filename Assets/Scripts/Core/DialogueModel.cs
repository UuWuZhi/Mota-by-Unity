using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话选项（分支）
/// </summary>
[Serializable]
public class DialogueChoice
{
    public string Text;
    public string NextNodeId; // 选择后跳转到的节点 id（null/empty 表示结束）
    public string ActionName; // 可选：选择触发的动作名（由 DialogueManager 处理）
}

/// <summary>
/// 单一对话节点
/// </summary>
[Serializable]
public class DialogueNode
{
    public string Id;
    public string Speaker; // 可用于显示说话人名字
    [TextArea(2, 6)]
    public string Text;
    public List<DialogueChoice> Choices = new List<DialogueChoice>();
    public string NextNodeId; // 当没有 Choice 时的顺序流（可为空）
    public bool IsEnd => (Choices == null || Choices.Count == 0) && string.IsNullOrEmpty(NextNodeId);
}