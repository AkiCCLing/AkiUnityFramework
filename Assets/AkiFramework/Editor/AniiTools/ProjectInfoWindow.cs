using System.IO;
using UnityEditor;
using UnityEngine;

namespace AkiFramework.Editor
{
    public class ProjectInfoWindow : EditorWindow
    {
        private Vector2 _scrollPosition;
        private ProjectInfoSnapshot _snapshot;
        private double _nextAutoRefreshTime;

        [MenuItem("Anii Tools/工程信息面板")]
        public static void Open()
        {
            var window = GetWindow<ProjectInfoWindow>("工程信息");
            window.minSize = new Vector2(520f, 420f);
            window.RefreshSnapshot();
            window.Show();
        }

        [MenuItem("Anii Tools/版本控制/更新仓库")]
        public static void UpdateRepositoryMenu()
        {
            var projectRoot = GetProjectRoot();
            var result = ProjectInfoProvider.UpdateRepositoryNow(projectRoot);
            EditorUtility.DisplayDialog("更新仓库", result.Message, "确定");
        }

        private void OnEnable()
        {
            RefreshSnapshot();
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup < _nextAutoRefreshTime)
            {
                return;
            }

            _nextAutoRefreshTime = EditorApplication.timeSinceStartup + 3d;
            RefreshSnapshot();
            Repaint();
        }

        private void OnGUI()
        {
            _snapshot ??= ProjectInfoProvider.Collect();

            DrawToolbar();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            DrawBasicSection();
            EditorGUILayout.Space(8f);
            DrawVersionSection();
            EditorGUILayout.Space(8f);
            DrawRuntimeSection();
            EditorGUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("当前工程状态概览", EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(!ProjectInfoProvider.CanUpdateRepository(_snapshot?.ProjectRoot ?? GetProjectRoot())))
                {
                    if (GUILayout.Button("更新仓库", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                    {
                        UpdateRepository();
                    }
                }

                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    RefreshSnapshot();
                }
            }
        }

        private void DrawBasicSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("基础信息", EditorStyles.boldLabel);
                DrawSelectableRow("工程名称", _snapshot.ProductName);
                DrawSelectableRow("公司名称", _snapshot.CompanyName);
                DrawSelectableRow("应用版本", _snapshot.AppVersion);
                DrawSelectableRow("Unity 版本", _snapshot.UnityVersion);
                DrawSelectableRow("当前平台", _snapshot.BuildTarget);
                DrawSelectableRow("编辑器模式", _snapshot.EditorMode);
                DrawSelectableRow("工程路径", _snapshot.ProjectRoot);
            }
        }

        private void DrawVersionSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("版本信息", EditorStyles.boldLabel);

                var color = _snapshot.VcsInfo.IsBehindLatest ? new Color(1f, 0.62f, 0.2f) : new Color(0f, 1f, 0f);
                var originalColor = GUI.color;
                GUI.color = color;
                EditorGUILayout.HelpBox(_snapshot.VcsInfo.DisplayText, MessageType.None);
                GUI.color = originalColor;

                DrawSelectableRow("版本控制", _snapshot.VcsInfo.VcsType);
                DrawSelectableRow("分支/通道", _snapshot.VcsInfo.BranchOrChannel);
                DrawSelectableRow("当前版本", _snapshot.VcsInfo.CurrentVersion);
                DrawSelectableRow("最新版本", _snapshot.VcsInfo.LatestVersion);
                DrawSelectableRow("最新更新时间", _snapshot.VcsInfo.LatestUpdateTime);
                DrawSelectableRow("版本状态", _snapshot.VcsInfo.StatusText);
            }
        }

        private void DrawRuntimeSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("运行与语言", EditorStyles.boldLabel);
                DrawSelectableRow("当前语言", _snapshot.CurrentLanguage);
                DrawSelectableRow("系统语言", _snapshot.SystemLanguage);
                DrawSelectableRow("当前场景", string.IsNullOrWhiteSpace(_snapshot.ActiveScenePath) ? "--" : _snapshot.ActiveScenePath);
                DrawSelectableRow("场景保存状态", _snapshot.SceneSaveState);
            }
        }

        private void DrawSelectableRow(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(90f));
                EditorGUILayout.SelectableLabel(string.IsNullOrWhiteSpace(value) ? "--" : value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        private void RefreshSnapshot()
        {
            _snapshot = ProjectInfoProvider.Collect();
        }

        private void UpdateRepository()
        {
            var result = ProjectInfoProvider.UpdateRepositoryNow(_snapshot?.ProjectRoot ?? GetProjectRoot());
            RefreshSnapshot();
            Repaint();
            EditorUtility.DisplayDialog("更新仓库", result.Message, "确定");
        }

        private static string GetProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }
    }
}
