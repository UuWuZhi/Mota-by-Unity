using UnityEngine;
using VContainer;
using Yarn.Unity;

/// <summary>
/// 桥接 ENS 与 Yarn 的核心服务
/// </summary>
public class YarnRouteBridge
{
    private DialogueRunner _dialogueRunner;

    // 当前通过 Yarn Command ("ens_route") 收到的分支信号
    private string _currentRoute = "";

    public string CurrentRoute => _currentRoute;

    public YarnRouteBridge(DialogueRunner dialogueRunner)
    {
        _dialogueRunner = dialogueRunner;
        if (_dialogueRunner != null)
        {
            _dialogueRunner.AddCommandHandler<string>("ens_route", TriggerRoute);
        }
        else
        {
            Debug.LogError("YarnRouteBridge: DialogueRunner is missing.");
        }
    }

    private void TriggerRoute(string routeName)
    {
        _currentRoute = routeName;
        // 注意：这里可以选择直接结束当前对话（_dialogueRunner.Stop()）
        // 但通常最好让 Yarn 自然结束（它遇到 <<ens_route>> 下面没有内容就会自己结束）。
        // 如果想无缝，可以通过触发回调或其他方式。
    }

    public void ClearRoute()
    {
        _currentRoute = "";
    }
}
