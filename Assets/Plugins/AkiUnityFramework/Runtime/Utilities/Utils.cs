using UnityEngine;

namespace Client
{
    /// <summary>
    /// 通用工具类
    /// 当前包含彩色日志输出能力
    /// </summary>
    public static class Utils
    {
        private const string SuccessColor = "#00FF00";
        private const string InfoColor = "#409EFF";
        private const string WarningColor = "#E6A23C";
        private const string ErrorColor = "#F56C6C";

        public static void LogInfo(string tag, string message, Object context = null)
        {
            Debug.Log(BuildMessage(tag, message, InfoColor), context);
        }

        public static void LogSuccess(string tag, string message, Object context = null)
        {
            Debug.Log(BuildMessage(tag, message, SuccessColor), context);
        }

        public static void LogWarning(string tag, string message, Object context = null)
        {
            Debug.LogWarning(BuildMessage(tag, message, WarningColor), context);
        }

        public static void LogError(string tag, string message, Object context = null)
        {
            Debug.LogError(BuildMessage(tag, message, ErrorColor), context);
        }

        private static string BuildMessage(string tag, string message, string color)
        {
            var content = string.IsNullOrWhiteSpace(tag)
                ? message
                : $"[{tag}] {message}";

            return string.IsNullOrWhiteSpace(color)
                ? content
                : $"<color={color}>{content}</color>";
        }
    }
}
