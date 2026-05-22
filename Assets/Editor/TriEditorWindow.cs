namespace Editor
{
    [TriInspector.HideMonoScript]
    public class TriEditorWindow : UnityEditor.EditorWindow
    {
        private UnityEditor.Editor _editor;
        protected virtual void OnEnable() => _editor = UnityEditor.Editor.CreateEditor(this);
        protected virtual void OnDisable() => DestroyImmediate(_editor);
        protected virtual void OnGUI() => _editor.OnInspectorGUI();
    }
}