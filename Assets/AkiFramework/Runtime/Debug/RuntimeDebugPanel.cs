using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Feif.UIFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AkiFramework
{
    /// <summary>
    /// 运行时调试工具面板
    /// 用于查看框架状态和执行常用调试操作
    /// </summary>
    public class RuntimeDebugPanel : MonoBehaviour
    {
        private const string SampleLocalizationManagerTypeName = "GameApp.LocalizationManager";

        private static RuntimeDebugPanel _instance;
        private static readonly List<Type> UiTypes = new();

        private readonly Vector2 _windowSize = new(1200f, 860f);
        private Rect _windowRect = new(30f, 20f, 1200f, 860f);
        private Vector2 _openedUiScroll;
        private Vector2 _panelListScroll;
        private bool _visible;
        private string _versionText = string.Empty;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        public static void EnsureCreated()
        {
            if (!ShouldCreate() || _instance != null)
            {
                return;
            }

            var panelObject = new GameObject(nameof(RuntimeDebugPanel));
            DontDestroyOnLoad(panelObject);
            _instance = panelObject.AddComponent<RuntimeDebugPanel>();
        }

        private static bool ShouldCreate()
        {
#if UNITY_EDITOR
            return true;
#else
            return Debug.isDebugBuild;
#endif
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _versionText = BuildVersionText();
            CacheUiTypes();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F10))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible)
            {
                return;
            }

            EnsureStyles();
            EnsureWindowInScreen();
            _windowRect = GUILayout.Window(GetInstanceID(), _windowRect, DrawWindow, "Runtime Debug", _windowStyle);
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.Space(8f);
            GUILayout.Label($"当前语言: {GetCurrentLanguageLabel()}", _labelStyle);
            GUILayout.Label($"当前场景: {SceneManager.GetActiveScene().name}", _labelStyle);
            GUILayout.Label($"版本信息: {_versionText}", _labelStyle);

            GUILayout.Space(16f);
            GUILayout.Label("语言切换", _labelStyle);
            GUILayout.BeginHorizontal();
            using (new GUIEnabledScope(CanSwitchLanguage()))
            {
                if (GUILayout.Button("中文", _buttonStyle, GUILayout.Height(48f)))
                {
                    SetCurrentLanguage("zh_cn");
                }

                if (GUILayout.Button("English", _buttonStyle, GUILayout.Height(48f)))
                {
                    SetCurrentLanguage("en");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16f);
            GUILayout.Label("当前打开的 UI", _labelStyle);
            _openedUiScroll = GUILayout.BeginScrollView(_openedUiScroll, GUILayout.Height(260f));
            foreach (var ui in GetOpenedUiNames())
            {
                GUILayout.Label(ui, _labelStyle);
            }
            GUILayout.EndScrollView();

            GUILayout.Space(16f);
            GUILayout.Label("快捷操作", _labelStyle);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("清缓存", _buttonStyle, GUILayout.Height(48f)))
            {
                _ = ClearCacheAsync();
            }

            if (GUILayout.Button("刷新版本", _buttonStyle, GUILayout.Height(48f)))
            {
                _versionText = BuildVersionText();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(16f);
            GUILayout.Label("快速打开面板", _labelStyle);
            _panelListScroll = GUILayout.BeginScrollView(_panelListScroll, GUILayout.Height(360f));
            foreach (var uiType in UiTypes)
            {
                if (GUILayout.Button(uiType.Name, _buttonStyle, GUILayout.Height(44f)))
                {
                    _ = OpenUiAsync(uiType);
                }
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("关闭调试面板", _buttonStyle, GUILayout.Height(52f)))
            {
                _visible = false;
            }

            GUI.DragWindow(new Rect(0f, 0f, _windowSize.x, 24f));
        }

        private void EnsureStyles()
        {
            if (_windowStyle != null && _labelStyle != null && _buttonStyle != null)
            {
                return;
            }

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            _windowStyle.padding = new RectOffset(18, 18, 40, 18);

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
        }

        private void EnsureWindowInScreen()
        {
            var targetWidth = Mathf.Min(Screen.width - 40f, _windowSize.x);
            var targetHeight = Mathf.Min(Screen.height - 40f, _windowSize.y);

            _windowRect.width = targetWidth;
            _windowRect.height = targetHeight;
            _windowRect.x = Mathf.Clamp(_windowRect.x, 20f, Mathf.Max(20f, Screen.width - _windowRect.width - 20f));
            _windowRect.y = Mathf.Clamp(_windowRect.y, 20f, Mathf.Max(20f, Screen.height - _windowRect.height - 20f));
        }

        private static IEnumerable<string> GetOpenedUiNames()
        {
            return UIFrame.GetAll()
                .Where(ui => ui != null && ui.gameObject.activeInHierarchy)
                .Select(ui => ui.GetType().Name)
                .DefaultIfEmpty("无");
        }

        private static void CacheUiTypes()
        {
            UiTypes.Clear();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types.Where(type => type != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract)
                    {
                        continue;
                    }

                    if (!typeof(UIBase).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (UIFrame.GetLayer(type) == null)
                    {
                        continue;
                    }

                    UiTypes.Add(type);
                }
            }

            UiTypes.Sort((left, right) => string.Compare(left.Name, right.Name, StringComparison.Ordinal));
        }

        private static async Task OpenUiAsync(Type uiType)
        {
            if (uiType == null)
            {
                return;
            }

            await UIFrame.Show(uiType);
        }

        private static async Task ClearCacheAsync()
        {
            await UIFrame.HideAll(true);
            AssetManager.ReleaseAll();
            Resources.UnloadUnusedAssets();
            GC.Collect();
            Utils.LogSuccess("Debug", "缓存已清理");
        }

        private static string BuildVersionText()
        {
            var appVersion = string.IsNullOrWhiteSpace(Application.version) ? "0.0.0" : Application.version;

#if UNITY_EDITOR
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var vcsVersion = GetGitVersion(projectRoot);
            if (string.IsNullOrWhiteSpace(vcsVersion))
            {
                vcsVersion = GetSvnVersion(projectRoot);
            }

            if (!string.IsNullOrWhiteSpace(vcsVersion))
            {
                return $"{appVersion} | {vcsVersion}";
            }
#endif

            return appVersion;
        }

        private static bool CanSwitchLanguage()
        {
            return ResolveSampleLocalizationManager() != null;
        }

        private static string GetCurrentLanguageLabel()
        {
            var managerType = ResolveSampleLocalizationManager();
            if (managerType == null)
            {
                return "未接入";
            }

            var property = managerType.GetProperty("CurrentLanguage", BindingFlags.Public | BindingFlags.Static);
            var value = property?.GetValue(null) as string;
            return string.IsNullOrWhiteSpace(value) ? "未知" : value;
        }

        private static void SetCurrentLanguage(string language)
        {
            var managerType = ResolveSampleLocalizationManager();
            var method = managerType?.GetMethod("SetLanguage", BindingFlags.Public | BindingFlags.Static);
            method?.Invoke(null, new object[] { language, true });
        }

        private static Type ResolveSampleLocalizationManager()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(SampleLocalizationManagerTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

#if UNITY_EDITOR
        private static string GetGitVersion(string workingDirectory)
        {
            if (!Directory.Exists(Path.Combine(workingDirectory, ".git")))
            {
                return null;
            }

            var branch = RunCommand("git", "rev-parse --abbrev-ref HEAD", workingDirectory);
            var hash = RunCommand("git", "rev-parse --short HEAD", workingDirectory);
            if (string.IsNullOrWhiteSpace(hash))
            {
                return null;
            }

            return string.IsNullOrWhiteSpace(branch) ? $"Git {hash}" : $"Git {branch} {hash}";
        }

        private static string GetSvnVersion(string workingDirectory)
        {
            var revision = RunCommand("svn", "info --show-item revision", workingDirectory);
            if (string.IsNullOrWhiteSpace(revision))
            {
                return null;
            }

            var relativeUrl = RunCommand("svn", "info --show-item relative-url", workingDirectory);
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return $"SVN r{revision}";
            }

            var path = relativeUrl.Replace("^/", string.Empty).Replace('\\', '/');
            if (path.StartsWith("trunk", StringComparison.OrdinalIgnoreCase))
            {
                return $"SVN 主干 r{revision}";
            }

            if (path.StartsWith("branches/", StringComparison.OrdinalIgnoreCase))
            {
                var branchName = path.Substring("branches/".Length);
                var slashIndex = branchName.IndexOf('/');
                if (slashIndex >= 0)
                {
                    branchName = branchName.Substring(0, slashIndex);
                }

                return string.IsNullOrWhiteSpace(branchName)
                    ? $"SVN 分支 r{revision}"
                    : $"SVN 分支 {branchName} r{revision}";
            }

            return $"SVN r{revision}";
        }

        private static string RunCommand(string fileName, string arguments, string workingDirectory)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd().Trim();
                process.WaitForExit(2000);

                if (process.ExitCode != 0)
                {
                    return null;
                }

                return string.IsNullOrWhiteSpace(output) ? null : output;
            }
            catch
            {
                return null;
            }
        }
#endif

        private readonly struct GUIEnabledScope : IDisposable
        {
            private readonly bool _previousState;

            public GUIEnabledScope(bool enabled)
            {
                _previousState = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = _previousState;
            }
        }
    }
}
