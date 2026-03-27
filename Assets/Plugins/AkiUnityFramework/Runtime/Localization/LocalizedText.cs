using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Client
{
    /// <summary>
    /// TMP 多语言文本组件
    /// 挂载后可根据多语言 id 自动刷新 TextMeshProUGUI 文本
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private int localizationId;
        [SerializeField] private bool refreshOnEnable = true;

        private TextMeshProUGUI _text;

        /// <summary>
        /// 当前多语言 id
        /// </summary>
        public int LocalizationId
        {
            get => localizationId;
            set => localizationId = value;
        }

        private void Awake()
        {
            CacheComponent();
        }

        private void OnEnable()
        {
            CacheComponent();
            Signals.Get<ChangeLanguageSignal>().AddListener(OnLanguageChanged);

            if (refreshOnEnable)
            {
                RefreshText();
            }
        }

        private void OnDisable()
        {
            Signals.Get<ChangeLanguageSignal>().RemoveListener(OnLanguageChanged);
        }

        /// <summary>
        /// 按当前语言刷新文本
        /// </summary>
        public void RefreshText()
        {
            CacheComponent();
            if (_text == null)
            {
                return;
            }

            if (localizationId <= 0)
            {
                return;
            }

            _text.text = LocalizationManager.GetText(localizationId);
        }

        private void OnLanguageChanged(string _)
        {
            RefreshText();
        }

        private void CacheComponent()
        {
            if (_text == null)
            {
                _text = GetComponent<TextMeshProUGUI>();
            }
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Client.LocalizedText))]
public class LocalizedTextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            $"当前语言: {Client.LocalizationManager.CurrentLanguage}",
            MessageType.Info);

        if (GUILayout.Button("根据当前语言填充文本"))
        {
            var localizedText = (Client.LocalizedText)target;
            localizedText.RefreshText();

            var text = localizedText.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                EditorUtility.SetDirty(text);
            }

            EditorUtility.SetDirty(localizedText);
        }
    }
}
#endif
