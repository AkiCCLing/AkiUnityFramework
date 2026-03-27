using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Feif.UI
{
    public partial class MainPanelController
    {
        #region Auto Bind References
        #region Field Declarations
        [SerializeField] private TextMeshProUGUI txtMaintitle;
        [SerializeField] private Button btnExit;
        [SerializeField] private TextMeshProUGUI txtContent;
        #endregion

        private void BindReferences()
        {
            txtMaintitle = transform.Find("group_MainPanel/@txt_MainTitle").GetComponent<TextMeshProUGUI>();
            btnExit = transform.Find("group_MainPanel/@btn_Exit").GetComponent<Button>();
            txtContent = transform.Find("group_MainPanel/@txt_Content").GetComponent<TextMeshProUGUI>();
        }

        #endregion
    }
}
