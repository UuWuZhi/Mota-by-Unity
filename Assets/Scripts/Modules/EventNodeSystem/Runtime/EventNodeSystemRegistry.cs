using System;
using System.Collections.Generic;
using Modules.EventNodeSystem.DataDefine;
using Modules.EventNodeSystem.Runtime.Nodes;

namespace Modules.EventNodeSystem.Runtime
{
    /// <summary>
    ///     ENS 映射注册表：维护 Data 类型到节点模板的映射关系。
    /// </summary>
    public class EventNodeSystemRegistry
    {
        private readonly Dictionary<Type, EventNode> _nodesByDataType = new();

        /// <summary>
        ///     注册映射关系。
        /// </summary>
        /// <param name="dataType">数据类型。</param>
        /// <param name="node">节点模板。</param>
        public void Register(Type dataType, EventNode node)
        {
            if (dataType == null || !node) return;
            _nodesByDataType[dataType] = node;
        }

        /// <summary>
        ///     注册映射关系（泛型版本）。
        /// </summary>
        /// <typeparam name="TData">数据类型。</typeparam>
        /// <param name="node">节点模板。</param>
        public void Register<TData>(EventNode node) where TData : BaseNodeData
        {
            Register(typeof(TData), node);
        }

        /// <summary>
        ///     获取映射的节点模板。
        /// </summary>
        /// <param name="dataType">数据类型。</param>
        /// <returns>节点模板或 null。</returns>
        public EventNode GetNode(Type dataType)
        {
            if (dataType == null) return null;
            _nodesByDataType.TryGetValue(dataType, out var node);
            return node;
        }

        /// <summary>
        ///     尝试获取映射的节点模板。
        /// </summary>
        /// <param name="dataType">数据类型。</param>
        /// <param name="node">节点模板。</param>
        /// <returns>是否找到映射。</returns>
        public bool TryGetNode(Type dataType, out EventNode node)
        {
            node = null;
            if (dataType == null) return false;
            return _nodesByDataType.TryGetValue(dataType, out node) && node;
        }

        /// <summary>
        ///     尝试获取映射的节点模板（泛型版本）。
        /// </summary>
        /// <typeparam name="TData">数据类型。</typeparam>
        /// <param name="node">节点模板。</param>
        /// <returns>是否找到映射。</returns>
        public bool TryGetNode<TData>(out EventNode node) where TData : BaseNodeData
        {
            return TryGetNode(typeof(TData), out node);
        }

        /// <summary>
        ///     清空所有映射。
        /// </summary>
        public void Clear()
        {
            _nodesByDataType.Clear();
        }

        /// <summary>
        ///     从映射表资产加载并填充注册表。
        /// </summary>
        /// <param name="table">映射表资产。</param>
        public void LoadFromMappingTable(NodeMappingTableSo table)
        {
            if (!table) return;
            Clear();
            foreach (var entry in table.entries)
            {
                // 解析数据类型并写入映射
                var dataType = entry?.ResolveDataType();
                if (dataType == null || !entry.node) continue;
                Register(dataType, entry.node);
            }
        }

        /// <summary>
        ///     从 Resources 中加载映射表并填充注册表。
        /// </summary>
        /// <param name="resourcePath">Resources 路径。</param>
        /// <returns>是否成功加载。</returns>
        public bool TryLoadFromResources(string resourcePath = NodeMappingTableSo.DefaultResourcesPath)
        {
            // 从 Resources 读取映射表资产
            var table = NodeMappingTableSo.LoadFromResources(resourcePath);
            if (!table) return false;
            LoadFromMappingTable(table);
            return true;
        }

        /// <summary>
        ///     获取只读映射表。
        /// </summary>
        /// <returns>映射字典。</returns>
        public IReadOnlyDictionary<Type, EventNode> GetAll()
        {
            return _nodesByDataType;
        }
    }
}