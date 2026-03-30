using TATools.FolderCommentTool;
using UnityEditor;
using UnityEngine;

namespace AkiFramework.Editor
{
    [InitializeOnLoad]
    public static class FolderCommentSync
    {
        private static readonly Color PluginColor = new(1f, 0.55f, 0f, 1f);
        private static readonly Color AppColor = new(0f, 0.65f, 1f, 1f);
        private static readonly Color FrameworkColor = new(0.32f, 0.9f, 0.18f, 1f);
        private static readonly Color ArtResColor = new(0.72f, 0.22f, 1f, 1f);
        private static readonly Color ResourceColor = new(1f, 0.82f, 0f, 1f);
        private static readonly Color TestColor = new(1f, 0.18f, 0.18f, 1f);
        private static readonly Color SettingColor = new(0.5f, 0.55f, 0.95f, 1f);
        private static readonly Color StreamingColor = new(0.78f, 1f, 0f, 1f);
        private static readonly Color AddressableColor = new(0.1f, 0.8f, 0.6f, 1f);

        static FolderCommentSync()
        {
            EditorApplication.delayCall += SyncDefaultComments;
        }

        [MenuItem("Anii Tools/Folder Comments/Sync Default Comments")]
        public static void SyncDefaultComments()
        {
            SetComment("Assets/Plugins", "◎ 第三方插件", "外部插件、第三方资源与供应商示例", PluginColor);
            SetComment("Assets/GameApp", "◆ 游戏业务", "业务逻辑、数据、测试与项目内配置", AppColor);
            SetComment("Assets/AkiFramework", "▣ 框架目录", "Aki 框架核心、编辑器扩展与框架示例", FrameworkColor);
            SetComment("Assets/ArtRes", "◈ 游戏资产", "UI、美术、预制体、场景截图等游戏资产", ArtResColor);
            SetComment("Assets/Resources", "◉ 运行时资源", "通过 Resources 直接加载的配置和资源", ResourceColor);
            SetComment("Assets/AddressableAssetsData", "⬢ Addressables", "Addressables 配置、分组与构建脚本", AddressableColor);
            SetComment("Assets/GameApp/Tests", "▲ 测试目录", "编辑器测试和运行时测试", TestColor);
            SetComment("Assets/GameApp/Settings", "▤ 游戏配置", "项目内渲染与运行配置资源", SettingColor);
            SetComment("Assets/StreamingAssets", "⬡ 外部文件", "构建后需要原样输出的资源", StreamingColor);

            EditorApplication.RepaintProjectWindow();
        }

        private static void SetComment(string assetPath, string title, string comment, Color color)
        {
            if (!AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            FolderCommentManager.Instance.SetFolderCommentByPath(assetPath, title, comment, color);
        }
    }
}
