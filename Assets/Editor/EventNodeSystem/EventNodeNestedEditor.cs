//using System;
//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;

//internal static class EventNodeInspectorContext
//{
//    [ThreadStatic]
//    private static List<EventNode> stack;

//    public static int Depth => stack?.Count ?? 0;

//    public static bool Contains(EventNode node)
//    {
//        if (node == null || stack == null) return false;
//        for (int i = 0; i < stack.Count; i++)
//        {
//            if (stack[i] == node) return true;
//        }
//        return false;
//    }

//    public static void Push(EventNode node)
//    {
//        if (node == null) return;
//        if (stack == null) stack = new List<EventNode>();
//        stack.Add(node);
//    }

//    public static void Pop(EventNode node)
//    {
//        if (node == null || stack == null || stack.Count == 0) return;
//        if (stack[stack.Count - 1] == node)
//        {
//            stack.RemoveAt(stack.Count - 1);
//            return;
//        }
//        stack.Remove(node);
//    }
//}

//internal readonly struct EventNodeInspectorScope : IDisposable
//{
//    private readonly EventNode node;
//    private readonly bool pushed;

//    public EventNodeInspectorScope(EventNode node)
//    {
//        this.node = node;
//        if (node != null)
//        {
//            EventNodeInspectorContext.Push(node);
//            pushed = true;
//        }
//        else
//        {
//            pushed = false;
//        }
//    }

//    public void Dispose()
//    {
//        if (pushed)
//        {
//            EventNodeInspectorContext.Pop(node);
//        }
//    }
//}

//public abstract class EventNodeNestedEditor : Editor
//{
//    private static readonly Dictionary<int, Editor> CachedEditors = new Dictionary<int, Editor>();

//    protected virtual int MaxDepth => 5;

//    public override void OnInspectorGUI()
//    {
//        using (new EventNodeInspectorScope(target as EventNode))
//        {
//            DrawInspectorGUI();
//        }
//    }

//    protected abstract void DrawInspectorGUI();

//    protected void DrawEventNodeField(SerializedProperty nodeProp, string label, ref bool foldout, Type objectType)
//    {
//        if (nodeProp == null) return;

//        EditorGUILayout.BeginVertical("box");
//        EditorGUILayout.BeginHorizontal();
//        foldout = EditorGUILayout.Foldout(foldout, label, true);
//        var newValue = EditorGUILayout.ObjectField(nodeProp.objectReferenceValue, objectType, false);
//        nodeProp.objectReferenceValue = newValue;
//        EditorGUILayout.EndHorizontal();

//        if (foldout && nodeProp.objectReferenceValue != null)
//        {
//            DrawNestedInspector(nodeProp.objectReferenceValue as EventNode);
//        }

//        EditorGUILayout.EndVertical();
//    }

//    protected void DrawEventNodeList(SerializedProperty listProp, string label, List<bool> foldouts)
//    {
//        if (listProp == null || !listProp.isArray) return;

//        EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
//        EditorGUI.indentLevel++;

//        int newSize = Mathf.Max(0, EditorGUILayout.IntField("数量", listProp.arraySize));
//        if (newSize != listProp.arraySize)
//        {
//            listProp.arraySize = newSize;
//        }

//        EnsureFoldouts(foldouts, listProp.arraySize);

//        int removeIndex = -1;
//        for (int i = 0; i < listProp.arraySize; i++)
//        {
//            var element = listProp.GetArrayElementAtIndex(i);
//            EditorGUILayout.BeginVertical("box");
//            EditorGUILayout.BeginHorizontal();
//            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], $"元素 {i}", true);
//            var newValue = EditorGUILayout.ObjectField(element.objectReferenceValue, typeof(EventNode), false);
//            element.objectReferenceValue = newValue;
//            if (GUILayout.Button("删除", GUILayout.Width(48)))
//            {
//                removeIndex = i;
//            }
//            EditorGUILayout.EndHorizontal();

//            if (foldouts[i] && element.objectReferenceValue != null)
//            {
//                DrawNestedInspector(element.objectReferenceValue as EventNode);
//            }

//            EditorGUILayout.EndVertical();
//        }

//        if (removeIndex >= 0)
//        {
//            DeleteArrayElementAtIndex(listProp, removeIndex);
//        }

//        if (GUILayout.Button("添加"))
//        {
//            listProp.arraySize += 1;
//            EnsureFoldouts(foldouts, listProp.arraySize);
//        }

//        EditorGUI.indentLevel--;
//    }

//    private void DrawNestedInspector(EventNode node)
//    {
//        if (node == null) return;

//        if (EventNodeInspectorContext.Contains(node))
//        {
//            EditorGUILayout.HelpBox("检测到循环引用，已停止展开。", MessageType.Info);
//            return;
//        }

//        if (EventNodeInspectorContext.Depth >= MaxDepth)
//        {
//            EditorGUILayout.HelpBox($"超过最大嵌套深度({MaxDepth})，已停止展开。", MessageType.Info);
//            return;
//        }

//        EditorGUI.indentLevel++;
//        var editor = GetCachedEditor(node);
//        editor?.OnInspectorGUI();
//        EditorGUI.indentLevel--;
//    }

//    private static Editor GetCachedEditor(EventNode node)
//    {
//        if (node == null) return null;
//        int id = node.GetInstanceID();
//        CachedEditors.TryGetValue(id, out var editor);
//        Editor.CreateCachedEditor(node, null, ref editor);
//        CachedEditors[id] = editor;
//        return editor;
//    }

//    private static void EnsureFoldouts(List<bool> foldouts, int size)
//    {
//        if (foldouts == null) return;
//        while (foldouts.Count < size)
//        {
//            foldouts.Add(false);
//        }
//        while (foldouts.Count > size)
//        {
//            foldouts.RemoveAt(foldouts.Count - 1);
//        }
//    }

//    private static void DeleteArrayElementAtIndex(SerializedProperty listProp, int index)
//    {
//        if (index < 0 || index >= listProp.arraySize) return;
//        listProp.DeleteArrayElementAtIndex(index);
//        if (index < listProp.arraySize && listProp.GetArrayElementAtIndex(index).objectReferenceValue != null)
//        {
//            listProp.DeleteArrayElementAtIndex(index);
//        }
//    }
//}

