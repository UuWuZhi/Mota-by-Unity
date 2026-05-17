using Modules.UI.DataDefine;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Modules.UI.Runtime
{
    /// <summary>
    ///     UI 基类：包含 UIRootType、控制的 root GameObject、显示状态，以及封装的 Show/Hide/Toggle 方法。
    ///     自动在 OnEnable 时尝试向场景中的 UIManager 注册（如果设置了 Type）。
    ///     派生类可重写 OnShown / OnHidden 来处理显示/隐藏后的额外逻辑。
    /// </summary>
    public class BaseUI : MonoBehaviour
    {
        [FormerlySerializedAs("Type")] [Tooltip("此 UI 对应的枚举类型，用于在 UIManager 中注册与查找")]
        public UIRootType type = UIRootType.None;

        [Tooltip("作为该 UI 根的 GameObject；若为空会使用当前挂载对象")]
        public GameObject root;

        private bool _registered;

        private UIManager _uiManager;

        protected virtual bool ShowOnAwake => false;

        /// <summary>
        ///     当前是否可见（root.activeSelf）
        /// </summary>
        public bool IsVisible => root && root.activeSelf;

        protected virtual void Awake()
        {
            if (!root) root = gameObject;
            if (ShowOnAwake)
                Show();
            else
                Hide();
        }

        protected virtual void OnEnable()
        {
            TryRegisterRoot();
        }

        protected virtual void OnDestroy()
        {
            TryUnregisterRoot();
        }

        [Inject]
        public void Inject(UIManager uiManager)
        {
            _uiManager = uiManager;
            TryRegisterRoot();
        }

        private void TryRegisterRoot()
        {
            if (_registered) return;
            if (!root) return;
            if (type == UIRootType.None) return;
            if (!_uiManager) return;
            // 注册当前 BaseUI 实例，UIManager 现在持有 BaseUI
            _uiManager.RegisterUIRoot(this, type);
            _registered = true;
        }

        private void TryUnregisterRoot()
        {
            if (!_registered) return;
            if (!_uiManager || !root) return;
            _uiManager.UnregisterUIRoot(this);
            _registered = false;
        }

        /// <summary>
        ///     显示 UI 根
        /// </summary>
        public virtual void Show()
        {
            if (!root) return;
            root.SetActive(true);
            OnShown();
        }

        /// <summary>
        ///     隐藏 UI 根
        /// </summary>
        public virtual void Hide()
        {
            if (!root) return;
            root.SetActive(false);
            OnHidden();
        }

        public virtual void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        /// <summary>
        ///     派生类在显示后可以覆盖此方法执行额外逻辑
        /// </summary>
        protected virtual void OnShown()
        {
        }

        /// <summary>
        ///     派生类在隐藏后可以覆盖此方法执行额外逻辑
        /// </summary>
        protected virtual void OnHidden()
        {
        }
    }
}