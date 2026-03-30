using AkiFramework;
using TMPro;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameApp
{
    /// <summary>
    /// TMP 澶氳瑷€鏂囨湰缁勪欢
    /// 鎸傝浇鍚庡彲鏍规嵁澶氳瑷€ id 鑷姩鍒锋柊 TextMeshProUGUI 鏂囨湰
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField] private int localizationId;
        [SerializeField] private bool refreshOnEnable = true;

        private TextMeshProUGUI _text;

        /// <summary>
        /// 褰撳墠澶氳瑷€ id
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
        /// 鎸夊綋鍓嶈瑷€鍒锋柊鏂囨湰
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
[CustomEditor(typeof(GameApp.LocalizedText))]
public class LocalizedTextEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            $"褰撳墠璇█: {GameApp.LocalizationManager.CurrentLanguage}",
            MessageType.Info);

        if (GUILayout.Button("鏍规嵁褰撳墠璇█濉厖鏂囨湰"))
        {
            var localizedText = (GameApp.LocalizedText)target;
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

