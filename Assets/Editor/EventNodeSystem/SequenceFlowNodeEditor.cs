//using System.Collections.Generic;
//using UnityEditor;
//using UnityEngine;

//[CustomEditor(typeof(SequenceFlowNode))]
//public class SequenceFlowNodeEditor : EventNodeNestedEditor
//{
//    private SerializedProperty nodeNameProp;
//    private SerializedProperty childrenProp;
//    private readonly List<bool> childrenFoldouts = new List<bool>();

//    private void OnEnable()
//    {
//        nodeNameProp = serializedObject.FindProperty("nodeName");
//        childrenProp = serializedObject.FindProperty("children");
//    }

//    protected override void DrawInspectorGUI()
//    {
//        serializedObject.Update();

//        if (nodeNameProp != null)
//        {
//            EditorGUILayout.PropertyField(nodeNameProp);
//        }

//        DrawEventNodeList(childrenProp, "子节点", childrenFoldouts);

//        serializedObject.ApplyModifiedProperties();
//    }
//}

