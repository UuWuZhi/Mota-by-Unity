using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IfFlowNode))]
public class IfFlowNodeEditor : EventNodeNestedEditor
{
    private SerializedProperty nodeNameProp;
    private SerializedProperty conditionProp;
    private SerializedProperty trueBranchProp;
    private SerializedProperty falseBranchProp;

    private bool conditionFoldout;
    private readonly List<bool> trueFoldouts = new List<bool>();
    private readonly List<bool> falseFoldouts = new List<bool>();

    private void OnEnable()
    {
        nodeNameProp = serializedObject.FindProperty("nodeName");
        conditionProp = serializedObject.FindProperty("condition");
        trueBranchProp = serializedObject.FindProperty("trueBranch");
        falseBranchProp = serializedObject.FindProperty("falseBranch");
    }

    protected override void DrawInspectorGUI()
    {
        serializedObject.Update();

        if (nodeNameProp != null)
        {
            EditorGUILayout.PropertyField(nodeNameProp);
        }

        DrawEventNodeField(conditionProp, "条件", ref conditionFoldout, typeof(ConditionNode));
        DrawEventNodeList(trueBranchProp, "True 分支", trueFoldouts);
        DrawEventNodeList(falseBranchProp, "False 分支", falseFoldouts);

        serializedObject.ApplyModifiedProperties();
    }
}
