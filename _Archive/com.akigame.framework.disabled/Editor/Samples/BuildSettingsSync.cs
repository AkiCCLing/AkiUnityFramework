using System.IO;
using System.Linq;
using UnityEditor;

namespace Client.Editor
{
    /// <summary>
    /// 确保样例场景加入 Build Settings
    /// </summary>
    [InitializeOnLoad]
    public static class BuildSettingsSync
    {
        private const string SampleStartScenePath = "Packages/com.akigame.framework/Samples/BasicDemo/Scenes/Start.unity";

        static BuildSettingsSync()
        {
            EditorApplication.delayCall += EnsureSampleSceneRegistered;
        }

        private static void EnsureSampleSceneRegistered()
        {
            if (EditorBuildSettings.scenes.Any(scene => scene.path == SampleStartScenePath))
            {
                return;
            }

            if (!File.Exists(SampleStartScenePath))
            {
                return;
            }

            var scenes = EditorBuildSettings.scenes
                .Where(scene => !string.Equals(scene.path, "Assets/Scenes/Start.unity"))
                .ToList();

            scenes.Insert(0, new EditorBuildSettingsScene(SampleStartScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
