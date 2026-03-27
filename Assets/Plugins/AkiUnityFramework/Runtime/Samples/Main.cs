using System;
using System.Threading.Tasks;
using Feif.UIFramework;
using UnityEngine;

namespace Client
{
    /// <summary>
    /// BasicDemo 启动入口
    /// 负责接入样例自己的数据和多语言初始化
    /// </summary>
    public class Main : MonoBehaviour
    {
        private const string DefaultMainPanelTypeName = "Feif.UI.MainPanelController";

        [SerializeField] private GameObject stuckPanel;
        [SerializeField] private bool autoHideUiOnSceneChanged = true;
        [SerializeField] private string defaultLanguage = LocalizationManager.ZhCn;
        [SerializeField] private string mainPanelTypeName = DefaultMainPanelTypeName;

        private async void Awake()
        {
            await BootstrapAsync();
        }

        private async Task BootstrapAsync()
        {
            await GameEntry.InitializeAsync(stuckPanel, autoHideUiOnSceneChanged);

            LubanManager.Initialize();
            LocalizationManager.LoadSavedLanguage(defaultLanguage);

            var mainPanelType = ResolveMainPanelType();
            if (mainPanelType == null)
            {
                Utils.LogError("Sample", $"未找到样例面板类型: {mainPanelTypeName}");
                return;
            }

            await UIFrame.Show(mainPanelType);
        }

        private Type ResolveMainPanelType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var resolvedType = assembly.GetType(mainPanelTypeName);
                if (resolvedType != null)
                {
                    return resolvedType;
                }
            }

            return null;
        }
    }
}
