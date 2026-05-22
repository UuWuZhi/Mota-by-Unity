// // using Modules.EventNodeSystem.DataDefine;
// using System;
// using System.Collections.Generic;
// using TriInspector;
// using TriInspector.Utilities;
// using UnityEditor;
// using UnityEditor.UIElements;
// using UnityEngine;
// using UnityEngine.UIElements;
//
// namespace Editor
// {
//     /// <summary>
//     ///     事件页编辑器窗口（UI Toolkit 简易实现）：窗口分为左侧摘要区和右侧详情区。
//     ///     该窗口可以被 EventNodeTile 的 Inspector 打开，当前仅实现占位布局。
//     /// </summary>
//     public class EventPageEditorWindow : TriEditorWindow
//     {
//         private const string LeftAreaClass = "eventpage-left";
//         private const string RightAreaClass = "eventpage-right";
//         private SerializedProperty _commandsProperty;
//         private int _lastCommandsCount = -1;
//
//         // 用于检测何时需要刷新列表（尽量避免每帧重建）
//         private int _lastSequenceInstanceId;
//         private VisualElement _leftArea;
//         private ListView _listView;
//         private VisualElement _rightArea;
//         private Action<IEnumerable<object>> _selectionCallback;
//
//         // SerializedObject/Property 用于支持撤销/重做与编辑操作
//         private SerializedObject _serializedTarget;
//
//         // 内部编辑时使用的序列容器（ScriptableObject）和对应的 Editor
//         private SequenceContainer _sequenceWrapper;
//         private UnityEditor.Editor _wrapperEditor;
//         private EventNodeTile _originalTile;
//
//         // 为了兼容旧代码引用，提供一个只读属性 _targetTile 指向原始 Tile（窗口优先使用 _sequenceWrapper）
//         private EventNodeTile _targetTile => _originalTile;
//
//         /// <summary>
//         ///     临时容器：用于在 EditorWindow 内保存可序列化的 EventSequence 副本，便于使用 SerializedObject/Editor 绘制与撤销支持。
//         /// </summary>
//         private class SequenceContainer : ScriptableObject
//         {
//             public EventSequence sequence = new EventSequence();
//         }
//
//         /// <summary>
//         ///     每帧更新窗口标题以反映当前目标（如果有）。
//         /// </summary>
//         private void Update()
//         {
//             try
//             {
//                 if (_originalTile != null)
//                     titleContent = new GUIContent($"EventPage - {_originalTile.name}");
//                 else if (_sequenceWrapper != null)
//                     titleContent = new GUIContent($"EventPage - {_sequenceWrapper.name}");
//                 else if (_targetTile != null)
//                     titleContent = new GUIContent($"EventPage - {_targetTile.name}");
//
//                 // 仅在目标或 commands 数量发生变化时刷新列表，避免每帧重建导致性能问题。
//                 TryRefreshListIfNeeded();
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(Update)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     窗口创建时初始化 UI 布局。
//         /// </summary>
//         protected override void OnEnable()
//         {
//             base.OnEnable();
//             try
//             {
//                 // 清理旧 UI
//                 rootVisualElement.Clear();
//
//                 // 创建主容器
//                 var root = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
//
//                 // 左侧摘要区（占固定宽度）
//                 _leftArea = new VisualElement { name = "leftArea", style = { width = 300, borderLeftWidth = 1 } };
//                 _leftArea.Add(new Label("Commands")
//                     { style = { unityFontStyleAndWeight = FontStyle.Bold, marginLeft = 6, marginTop = 6 } });
//
//                 // 创建 ListView，用于显示 commands 的摘要行。初始为空，后续 OnEnable/Update 时绑定数据。
//                 _listView = new ListView
//                 {
//                     name = "commandsList",
//                     style =
//                     {
//                         flexGrow = 1,
//                         marginTop = 6
//                     },
//                     selectionType = SelectionType.Single
//                 };
//
//                 // 列表占位文本
//                 var lvPlaceholder = new Label("(加载中...)") { style = { marginLeft = 6, marginTop = 6 } };
//                 _leftArea.Add(lvPlaceholder);
//
//                 // 右侧详情区（自适应）
//                 _rightArea = new VisualElement
//                     { name = "rightArea", style = { flexGrow = 1, borderLeftWidth = 1, paddingLeft = 8 } };
//                 _rightArea.Add(new Label("Details")
//                     { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6 } });
//                 // _rightDetailLabel = new Label("(占位) 详情面板将在此展示选中指令的 PropertyField") { style = { marginTop = 8 } };
//                 // _rightArea.Add(_rightDetailLabel);
//
//                 // 将左右两侧加入根
//                 root.Add(_leftArea);
//                 root.Add(_rightArea);
//
//                 rootVisualElement.Add(root);
//
//                 // 简单样式调整（内联）
//                 rootVisualElement.style.paddingLeft = 4;
//                 rootVisualElement.style.paddingRight = 4;
//                 rootVisualElement.style.paddingTop = 4;
//                 rootVisualElement.style.paddingBottom = 4;
//
//                 // 初始化 SerializedObject 与属性引用（若已设置 wrapper 或原始 target）
//                 if (_sequenceWrapper != null)
//                 {
//                     _serializedTarget = new SerializedObject(_sequenceWrapper);
//                     _commandsProperty = _serializedTarget.FindProperty("sequence")?.FindPropertyRelative("commands");
//                     _lastSequenceInstanceId = _originalTile != null ? _originalTile.GetInstanceID() : 0;
//                 }
//                 else if (_targetTile != null)
//                 {
//                     _serializedTarget = new SerializedObject(_targetTile);
//                     _commandsProperty = _serializedTarget.FindProperty("sequence")?.FindPropertyRelative("commands");
//                     _lastSequenceInstanceId = _targetTile.GetInstanceID();
//                 }
//
//                 // 尝试绑定一次（如果可能）
//                 TryRefreshListIfNeeded(true);
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(OnEnable)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     打开一个事件页窗口并聚焦到指定的 EventNodeTile。
//         /// </summary>
//         /// <param name="tile">要编辑的事件挂载点。</param>
//         public static void OpenFor(EventNodeTile tile)
//         {
//             try
//             {
//                 var wnd = GetWindow<EventPageEditorWindow>(true, "Event Page Editor", true);
//                 wnd.titleContent = new GUIContent($"EventPage - {(tile ? tile.name : null)}");
//                 wnd.minSize = new Vector2(600, 300);
//
//                 // 使用内部 SequenceContainer 拷贝原始 Tile 的 sequence 到窗口内部（编辑时不直接引用原对象）
//                 wnd._originalTile = tile;
//                 // 创建临时 ScriptableObject 作为序列副本
//                 wnd._sequenceWrapper = CreateInstance<SequenceContainer>();
//
//                 try
//                 {
//                     var origSO = new SerializedObject(tile);
//                     var origSeq = origSO.FindProperty("sequence");
//                     var wrapSO = new SerializedObject(wnd._sequenceWrapper);
//                     var wrapSeq = wrapSO.FindProperty("sequence");
//                     if (origSeq != null && wrapSeq != null)
//                     {
//                         // 手动复制：将原始序列字段逐项复制到 wrapper（因为 SerializedProperty 没有 CopyFromSerializedProperty ）
//                         CopySerializedSequence(origSO, wrapSO);
//                         wrapSO.ApplyModifiedProperties();
//                     }
//                 }
//                 catch (Exception copyEx)
//                 {
//                     Debug.LogWarning($"[EventPageEditorWindow(OpenFor)]: failed to copy sequence to wrapper: {copyEx}");
//                 }
//
//                 // 创建针对 wrapper 的 Editor，以便在窗口中复用 Inspector 绘制逻辑（包括 Tri-Inspector）
//                 if (wnd._sequenceWrapper != null)
//                 {
//                     wnd._wrapperEditor = UnityEditor.Editor.CreateEditor(wnd._sequenceWrapper);
//                     wnd._serializedTarget = new SerializedObject(wnd._sequenceWrapper);
//                 }
//
//                 wnd.Show();
//
//
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(OpenFor)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     手动复制 sequence 的 SerializedProperty 内容从源到目标。
//         /// </summary>
//         private static void CopySerializedSequence(SerializedObject srcSO, SerializedObject dstSO)
//         {
//             try
//             {
//                 var srcSeq = srcSO.FindProperty("sequence");
//                 var dstSeq = dstSO.FindProperty("sequence");
//                 if (srcSeq == null || dstSeq == null) return;
//
//                 // 复制 commands 数组
//                 var srcCommands = srcSeq.FindPropertyRelative("commands");
//                 var dstCommands = dstSeq.FindPropertyRelative("commands");
//                 if (srcCommands == null || dstCommands == null) return;
//
//                 dstCommands.arraySize = srcCommands.arraySize;
//                 for (int i = 0; i < srcCommands.arraySize; i++)
//                 {
//                     dstCommands.DeleteArrayElementAtIndex(i);
//                     dstCommands.InsertArrayElementAtIndex(i);
//                     var srcElem = srcCommands.GetArrayElementAtIndex(i);
//                     var dstElem = dstCommands.GetArrayElementAtIndex(i);
//                     dstElem.managedReferenceValue = srcElem.managedReferenceValue;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogWarning($"[EventPageEditorWindow(CopySerializedSequence)]: {ex}");
//             }
//         }
//
//
//
//         private void BindSequenceToList()
//         {
//             try
//             {
//                 if (!_targetTile || _targetTile.sequence == null)
//                     // 如果还未设置目标或 sequence 为空，则显示占位提示。
//                     return;
//
//                 if (_serializedTarget == null || _commandsProperty == null)
//                 {
//                     // 改为针对内部 wrapper
//                     if (_sequenceWrapper != null)
//                     {
//                         _serializedTarget = new SerializedObject(_sequenceWrapper);
//                         var seqProp = _serializedTarget.FindProperty("sequence");
//                         _commandsProperty = seqProp?.FindPropertyRelative("commands");
//                     }
//                     else if (_targetTile != null)
//                     {
//                         _serializedTarget = new SerializedObject(_targetTile);
//                         var seqProp = _serializedTarget.FindProperty("sequence");
//                         _commandsProperty = seqProp?.FindPropertyRelative("commands");
//                     }
//                 }
//
//                 if (_commandsProperty == null)
//                     return;
//
//                 // 以 SerializedProperty 为源创建 items list，显示每项的 GetSummary()
//                 var items = new List<string>();
//                 for (var i = 0; i < _commandsProperty.arraySize; i++)
//                 {
//                     var elem = _commandsProperty.GetArrayElementAtIndex(i);
//                     var summary = "(Null)";
//
//                     // 仅在属性是 ObjectReference 时安全访问 objectReferenceValue，避免对 SerializeReference 使用该 API（会抛异常）
//                     if (elem.propertyType == SerializedPropertyType.ObjectReference)
//                     {
//                         var objRef = elem.objectReferenceValue;
//                         if (objRef != null)
//                             summary = objRef.name;
//                     }
//                     else
//                     {
//                         // 对于 SerializeReference 或其他托管引用类型，从运行时列表回退以获取 GetSummary()
//                         var runtimeList = _targetTile.sequence.commands;
//                         if (runtimeList != null && i < runtimeList.Count)
//                         {
//                             var cmd = runtimeList[i];
//                             summary = cmd == null ? "(Null)" : cmd.GetSummary();
//                         }
//                     }
//
//                     items.Add(summary);
//                 }
//
//                 _listView.makeItem = () => new Label();
//                 _listView.bindItem = (ve, index) => { (ve as Label).text = items[index]; };
//                 _listView.itemsSource = items;
//
//                 // 注册 selection 回调（先注销旧回调）
//                 if (_selectionCallback != null)
//                 {
//                     try
//                     {
//                         _listView.selectionChanged -= _selectionCallback;
//                     }
//                     catch
//                     {
//                         // ignored
//                     }
//
//                     _selectionCallback = null;
//                 }
//
//                 _selectionCallback = objs =>
//                 {
//                     var sel = _listView.selectedIndex;
//                     _rightArea.Clear();
//                     _rightArea.Add(new Label("Details")
//                     { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 6 } });
//
//                     if (sel >= 0 && sel < _targetTile.sequence.commands.Count)
//                     {
//                         try
//                         {
//                             // 创建 IMGUIContainer，内部根据当前选中索引动态获取 SerializedProperty
//                             var imguiContainer = new IMGUIContainer(() =>
//                             {
//                                 // 每次重绘时重新获取当前选中的索引（防止捕获旧值）
//                                 int currentSel = _listView.selectedIndex;
//                                 if (currentSel < 0 || _targetTile == null || _targetTile.sequence == null)
//                                     return;
//                                 if (currentSel >= _targetTile.sequence.commands.Count)
//                                     return;
//
//                                 // 确保序列化对象和数据是最新的
//                                 if (_serializedTarget == null)
//                                     _serializedTarget = new SerializedObject(_targetTile);
//                                 _serializedTarget.UpdateIfRequiredOrScript();
//
//                                 // 动态获取当前选中元素的 SerializedProperty
//                                 var seqProp = _serializedTarget.FindProperty("sequence");
//                                 if (seqProp == null) return;
//                                 var commandsProp = seqProp.FindPropertyRelative("commands");
//                                 if (commandsProp == null || currentSel >= commandsProp.arraySize) return;
//                                 var elemProp = commandsProp.GetArrayElementAtIndex(currentSel);
//
//                                 // 使用 IMGUI 的 PropertyField，Tri-Inspector 的 Drawer 会自动生效
//                                 EditorGUILayout.PropertyField(elemProp, true);
//                                 _serializedTarget.ApplyModifiedProperties();
//                             });
//
//                             imguiContainer.style.flexGrow = 1;
//                             var scroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
//                             scroll.Add(imguiContainer);
//                             _rightArea.Add(scroll);
//                         }
//                         catch (Exception ex)
//                         {
//                             Debug.LogError($"[EventPageEditorWindow(SelectionCallback)]: {ex}");
//                             _rightArea.Add(new Label("(详情绘制出错)"));
//                         }
//                     }
//                     else
//                     {
//                         _rightArea.Add(new Label("(占位) 详情面板将在此展示选中指令的 PropertyField"));
//                     }
//                 };
//
//                 // 使用 selectionChanged 替代已废弃的 onSelectionChange
//                 _listView.selectionChanged += _selectionCallback;
//
//                 // 在绑定后，如果 wrapperEditor 可用，则渲染一份基于 wrapper 的完整 Inspector 到右侧，作为备用视图
//                 if (_wrapperEditor != null)
//                 {
//                     var inspectorContainer = new IMGUIContainer(() =>
//                     {
//                         try
//                         {
//                             // 绘制 wrapper 的完整 Inspector
//                             _wrapperEditor.OnInspectorGUI();
//                         }
//                         catch (Exception ex)
//                         {
//                             Debug.LogError($"[EventPageEditorWindow(WrapperInspector)]: {ex}");
//                         }
//                     });
//
//                     inspectorContainer.style.flexGrow = 1;
//                     var inspectorScroll = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
//                     inspectorScroll.Add(inspectorContainer);
//                     _rightArea.Add(inspectorScroll);
//                 }
//
//                 // 将 ListView 插入 leftArea（替换占位）并添加操作按钮工具栏
//                 _leftArea.Clear();
//                 _leftArea.Add(new Label("Commands")
//                     { style = { unityFontStyleAndWeight = FontStyle.Bold, marginLeft = 6, marginTop = 6 } });
//
//                 // 工具栏：新增、删除、上移、下移
//                 var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, marginLeft = 6, marginTop = 6, marginBottom = 6 } };
//                 var addBtn = new Button(() => { AddCommand(); }) { text = "+" , tooltip = "Add new command" };
//                 var removeBtn = new Button(() => { RemoveSelectedCommand(); }) { text = "-" , tooltip = "Remove selected command" };
//                 var upBtn = new Button(() => { MoveSelectedUp(); }) { text = "↑" , tooltip = "Move up" };
//                 var downBtn = new Button(() => { MoveSelectedDown(); }) { text = "↓" , tooltip = "Move down" };
//                 toolbar.Add(addBtn);
//                 toolbar.Add(removeBtn);
//                 toolbar.Add(upBtn);
//                 toolbar.Add(downBtn);
//
//                 _leftArea.Add(toolbar);
//                 _leftArea.Add(_listView);
//
//                 // 记录状态以便后续检测变更
//                 _lastSequenceInstanceId = _originalTile != null ? _originalTile.GetInstanceID() : (_targetTile != null ? _targetTile.GetInstanceID() : 0);
//                 _lastCommandsCount = _commandsProperty.arraySize;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(BindSequenceToList)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     确保 SerializedObject 与 commands 属性已初始化。
//         /// </summary>
//         private void EnsureSerialized()
//         {
//             try
//             {
//                 if (_serializedTarget == null || _commandsProperty == null)
//                 {
//                     if (_targetTile == null)
//                         return;
//                     _serializedTarget = new SerializedObject(_targetTile);
//                     var seqProp = _serializedTarget.FindProperty("sequence");
//                     _commandsProperty = seqProp?.FindPropertyRelative("commands");
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(EnsureSerialized)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     向命令列表末尾新增一个空项（基于 SerializedProperty），并支持撤销。
//         /// </summary>
//         private void AddCommand()
//         {
//             try
//             {
//                 // 如果存在 wrapper，则针对 wrapper 操作并记录 Undo 到 wrapper
//                 if (_sequenceWrapper != null)
//                 {
//                     EnsureSerialized();
//                     if (_commandsProperty == null)
//                         return;
//
//                     Undo.RecordObject(_sequenceWrapper, "Add Command");
//                     var insertIndex = _commandsProperty.arraySize;
//                     _commandsProperty.InsertArrayElementAtIndex(insertIndex);
//                     _serializedTarget.ApplyModifiedProperties();
//                     EditorUtility.SetDirty(_sequenceWrapper);
//
//                     TryRefreshListIfNeeded(force: true);
//                     _listView.selectedIndex = insertIndex;
//                 }
//                 else
//                 {
//                     if (_targetTile == null)
//                         return;
//
//                     EnsureSerialized();
//                     if (_commandsProperty == null)
//                         return;
//
//                     Undo.RecordObject(_targetTile, "Add Command");
//                     var insertIndex = _commandsProperty.arraySize;
//                     _commandsProperty.InsertArrayElementAtIndex(insertIndex);
//                     _serializedTarget.ApplyModifiedProperties();
//                     EditorUtility.SetDirty(_targetTile);
//
//                     TryRefreshListIfNeeded(force: true);
//                     _listView.selectedIndex = insertIndex;
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(AddCommand)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     删除当前选中的命令（如果有），并支持撤销。
//         /// </summary>
//         private void RemoveSelectedCommand()
//         {
//             try
//             {
//                 object undoTarget = _sequenceWrapper != null ? (object)_sequenceWrapper : (object)_targetTile;
//
//                 var sel = _listView.selectedIndex;
//                 if (sel < 0)
//                     return;
//
//                 EnsureSerialized();
//                 if (_commandsProperty == null || sel >= _commandsProperty.arraySize)
//                     return;
//
//                 Undo.RecordObject(undoTarget as UnityEngine.Object, "Remove Command");
//
//                 _commandsProperty.DeleteArrayElementAtIndex(sel);
//                 // 对于引用类型，DeleteArrayElementAtIndex 有时需要再次删除空占位；尝试调用一次 ApplyModifiedProperties 并检查数组长度变化。
//                 _serializedTarget.ApplyModifiedProperties();
//                 EditorUtility.SetDirty(undoTarget as UnityEngine.Object);
//
//                 // 刷新列表并调整选择到相邻项
//                 TryRefreshListIfNeeded(force: true);
//                 var newSel = Mathf.Clamp(sel, 0, Math.Max(0, _commandsProperty.arraySize - 1));
//                 _listView.selectedIndex = newSel;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(RemoveSelectedCommand)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     将选中项上移一位（如果可能），并支持撤销。
//         /// </summary>
//         private void MoveSelectedUp()
//         {
//             try
//             {
//                 object undoTarget = _sequenceWrapper != null ? (object)_sequenceWrapper : (object)_targetTile;
//
//                 var sel = _listView.selectedIndex;
//                 if (sel <= 0)
//                     return;
//
//                 EnsureSerialized();
//                 if (_commandsProperty == null)
//                     return;
//
//                 Undo.RecordObject(undoTarget as UnityEngine.Object, "Move Command Up");
//                 _commandsProperty.MoveArrayElement(sel, sel - 1);
//                 _serializedTarget.ApplyModifiedProperties();
//                 EditorUtility.SetDirty(undoTarget as UnityEngine.Object);
//
//                 TryRefreshListIfNeeded(force: true);
//                 _listView.selectedIndex = sel - 1;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(MoveSelectedUp)]: {ex}");
//             }
//         }
//
//         /// <summary>
//         ///     将选中项下移一位（如果可能），并支持撤销。
//         /// </summary>
//         private void MoveSelectedDown()
//         {
//             try
//             {
//                 object undoTarget = _sequenceWrapper != null ? (object)_sequenceWrapper : (object)_targetTile;
//
//                 var sel = _listView.selectedIndex;
//                 if (sel < 0)
//                     return;
//
//                 EnsureSerialized();
//                 if (_commandsProperty == null)
//                     return;
//
//                 if (sel >= _commandsProperty.arraySize - 1)
//                     return;
//
//                 Undo.RecordObject(undoTarget as UnityEngine.Object, "Move Command Down");
//                 _commandsProperty.MoveArrayElement(sel, sel + 1);
//                 _serializedTarget.ApplyModifiedProperties();
//                 EditorUtility.SetDirty(undoTarget as UnityEngine.Object);
//
//                 TryRefreshListIfNeeded(force: true);
//                 _listView.selectedIndex = sel + 1;
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(MoveSelectedDown)]: {ex}");
//             }
//         }
//
//         private void TryRefreshListIfNeeded(bool force = false)
//         {
//             try
//             {
//                 if (_targetTile == null)
//                     return;
//
//                 // force 表示强制刷新（用于首次绑定）
//                 if (force)
//                 {
//                     BindSequenceToList();
//                     return;
//                 }
//
//                 // 如果使用 wrapper，则依据原始 tile 的 instance id 检测
//                 if (_originalTile != null)
//                 {
//                     if (_originalTile.GetInstanceID() != _lastSequenceInstanceId)
//                     {
//                         // 如果原始 tile 发生变化（不太可能），重新绑定为 wrapper 或原始 tile
//                         if (_sequenceWrapper != null)
//                             _serializedTarget = new SerializedObject(_sequenceWrapper);
//                         else
//                             _serializedTarget = new SerializedObject(_originalTile);
//
//                         var seqProp = _serializedTarget.FindProperty("sequence");
//                         _commandsProperty = seqProp?.FindPropertyRelative("commands");
//                         BindSequenceToList();
//                         return;
//                     }
//                 }
//                 else if (_targetTile != null)
//                 {
//                     if (_targetTile.GetInstanceID() != _lastSequenceInstanceId)
//                     {
//                         _serializedTarget = new SerializedObject(_targetTile);
//                         var seqProp = _serializedTarget.FindProperty("sequence");
//                         _commandsProperty = seqProp?.FindPropertyRelative("commands");
//                         BindSequenceToList();
//                         return;
//                     }
//                 }
//
//                 // 如果命令数量发生变化，刷新列表
//                 if (_commandsProperty != null && _commandsProperty.arraySize != _lastCommandsCount)
//                     BindSequenceToList();
//             }
//             catch (Exception ex)
//             {
//                 Debug.LogError($"[EventPageEditorWindow(TryRefreshListIfNeeded)]: {ex}");
//             }
//         }
//     }
// }