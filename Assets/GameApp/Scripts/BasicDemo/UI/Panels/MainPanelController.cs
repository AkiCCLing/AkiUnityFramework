using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Feif.UIFramework;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameApp.UI
{
    public class MainPanelControllerData : UIData
    {
    }

    [PanelLayer]
    public partial class MainPanelController : UIComponent<MainPanelControllerData>
    {

        #region Override Functions
        protected override Task OnCreate()
        {
            return Task.CompletedTask;
        }

        protected override Task OnRefresh()
        {
            return Task.CompletedTask;
        }

        protected override void OnBind()
        {
            BindReferences();
        }

        protected override void OnUnbind()
        {
        }

        protected override void OnShow()
        {
        }

        protected override void OnHide()
        {
        }

        protected override void OnDied()
        {
        }
        #endregion

        #region Private Functions
        [UGUIButtonEvent("@btn_Exit")]
        protected void OnClickBtn_Exit()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        #endregion
    }
}
