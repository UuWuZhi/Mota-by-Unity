using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShowDialogueAction", menuName = "EventNodes/Action/ShowDialogue")]
public class ShowDialogueAction : ActionNode
{
    [Tooltip("说话者名称（留空则不显示）")]
    public string speaker;

    [Tooltip("要显示的文本内容")]
    [TextArea(3, 6)]
    public string text;

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke();
            return;
        }

        // 利用现有DialogueManager的Show方法显示文本
        // 并在对话结束后调用onComplete继续节点流程
        DialogueManager.Instance.Show(text,speaker, () =>
        {
            onComplete?.Invoke();
        });
    }
}