using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShowDialogue", menuName = "EventNodes/Action/ShowDialogue")]
public class ShowDialogue : ActionNode
{
    [Tooltip("说话者名称（留空则不显示）")]
    public string speaker;

    [Tooltip("要显示的文本内容")]
    [TextArea(3, 6)]
    public string text;

    public override Type[] GetRequiredServices()
    {
        return Array.Empty<Type>();
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        // 暂不支持 ENS 与 Yarn 的集成，为了让游戏可编译，立刻结束
        Debug.LogWarning($"[展示对话，未接入Yarn] 说话者：{speaker}，内容：{text}");
        onComplete?.Invoke();
    }
}