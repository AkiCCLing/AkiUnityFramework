using System.Threading.Tasks;
using Feif.UI;
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
        [SerializeField] private GameObject stuckPanel;
        [SerializeField] private bool autoHideUiOnSceneChanged = true;
        [SerializeField] private string defaultLanguage = LocalizationManager.ZhCn;

        private async void Awake()
        {
            await BootstrapAsync();
        }

        private async Task BootstrapAsync()
        {
            await GameEntry.InitializeAsync(stuckPanel, autoHideUiOnSceneChanged);

            LubanManager.Initialize();
            LocalizationManager.LoadSavedLanguage(defaultLanguage);

            await UIFrame.Show<MainPanelController>();
        }
    }
}
