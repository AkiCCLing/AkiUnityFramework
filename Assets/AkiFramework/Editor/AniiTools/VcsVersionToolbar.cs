using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AkiFramework.Editor
{
    /// <summary>
    /// 在 Unity 顶部工具栏显示版本信息
    /// 默认插入到 Play、Pause、Step 左侧
    /// </summary>
    [InitializeOnLoad]
    public static class VcsVersionToolbar
    {
        private const string ContainerName = "AniiTools-VcsVersion";
        private const string LeftZoneName = "ToolbarZoneLeftAlign";
        private const string ToolbarTypeName = "UnityEditor.Toolbar";
        private const string RootFieldName = "m_Root";

        private static ScriptableObject _toolbar;
        private static Label _versionLabel;
        private static double _nextRefreshTime;
        private static string _cachedVersionText = "Version: --";

        static VcsVersionToolbar()
        {
            _cachedVersionText = BuildVersionText();
            EditorApplication.update += OnUpdate;
        }

        private static void OnUpdate()
        {
            if (_toolbar == null)
            {
                TryAttachToToolbar();
            }

            if (EditorApplication.timeSinceStartup < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = EditorApplication.timeSinceStartup + 10d;
            RefreshVersionLabel();
        }

        private static void TryAttachToToolbar()
        {
            var toolbarType = typeof(UnityEditor.Editor).Assembly.GetType(ToolbarTypeName);
            if (toolbarType == null)
            {
                return;
            }

            var toolbars = Resources.FindObjectsOfTypeAll(toolbarType);
            if (toolbars == null || toolbars.Length == 0)
            {
                return;
            }

            _toolbar = toolbars[0] as ScriptableObject;
            if (_toolbar == null)
            {
                return;
            }

            var rootField = toolbarType.GetField(RootFieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            var root = rootField?.GetValue(_toolbar) as VisualElement;
            if (root == null)
            {
                _toolbar = null;
                return;
            }

            var leftZone = root.Q(LeftZoneName);
            if (leftZone == null)
            {
                _toolbar = null;
                return;
            }

            var exists = leftZone.Q(ContainerName);
            if (exists != null)
            {
                _versionLabel = exists.Q<Label>();
                RefreshVersionLabel();
                return;
            }

            var container = new VisualElement
            {
                name = ContainerName
            };
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginLeft = 8;
            container.style.marginRight = 8;
            container.style.unityTextAlign = TextAnchor.MiddleLeft;

            _versionLabel = new Label();
            _versionLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            _versionLabel.style.color = new Color(0f, 1f, 0f);
            _versionLabel.style.fontSize = 12;
            _versionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _versionLabel.style.paddingLeft = 6;
            _versionLabel.style.paddingRight = 6;
            _versionLabel.style.borderLeftWidth = 1;
            _versionLabel.style.borderRightWidth = 1;
            _versionLabel.style.borderLeftColor = new Color(1f, 1f, 1f, 0.12f);
            _versionLabel.style.borderRightColor = new Color(1f, 1f, 1f, 0.12f);
            _versionLabel.pickingMode = PickingMode.Ignore;

            container.Add(_versionLabel);
            leftZone.Add(container);
            RefreshVersionLabel();
        }

        private static void RefreshVersionLabel()
        {
            var text = BuildVersionText();
            if (text == _cachedVersionText && _versionLabel != null)
            {
                _versionLabel.text = _cachedVersionText;
                return;
            }

            _cachedVersionText = text;
            if (_versionLabel != null)
            {
                _versionLabel.text = _cachedVersionText;
                _versionLabel.tooltip = _cachedVersionText;
            }
        }

        private static string BuildVersionText()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            var gitText = GetGitVersion(projectRoot);
            if (!string.IsNullOrEmpty(gitText))
            {
                return gitText;
            }

            var svnText = GetSvnVersion(projectRoot);
            if (!string.IsNullOrEmpty(svnText))
            {
                return svnText;
            }

            return "Version: --";
        }

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

            if (string.IsNullOrWhiteSpace(branch))
            {
                return $"Git {hash}";
            }

            return $"Git {branch} {hash}";
        }

        private static string GetSvnVersion(string workingDirectory)
        {
            var revision = RunCommand("svn", "info --show-item revision", workingDirectory);
            if (string.IsNullOrWhiteSpace(revision))
            {
                return null;
            }

            var relativeUrl = RunCommand("svn", "info --show-item relative-url", workingDirectory);
            var branchText = GetSvnBranchText(relativeUrl);

            if (string.IsNullOrWhiteSpace(branchText))
            {
                return $"SVN r{revision}";
            }

            return $"SVN {branchText} r{revision}";
        }

        private static string GetSvnBranchText(string relativeUrl)
        {
            if (string.IsNullOrWhiteSpace(relativeUrl))
            {
                return null;
            }

            var path = relativeUrl.Trim();
            if (path.StartsWith("^/"))
            {
                path = path.Substring(2);
            }

            path = path.Replace('\\', '/');
            if (path.StartsWith("trunk", StringComparison.OrdinalIgnoreCase))
            {
                return "主干";
            }

            if (!path.StartsWith("branches/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var branchName = path.Substring("branches/".Length);
            var slashIndex = branchName.IndexOf('/');
            if (slashIndex >= 0)
            {
                branchName = branchName.Substring(0, slashIndex);
            }

            if (string.IsNullOrWhiteSpace(branchName))
            {
                return "分支";
            }

            return $"分支 {branchName}";
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
            catch (Exception)
            {
                return null;
            }
        }
    }
}
