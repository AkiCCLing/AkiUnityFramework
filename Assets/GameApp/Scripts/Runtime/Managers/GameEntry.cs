using System;
using System.Threading.Tasks;
using AkiFramework;
using Feif.UIFramework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameApp
{
    /// <summary>
    /// 统一启动入口
    /// 负责初始化通用框架能力
    /// </summary>
    public static class GameEntry
    {
        private static bool _initialized;
        private static bool _uiFrameRegistered;
        private static bool _sceneRegistered;
        private static bool _autoHideUiOnSceneChanged;
        private static GameObject _stuckPanel;

        /// <summary>
        /// 完整启动流程
        /// 初始化完成后打开首个 UI
        /// </summary>
        public static async Task BootstrapAsync<T>(
            GameObject stuckPanel = null,
            bool autoHideUiOnSceneChanged = true) where T : UIBase
        {
            await InitializeAsync(stuckPanel, autoHideUiOnSceneChanged);
            await UIFrame.Show<T>();
        }

        /// <summary>
        /// 初始化框架入口
        /// </summary>
        public static async Task InitializeAsync(
            GameObject stuckPanel = null,
            bool autoHideUiOnSceneChanged = true)
        {
            UpdateStuckPanel(stuckPanel);
            _autoHideUiOnSceneChanged = autoHideUiOnSceneChanged;

            RegisterUIFrameEvents();
            RegisterSceneEvents();

            if (_initialized)
            {
                return;
            }

            await AssetManager.InitializeAsync();
            RuntimeDebugPanel.EnsureCreated();

            _initialized = true;
            Utils.LogSuccess("GameEntry", "框架初始化完成");
        }

        private static void RegisterUIFrameEvents()
        {
            if (_uiFrameRegistered)
            {
                return;
            }

            UIFrame.OnAssetRequest += HandleUIAssetRequest;
            UIFrame.OnAssetRelease += HandleUIAssetRelease;
            UIFrame.StuckTime = 0.5f;
            UIFrame.OnStuckStart += OnStuckStart;
            UIFrame.OnStuckEnd += OnStuckEnd;

            _uiFrameRegistered = true;
        }

        private static void RegisterSceneEvents()
        {
            if (_sceneRegistered)
            {
                return;
            }

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            _sceneRegistered = true;
        }

        private static void UpdateStuckPanel(GameObject stuckPanel)
        {
            if (stuckPanel == null)
            {
                return;
            }

            _stuckPanel = stuckPanel;
            _stuckPanel.SetActive(false);
        }

        private static Task<GameObject> HandleUIAssetRequest(Type type)
        {
            return AssetManager.LoadUIPrefabAsync(type);
        }

        private static void HandleUIAssetRelease(Type type)
        {
            AssetManager.ReleaseUIPrefab(type);
        }

        private static void OnStuckStart()
        {
            if (_stuckPanel != null)
            {
                _stuckPanel.SetActive(true);
            }
        }

        private static void OnStuckEnd()
        {
            if (_stuckPanel != null)
            {
                _stuckPanel.SetActive(false);
            }
        }

        private static async void OnActiveSceneChanged(Scene current, Scene next)
        {
            if (!_autoHideUiOnSceneChanged)
            {
                return;
            }

            await UIFrame.HideAll(true);
            Utils.LogInfo("Scene", $"场景切换完成: {current.name} -> {next.name}");
        }
    }
}
