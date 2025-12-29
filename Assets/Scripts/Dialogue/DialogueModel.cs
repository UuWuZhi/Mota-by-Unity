using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话选项（分支）
/// </summary>
[Serializable]
public class DialogueOption
{
    public string Text;
    public string ActionName; // 可选：选择触发的动作名（由 DialogueManager 处理）
}

/// <summary>
/// 对话数据（用于 UI 展示）
/// 仅包含必要的显示字段：说话者、文本、以及选项列表（若无则为单段对话）
/// </summary>
[Serializable]
public class DialogueData
{
    public string Id;
    public string Speaker; // 可用于显示说话人名字
    [TextArea(2, 6)]
    public string Text;
    public List<DialogueOption> Options = new List<DialogueOption>();

    public bool IsEnd => Options == null || Options.Count == 0;
}