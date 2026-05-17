using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.Runtime;
using Modules.EventNodeSystem.Runtime.Nodes;
using UnityEditor;
using UnityEngine;

namespace Editor.EventNodeSystem
{
    /// <summary>
    ///     ENS 注册表生成器：扫描节点与数据类型并生成映射表资产。
    /// </summary>
    public static class EnsRegistryGenerator
    {
        private const string OutputAssetPath = "Assets/Resources/ENS/NodeMappingTable.asset";

        /// <summary>
        ///     生成映射表资产并写入 Resources。
        /// </summary>
        [MenuItem("Tools/ENS/生成 Registry 映射")]
        public static void Generate()
        {
            // 扫描所有 BaseNodeData 子类
            var dataTypes = TypeCache.GetTypesDerivedFrom<BaseNodeData>()
                .Where(type => type is { IsAbstract: false, IsGenericType: false })
                .OrderBy(type => type.Name)
                .ToList();

            // 扫描所有 EventNode 子类
            var nodeTypes = TypeCache.GetTypesDerivedFrom<EventNode>()
                .Where(type => type is { IsAbstract: false, IsGenericType: false })
                .OrderBy(type => type.Name)
                .ToList();

            var mappings = BuildMappings(dataTypes, nodeTypes);
            WriteAsset(mappings);
            AssetDatabase.Refresh();
            Debug.Log($"EventNodeSystemRegistry 生成完成: {OutputAssetPath} (数量: {mappings.Count})");
        }

        /// <summary>
        ///     构建数据类型与节点模板的映射列表。
        /// </summary>
        /// <param name="dataTypes">数据类型列表。</param>
        /// <param name="nodeTypes">节点类型列表。</param>
        /// <returns>映射表条目列表。</returns>
        private static List<NodeMappingTableSo.Entry> BuildMappings(IReadOnlyList<Type> dataTypes,
            IReadOnlyList<Type> nodeTypes)
        {
            var mappings = new List<NodeMappingTableSo.Entry>();
            var matchedNodes = new HashSet<Type>();

            foreach (var dataType in dataTypes)
            {
                // 依据命名规则推导节点类型
                var dataKey = GetDataKey(dataType);
                var nodeType = FindNodeType(nodeTypes, dataKey, matchedNodes);
                if (nodeType == null)
                {
                    Debug.LogWarning($"ENSRegistryGenerator: 未找到匹配节点，Data = {dataType.Name}");
                    continue;
                }

                var nodeAsset = FindNodeAsset(nodeType);
                if (!nodeAsset) Debug.LogWarning($"ENSRegistryGenerator: 未找到节点资产，Node = {nodeType.Name}");

                matchedNodes.Add(nodeType);
                mappings.Add(new NodeMappingTableSo.Entry
                {
                    // 保存完整类型名以便运行时解析
                    dataTypeName = dataType.AssemblyQualifiedName,
                    node = nodeAsset
                });
            }

            return mappings;
        }

        /// <summary>
        ///     获取数据类型匹配用的短名。
        /// </summary>
        /// <param name="dataType">数据类型。</param>
        /// <returns>用于匹配的短名。</returns>
        private static string GetDataKey(Type dataType)
        {
            var name = dataType.Name;
            return name.EndsWith("Data", StringComparison.Ordinal) ? name[..^4] : name;
        }

        /// <summary>
        ///     根据命名规则找到对应节点类型。
        /// </summary>
        /// <param name="nodeTypes">节点类型列表。</param>
        /// <param name="dataKey">数据短名。</param>
        /// <param name="matchedNodes">已匹配节点集合。</param>
        /// <returns>匹配的节点类型。</returns>
        private static Type FindNodeType(IReadOnlyList<Type> nodeTypes, string dataKey, HashSet<Type> matchedNodes)
        {
            var exact = nodeTypes.FirstOrDefault(type => !matchedNodes.Contains(type) && type.Name == dataKey);
            return exact ?? nodeTypes.FirstOrDefault(type => !matchedNodes.Contains(type) && type.Name == dataKey + "Node");
        }

        /// <summary>
        ///     在工程中查找指定节点类型的资产。
        /// </summary>
        /// <param name="nodeType">节点类型。</param>
        /// <returns>节点资产。</returns>
        private static EventNode FindNodeAsset(Type nodeType)
        {
            // 按类型名查找 ScriptableObject 资产
            var filter = $"t:{nodeType.Name}";
            var guids = AssetDatabase.FindAssets(filter);
            if (guids == null || guids.Length == 0) return null;

            if (guids.Length > 1) Debug.LogWarning($"ENSRegistryGenerator: 检测到多个节点资产 {nodeType.Name}，将使用第一个。");

            var path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<EventNode>(path);
        }

        /// <summary>
        ///     写入或更新映射表资产。
        /// </summary>
        /// <param name="mappings">映射表条目列表。</param>
        private static void WriteAsset(IReadOnlyList<NodeMappingTableSo.Entry> mappings)
        {
            var directory = Path.GetDirectoryName(OutputAssetPath);
            if (!string.IsNullOrEmpty(directory))
                // 确保 Resources 目录存在
                Directory.CreateDirectory(directory);

            var table = AssetDatabase.LoadAssetAtPath<NodeMappingTableSo>(OutputAssetPath);
            if (!table)
            {
                // 首次生成时创建资产
                table = ScriptableObject.CreateInstance<NodeMappingTableSo>();
                AssetDatabase.CreateAsset(table, OutputAssetPath);
            }

            table.entries = mappings.ToList();
            // 标记资源已修改并保存
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();
        }
    }
}