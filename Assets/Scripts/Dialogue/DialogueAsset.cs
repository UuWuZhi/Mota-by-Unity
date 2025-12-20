using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 对话资产：在编辑器中创建并序列化对话树
/// </summary>
[CreateAssetMenu(fileName = "DialogueAsset", menuName = "Dialogue/DialogueAsset", order = 1)]
public class DialogueAsset : ScriptableObject
{
    public List<DialogueNode> Nodes = new List<DialogueNode>();

    public DialogueNode GetNodeById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return Nodes.Find(n => string.Equals(n.Id, id, StringComparison.Ordinal));
    }

    public DialogueNode GetStartNode()
    {
        return Nodes.Count > 0 ? Nodes[0] : null;
    }
}