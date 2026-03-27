using TATools.FolderCommentTool;
using UnityEditor;
using UnityEngine;

namespace Client.Editor
{
    /// <summary>
    /// 同步项目默认文件夹注释
    /// 通过插件 API 写入 确保编辑器能立刻识别
    /// </summary>
    [InitializeOnLoad]
    public static class FolderCommentSync
    {
        private static readonly Color ThirdPartyColor = new(1f, 0.55f, 0f, 1f);
        private static readonly Color ScriptColor = new(0f, 0.65f, 1f, 1f);
        private static readonly Color PrefabColor = new(0.32f, 0.9f, 0.18f, 1f);
        private static readonly Color SceneColor = new(0.72f, 0.22f, 1f, 1f);
        private static readonly Color ResourceColor = new(1f, 0.82f, 0f, 1f);
        private static readonly Color UiColor = new(0f, 0.92f, 0.78f, 1f);
        private static readonly Color ArtColor = new(1f, 0.2f, 0.58f, 1f);
        private static readonly Color TestColor = new(1f, 0.18f, 0.18f, 1f);
        private static readonly Color SettingColor = new(0.5f, 0.55f, 0.95f, 1f);
        private static readonly Color StreamingColor = new(0.78f, 1f, 0f, 1f);

        static FolderCommentSync()
        {
            EditorApplication.delayCall += SyncDefaultComments;
        }

        [MenuItem("Anii Tools/Folder Comments/Sync Default Comments")]
        public static void SyncDefaultComments()
        {
            SetComment("Assets/ThirdParty", "◎ 第三方插件", "", ThirdPartyColor);
            SetComment("Assets/Scripts", "◆ 业务逻辑", "项目主要运行时代码和编辑器工具", ScriptColor);
            SetComment("Assets/Prefabs", "▣ 预制体资源", "UI 和通用预制体集中目录", PrefabColor);
            SetComment("Assets/Scenes", "◈ 场景文件", "项目场景和功能演示场景", SceneColor);
            SetComment("Assets/Resources", "◉ 运行时资源", "通过 Resources 直接加载的配置和资源", ResourceColor);
            SetComment("Assets/UI", "✦ 界面资源", "图集 字体 图片等 UI 资源", UiColor);
            SetComment("Assets/Art", "★ 美术资源", "模型 材质 纹理 音频和特效资源", ArtColor);
            SetComment("Assets/Tests", "▲ 测试目录", "编辑器测试和运行时测试", TestColor);
            SetComment("Assets/Settings", "▤ 工程设置", "项目内资源设置和配置文件", SettingColor);
            SetComment("Assets/StreamingAssets", "⬢ 外部文件", "构建后需要原样输出的资源", StreamingColor);

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
