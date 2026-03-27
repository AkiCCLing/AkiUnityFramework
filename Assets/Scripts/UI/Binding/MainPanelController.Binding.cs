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
            txtMaintitle = transform.Find("@txt_MainTitle").GetComponent<TextMeshProUGUI>();
            btnExit = transform.Find("@btn_Exit").GetComponent<Button>();
            txtContent = transform.Find("@txt_Content").GetComponent<TextMeshProUGUI>();
        }

        #endregion
    }
}
