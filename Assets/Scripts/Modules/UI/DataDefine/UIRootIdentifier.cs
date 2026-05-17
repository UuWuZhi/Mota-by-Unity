using UnityEngine;
using UnityEngine.Serialization;

// 挂在 UI 根节点上，用于在注册时声明它对应的枚举类型
namespace Modules.UI.DataDefine
{
    public class UIRootIdentifier : MonoBehaviour
    {
        [FormerlySerializedAs("Type")] public UIRootType type = UIRootType.None;
    }
}