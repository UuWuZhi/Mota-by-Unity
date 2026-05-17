using System;
using System.Collections.Generic;
using Modules.EventNodeSystem.Runtime.Nodes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Modules.EventNodeSystem.Runtime
{
    /// <summary>
    ///     ENS 节点映射表资产：用于保存 Data 类型与节点模板的对应关系。
    /// </summary>
    [CreateAssetMenu(fileName = "NodeMappingTable", menuName = "ENS/Node Mapping Table")]
    public class NodeMappingTableSo : ScriptableObject
    {
        /// <summary>
        ///     默认 Resources 加载路径。
        /// </summary>
        public const string DefaultResourcesPath = "ENS/NodeMappingTable";

        /// <summary>
        ///     映射表条目集合。
        /// </summary>
        [FormerlySerializedAs("Entries")] public List<Entry> entries = new();

        /// <summary>
        ///     从 Resources 加载映射表资产。
        /// </summary>
        /// <param name="resourcePath">Resources 路径。</param>
        /// <returns>加载到的映射表资产。</returns>
        public static NodeMappingTableSo LoadFromResources(string resourcePath = DefaultResourcesPath)
        {
            return Resources.Load<NodeMappingTableSo>(resourcePath);
        }

        [Serializable]
        public class Entry
        {
            /// <summary>
            ///     数据类型的 AssemblyQualifiedName。
            /// </summary>
            public string dataTypeName;

            /// <summary>
            ///     对应的节点模板资产。
            /// </summary>
            public EventNode node;

            /// <summary>
            ///     解析数据类型。
            /// </summary>
            /// <returns>解析得到的类型，失败则返回 null。</returns>
            public Type ResolveDataType()
            {
                if (string.IsNullOrEmpty(dataTypeName)) return null;
                var type = Type.GetType(dataTypeName);
                if (type != null) return type;
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    // 逐个程序集尝试解析类型
                    type = assembly.GetType(dataTypeName);
                    if (type != null) return type;
                }

                return null;
            }
        }
    }
}