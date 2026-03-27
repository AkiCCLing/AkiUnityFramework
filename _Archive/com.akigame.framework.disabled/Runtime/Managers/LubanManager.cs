using System;
using cfg;
using SimpleJSON;
using UnityEngine;

namespace Client
{
    /// <summary>
    /// Luban 数据表管理器
    /// 负责从 Resources 加载配置表，并提供类型安全的访问接口
    /// </summary>
    public static class LubanManager
    {
        // 对应 Assets/Resources/Config/LubanData
        private const string DataRoot = "Config/LubanData";

        private static Tables _tables;

        /// <summary>
        /// 是否已经初始化
        /// </summary>
        public static bool IsInitialized => _tables != null;

        /// <summary>
        /// 全局 Tables 实例
        /// </summary>
        public static Tables Tables
        {
            get
            {
                EnsureInitialized();
                return _tables;
            }
        }

        /// <summary>
        /// 初始化
        /// 可重复调用，已初始化时直接返回
        /// </summary>
        public static void Initialize()
        {
            if (_tables != null)
            {
                return;
            }

            _tables = new Tables(LoadJsonNode);
            Utils.LogSuccess("Luban", "数据表初始化完成");
        }

        /// <summary>
        /// 强制重载所有数据表
        /// </summary>
        public static void Reload()
        {
            _tables = new Tables(LoadJsonNode);
            Utils.LogSuccess("Luban", "数据表重载完成");
        }

        /// <summary>
        /// 根据 id 获取 item，找不到时返回 null
        /// </summary>
        public static item GetItemOrDefault(int id)
        {
            EnsureInitialized();
            return _tables.Tbitem.GetOrDefault(id);
        }

        /// <summary>
        /// 根据 id 获取 localization，找不到时返回 null
        /// </summary>
        public static localization GetLocalizationOrDefault(int id)
        {
            EnsureInitialized();
            return _tables.Tblocalization.GetOrDefault(id);
        }

        /// <summary>
        /// 按文本 id 和语言返回本地化字符串
        /// language 支持: "zh_cn" | "en"
        /// </summary>
        public static string GetText(int id, string language = "zh_cn")
        {
            var row = GetLocalizationOrDefault(id);
            if (row == null)
            {
                return string.Empty;
            }

            if (string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
            {
                return row.En;
            }

            return row.ZhCn;
        }

        private static void EnsureInitialized()
        {
            if (_tables == null)
            {
                Initialize();
            }
        }

        private static JSONNode LoadJsonNode(string file)
        {
            // Luban 传入的是表名，比如 "tbitem"、"tblocalization"
            var resourcePath = $"{DataRoot}/{file}";
            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                throw new Exception($"[Luban] 未找到数据文件: Resources/{resourcePath}");
            }

            return JSON.Parse(textAsset.text);
        }
    }
}
