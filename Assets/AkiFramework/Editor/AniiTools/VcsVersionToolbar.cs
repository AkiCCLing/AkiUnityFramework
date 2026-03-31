using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace AkiFramework.Editor
{
    /// <summary>
    /// 在 Unity 顶部工具栏显示版本信息。
    /// 默认插入到 Play、Pause、Step 左侧。
    /// </summary>
    [InitializeOnLoad]
    public static class VcsVersionToolbar
    {
        private const string ContainerName = "AniiTools-VcsVersion";
        private const string LeftZoneName = "ToolbarZoneLeftAlign";
        private const string ToolbarTypeName = "UnityEditor.Toolbar";
        private const string RootFieldName = "m_Root";
        private const string UnknownVersionText = "版本: --";

        private static readonly Color LatestColor = new(0f, 1f, 0f);
        private static readonly Color OutdatedColor = new(1f, 0.62f, 0.2f);
        private static readonly Color UnknownColor = new(0.7f, 0.7f, 0.7f);

        private static ScriptableObject _toolbar;
        private static Label _versionLabel;
        private static double _nextRefreshTime;
        private static VersionDisplayInfo _cachedDisplayInfo = VersionDisplayInfo.Unknown;

        static VcsVersionToolbar()
        {
            _cachedDisplayInfo = BuildVersionDisplayInfo();
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
            _versionLabel.style.color = LatestColor;
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
            var displayInfo = BuildVersionDisplayInfo();
            if (_versionLabel == null)
            {
                _cachedDisplayInfo = displayInfo;
                return;
            }

            if (_cachedDisplayInfo.Equals(displayInfo))
            {
                _versionLabel.text = _cachedDisplayInfo.Text;
                _versionLabel.tooltip = _cachedDisplayInfo.Tooltip;
                _versionLabel.style.color = _cachedDisplayInfo.Color;
                return;
            }

            _cachedDisplayInfo = displayInfo;
            _versionLabel.text = _cachedDisplayInfo.Text;
            _versionLabel.tooltip = _cachedDisplayInfo.Tooltip;
            _versionLabel.style.color = _cachedDisplayInfo.Color;
        }

        private static VersionDisplayInfo BuildVersionDisplayInfo()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            var gitInfo = GetGitVersionInfo(projectRoot);
            if (gitInfo != null)
            {
                return gitInfo;
            }

            var svnInfo = GetSvnVersionInfo(projectRoot);
            if (svnInfo != null)
            {
                return svnInfo;
            }

            return VersionDisplayInfo.Unknown;
        }

        private static VersionDisplayInfo GetGitVersionInfo(string workingDirectory)
        {
            if (!Directory.Exists(Path.Combine(workingDirectory, ".git")))
            {
                return null;
            }

            var branch = RunCommand("git", "rev-parse --abbrev-ref HEAD", workingDirectory);
            var localShortHash = RunCommand("git", "rev-parse --short HEAD", workingDirectory);
            var localFullHash = RunCommand("git", "rev-parse HEAD", workingDirectory);

            if (string.IsNullOrWhiteSpace(localShortHash) || string.IsNullOrWhiteSpace(localFullHash))
            {
                return null;
            }

            var upstreamRef = RunCommand("git", "rev-parse --abbrev-ref --symbolic-full-name @{u}", workingDirectory);
            var latestShortHash = localShortHash;
            var latestFullHash = localFullHash;
            var latestTimeRaw = RunCommand("git", "show -s --format=%cI HEAD", workingDirectory);
            var statusText = "已是最新";

            if (!string.IsNullOrWhiteSpace(upstreamRef))
            {
                latestShortHash = RunCommand("git", $"rev-parse --short {upstreamRef}", workingDirectory) ?? localShortHash;
                latestFullHash = RunCommand("git", $"rev-parse {upstreamRef}", workingDirectory) ?? localFullHash;
                latestTimeRaw = RunCommand("git", $"show -s --format=%cI {upstreamRef}", workingDirectory) ?? latestTimeRaw;
                statusText = DetermineGitStatus(localFullHash, latestFullHash, upstreamRef, workingDirectory);
            }

            var isBehindLatest = string.Equals(statusText, "落后于最新版本", StringComparison.Ordinal);
            var latestTimeText = FormatTimeText(latestTimeRaw);
            var branchText = string.IsNullOrWhiteSpace(branch) ? "Git" : $"Git {branch}";
            var text = $"{branchText} {localShortHash} / 最新 {latestShortHash}";

            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                text += $" / 更新时间 {latestTimeText}";
            }

            var tooltip = $"{statusText}\n当前版本: {localFullHash}\n最新版本: {latestFullHash}";
            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                tooltip += $"\n最新更新时间: {latestTimeText}";
            }

            return new VersionDisplayInfo(text, tooltip, isBehindLatest ? OutdatedColor : LatestColor);
        }

        private static string DetermineGitStatus(string localFullHash, string latestFullHash, string upstreamRef, string workingDirectory)
        {
            if (string.IsNullOrWhiteSpace(localFullHash) || string.IsNullOrWhiteSpace(latestFullHash))
            {
                return "版本状态未知";
            }

            if (string.Equals(localFullHash, latestFullHash, StringComparison.OrdinalIgnoreCase))
            {
                return "已是最新";
            }

            var mergeBase = RunCommand("git", $"merge-base HEAD {upstreamRef}", workingDirectory);
            if (string.Equals(mergeBase, localFullHash, StringComparison.OrdinalIgnoreCase))
            {
                return "落后于最新版本";
            }

            return "已包含更新";
        }

        private static VersionDisplayInfo GetSvnVersionInfo(string workingDirectory)
        {
            var revision = RunCommand("svn", "info --show-item revision", workingDirectory);
            if (string.IsNullOrWhiteSpace(revision))
            {
                return null;
            }

            var relativeUrl = RunCommand("svn", "info --show-item relative-url", workingDirectory);
            var branchText = GetSvnBranchText(relativeUrl);
            var latestRevision = RunCommand("svn", "info -r HEAD --show-item revision", workingDirectory) ?? revision;
            var latestTimeRaw = RunCommand("svn", "info -r HEAD --show-item last-changed-date", workingDirectory);
            var isBehindLatest = !string.Equals(revision, latestRevision, StringComparison.Ordinal);
            var latestTimeText = FormatTimeText(latestTimeRaw);

            var text = string.IsNullOrWhiteSpace(branchText)
                ? $"SVN r{revision} / 最新 r{latestRevision}"
                : $"SVN {branchText} r{revision} / 最新 r{latestRevision}";

            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                text += $" / 更新时间 {latestTimeText}";
            }

            var statusText = isBehindLatest ? "落后于最新版本" : "已是最新";
            var tooltip = $"{statusText}\n当前版本: r{revision}\n最新版本: r{latestRevision}";
            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                tooltip += $"\n最新更新时间: {latestTimeText}";
            }

            return new VersionDisplayInfo(text, tooltip, isBehindLatest ? OutdatedColor : LatestColor);
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

        private static string FormatTimeText(string rawTime)
        {
            if (string.IsNullOrWhiteSpace(rawTime))
            {
                return null;
            }

            if (DateTimeOffset.TryParse(rawTime, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedTime))
            {
                return parsedTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
            }

            return rawTime.Trim();
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
                process.StandardError.ReadToEnd();

                if (!process.WaitForExit(2000))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                    }

                    return null;
                }

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

        private sealed class VersionDisplayInfo : IEquatable<VersionDisplayInfo>
        {
            public static VersionDisplayInfo Unknown { get; } = new(UnknownVersionText, UnknownVersionText, UnknownColor);

            public VersionDisplayInfo(string text, string tooltip, Color color)
            {
                Text = text;
                Tooltip = tooltip;
                Color = color;
            }

            public string Text { get; }

            public string Tooltip { get; }

            public Color Color { get; }

            public bool Equals(VersionDisplayInfo other)
            {
                if (ReferenceEquals(other, null))
                {
                    return false;
                }

                return Text == other.Text && Tooltip == other.Tooltip && Color.Equals(other.Color);
            }

            public override bool Equals(object obj)
            {
                return obj is VersionDisplayInfo other && Equals(other);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(Text, Tooltip, Color);
            }
        }
    }
}
