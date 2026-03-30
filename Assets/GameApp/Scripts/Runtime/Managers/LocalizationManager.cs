using AkiFramework;
using UnityEngine;

namespace GameApp
{
    /// <summary>
    /// 多语言管理器
    /// 负责维护当前语言，并通过 LubanManager 读取多语言表数据
    /// </summary>
    public static class LocalizationManager
    {
        public const string ZhCn = "zh_cn";
        public const string En = "en";

        private const string LanguagePrefsKey = "GameApp.Language";

        private static string _currentLanguage = ZhCn;

        /// <summary>
        /// 当前语言
        /// </summary>
        public static string CurrentLanguage => _currentLanguage;

        /// <summary>
        /// 设置当前语言
        /// 语言切换后会发送 ChangeLanguageSignal
        /// </summary>
        public static void SetLanguage(string language, bool save = true)
        {
            var normalized = NormalizeLanguage(language);
            if (_currentLanguage == normalized)
            {
                if (save)
                {
                    SaveLanguage(normalized);
                }
                return;
            }

            _currentLanguage = normalized;
            if (save)
            {
                SaveLanguage(normalized);
            }

            Signals.Get<ChangeLanguageSignal>().Dispatch(_currentLanguage);
            Utils.LogInfo("Localization", $"当前语言切换为: {_currentLanguage}");
        }

        /// <summary>
        /// 加载已保存的语言设置
        /// 没有保存值时使用默认语言
        /// </summary>
        public static void LoadSavedLanguage(string defaultLanguage = ZhCn)
        {
            var language = PlayerPrefs.GetString(LanguagePrefsKey, NormalizeLanguage(defaultLanguage));
            SetLanguage(language, false);
        }

        /// <summary>
        /// 重新广播当前语言，用于强制刷新所有监听对象
        /// </summary>
        public static void RefreshCurrentLanguage()
        {
            Signals.Get<ChangeLanguageSignal>().Dispatch(_currentLanguage);
        }

        /// <summary>
        /// 根据多语言 id 获取当前语言文本
        /// </summary>
        public static string GetText(int localizationId)
        {
            return LubanManager.GetText(localizationId, _currentLanguage);
        }

        /// <summary>
        /// 根据多语言 id 和指定语言获取文本
        /// </summary>
        public static string GetText(int localizationId, string language)
        {
            return LubanManager.GetText(localizationId, NormalizeLanguage(language));
        }

        private static void SaveLanguage(string language)
        {
            PlayerPrefs.SetString(LanguagePrefsKey, language);
            PlayerPrefs.Save();
        }

        private static string NormalizeLanguage(string language)
        {
            if (string.IsNullOrWhiteSpace(language))
            {
                return ZhCn;
            }

            language = language.Trim().ToLowerInvariant();
            switch (language)
            {
                case En:
                    return En;
                case ZhCn:
                default:
                    return ZhCn;
            }
        }
    }
}
