using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AkiFramework.Editor
{
    [CreateAssetMenu(fileName = "UIFrameSetting", menuName = "UIFrame/UIFrameSetting", order = 0)]
    public class UIFrameSetting : ScriptableObject
    {
        public TextAsset UIBaseTemplate;
        public TextAsset UIComponentTemplate;
        public TextAsset UIPanelTemplate;
        public TextAsset UIWindowTemplate;
        public bool AutoReference = true;
        public string UIBaseScriptFolder = "Assets/GameApp/Scripts/UI/Core";
        public string UIComponentScriptFolder = "Assets/GameApp/Scripts/UI/Components";
        public string UIPanelScriptFolder = "Assets/GameApp/Scripts/UI/Panels";
        public string UIWindowScriptFolder = "Assets/GameApp/Scripts/UI/Windows";
        public string UIBindingScriptFolder = "Assets/GameApp/Scripts/UI/Binding";

        public static UIFrameSetting Instance
        {
            get
            {
                var guid = AssetDatabase.FindAssets("t:UIFrameSetting").FirstOrDefault();
                if (guid == null)
                {
                    var asset = CreateInstance<UIFrameSetting>();
                    EnsureFolderExists("Assets/Resources/UI/Config");
                    AssetDatabase.CreateAsset(asset, "Assets/Resources/UI/Config/UIFrameSetting.asset");
                    AssetDatabase.Refresh();
                    guid = AssetDatabase.FindAssets("t:UIFrameSetting").FirstOrDefault();
                }
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<UIFrameSetting>(path);
                return setting;
            }
        }

        private void Reset()
        {
            var ms = MonoScript.FromScriptableObject(this);
            var path = AssetDatabase.GetAssetPath(ms);
            var resPath = Path.GetDirectoryName(path).Replace("Scripts", "Resources");
            var fields = GetType().GetFields();
            foreach (var field in fields)
            {
                if (field.Name.EndsWith("Template"))
                {
                    var file = Path.Combine(resPath, $"{field.Name}.txt");
                    var res = AssetDatabase.LoadAssetAtPath<TextAsset>(file);
                    field.SetValue(this, res);
                }
            }
            EditorUtility.SetDirty(this);
        }

        private static void EnsureFolderExists(string folderPath)
        {
            var normalized = folderPath.Replace("\\", "/");
            var parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets") return;

            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }
    }
}
