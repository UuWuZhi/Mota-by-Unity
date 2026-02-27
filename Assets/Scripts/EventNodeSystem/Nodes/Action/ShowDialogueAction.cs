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

    public override Type[] GetRequiredServices()
    {
        return new[] { typeof(DialogueManager) };
    }

    public override void Execute(EventNodeContext ctx, Action onComplete)
    {
        if (string.IsNullOrEmpty(text))
        {
            onComplete?.Invoke();
            return;
        }

        // 优先通过上下文服务字典获取 DialogueManager
        var dm = ctx.GetService<DialogueManager>();
        if (dm == null)
        {
            // 回退：尝试通过容器注入的单例（如果容器已注册并注入到调用者）
            Debug.LogWarning("ShowDialogueAction: DialogueManager 未在 EventNodeContext 中提供，无法显示对话");
            onComplete?.Invoke();
            return;
        }

        dm.Show(text, speaker, () =>
        {
            onComplete?.Invoke();
        });
    }
}