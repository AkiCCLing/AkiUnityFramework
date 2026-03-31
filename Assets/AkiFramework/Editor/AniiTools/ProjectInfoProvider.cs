using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AkiFramework.Editor
{
    internal static class ProjectInfoProvider
    {
        private const string LanguagePrefsKey = "GameApp.Language";

        public static ProjectInfoSnapshot Collect()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var activeScene = SceneManager.GetActiveScene();

            return new ProjectInfoSnapshot(
                projectRoot,
                Application.unityVersion,
                PlayerSettings.productName,
                PlayerSettings.companyName,
                PlayerSettings.bundleVersion,
                EditorUserBuildSettings.activeBuildTarget.ToString(),
                activeScene.path,
                GetSceneDirtyText(activeScene),
                GetCurrentLanguage(),
                Application.systemLanguage.ToString(),
                EditorApplication.isPlaying ? "运行中" : "编辑模式",
                GetVcsVersionInfo(projectRoot));
        }

        public static VcsVersionInfo GetVcsVersionInfo(string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                return VcsVersionInfo.Unknown;
            }

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

            return VcsVersionInfo.Unknown;
        }

        private static VcsVersionInfo GetGitVersionInfo(string workingDirectory)
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

            var latestTimeText = FormatTimeText(latestTimeRaw);
            var branchText = string.IsNullOrWhiteSpace(branch) ? "Git" : $"Git {branch}";
            var displayText = $"{branchText} {localShortHash} / 最新 {latestShortHash}";
            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                displayText += $" / 更新时间 {latestTimeText}";
            }

            var tooltip = $"{statusText}\n当前版本: {localFullHash}\n最新版本: {latestFullHash}";
            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                tooltip += $"\n最新更新时间: {latestTimeText}";
            }

            var isBehindLatest = string.Equals(statusText, "落后于最新版本", StringComparison.Ordinal);
            return new VcsVersionInfo(
                "Git",
                displayText,
                tooltip,
                branchText,
                localShortHash,
                latestShortHash,
                latestTimeText,
                statusText,
                isBehindLatest);
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

        private static VcsVersionInfo GetSvnVersionInfo(string workingDirectory)
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
            var latestTimeText = FormatTimeText(latestTimeRaw);
            var statusText = string.Equals(revision, latestRevision, StringComparison.Ordinal) ? "已是最新" : "落后于最新版本";

            var displayText = string.IsNullOrWhiteSpace(branchText)
                ? $"SVN r{revision} / 最新 r{latestRevision}"
                : $"SVN {branchText} r{revision} / 最新 r{latestRevision}";

            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                displayText += $" / 更新时间 {latestTimeText}";
            }

            var tooltip = $"{statusText}\n当前版本: r{revision}\n最新版本: r{latestRevision}";
            if (!string.IsNullOrWhiteSpace(latestTimeText))
            {
                tooltip += $"\n最新更新时间: {latestTimeText}";
            }

            return new VcsVersionInfo(
                "SVN",
                displayText,
                tooltip,
                string.IsNullOrWhiteSpace(branchText) ? "SVN" : $"SVN {branchText}",
                $"r{revision}",
                $"r{latestRevision}",
                latestTimeText,
                statusText,
                !string.Equals(revision, latestRevision, StringComparison.Ordinal));
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

            return string.IsNullOrWhiteSpace(branchName) ? "分支" : $"分支 {branchName}";
        }

        private static string GetCurrentLanguage()
        {
            var managerType = FindType("GameApp.LocalizationManager");
            var property = managerType?.GetProperty("CurrentLanguage", BindingFlags.Public | BindingFlags.Static);
            if (property?.GetValue(null) is string currentLanguage && !string.IsNullOrWhiteSpace(currentLanguage))
            {
                return currentLanguage;
            }

            return PlayerPrefs.GetString(LanguagePrefsKey, "zh_cn");
        }

        private static Type FindType(string fullTypeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullTypeName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static string GetSceneDirtyText(Scene scene)
        {
            if (!scene.IsValid())
            {
                return "未打开场景";
            }

            return EditorSceneManager.GetActiveScene().isDirty ? "未保存" : "已保存";
        }

        private static string FormatTimeText(string rawTime)
        {
            if (string.IsNullOrWhiteSpace(rawTime))
            {
                return "--";
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

                return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output) ? output : null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    internal sealed class ProjectInfoSnapshot
    {
        public ProjectInfoSnapshot(
            string projectRoot,
            string unityVersion,
            string productName,
            string companyName,
            string appVersion,
            string buildTarget,
            string activeScenePath,
            string sceneSaveState,
            string currentLanguage,
            string systemLanguage,
            string editorMode,
            VcsVersionInfo vcsInfo)
        {
            ProjectRoot = projectRoot;
            UnityVersion = unityVersion;
            ProductName = productName;
            CompanyName = companyName;
            AppVersion = appVersion;
            BuildTarget = buildTarget;
            ActiveScenePath = activeScenePath;
            SceneSaveState = sceneSaveState;
            CurrentLanguage = currentLanguage;
            SystemLanguage = systemLanguage;
            EditorMode = editorMode;
            VcsInfo = vcsInfo ?? VcsVersionInfo.Unknown;
        }

        public string ProjectRoot { get; }
        public string UnityVersion { get; }
        public string ProductName { get; }
        public string CompanyName { get; }
        public string AppVersion { get; }
        public string BuildTarget { get; }
        public string ActiveScenePath { get; }
        public string SceneSaveState { get; }
        public string CurrentLanguage { get; }
        public string SystemLanguage { get; }
        public string EditorMode { get; }
        public VcsVersionInfo VcsInfo { get; }
    }

    internal sealed class VcsVersionInfo
    {
        public static VcsVersionInfo Unknown { get; } = new("未知", "版本: --", "版本: --", "--", "--", "--", "--", "版本状态未知", false);

        public VcsVersionInfo(
            string vcsType,
            string displayText,
            string tooltip,
            string branchOrChannel,
            string currentVersion,
            string latestVersion,
            string latestUpdateTime,
            string statusText,
            bool isBehindLatest)
        {
            VcsType = vcsType;
            DisplayText = displayText;
            Tooltip = tooltip;
            BranchOrChannel = branchOrChannel;
            CurrentVersion = currentVersion;
            LatestVersion = latestVersion;
            LatestUpdateTime = latestUpdateTime;
            StatusText = statusText;
            IsBehindLatest = isBehindLatest;
        }

        public string VcsType { get; }
        public string DisplayText { get; }
        public string Tooltip { get; }
        public string BranchOrChannel { get; }
        public string CurrentVersion { get; }
        public string LatestVersion { get; }
        public string LatestUpdateTime { get; }
        public string StatusText { get; }
        public bool IsBehindLatest { get; }
    }
}
