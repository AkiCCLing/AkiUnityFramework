using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using System;

namespace Feif.UIFramework.Editor
{
    [Serializable]
    internal class PendingPrefabScriptAttach
    {
        public string PrefabPath;
        public string ScriptTypeName;
    }

    public class TestEndAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
    {
        public override void Action(int instanceId, string path, string content)
        {
            var script = content;
            var fileName = Path.GetFileName(path);
            script = script.Replace("#SCRIPTNAME#", Path.GetFileNameWithoutExtension(fileName));
            script = Regex.Replace(script, " *#FIELDS#", "");
            script = Regex.Replace(script, " *#FUNCTIONS#", "");
            script = Regex.Replace(script, @"^ +\n", "\n", RegexOptions.Multiline);
            script = Regex.Replace(script, @"^ +\r\n", "\r\n", RegexOptions.Multiline);
            var currentDir = ReflactionUtils.RunClassFunc<string>(typeof(ProjectWindowUtil), "GetActiveFolderPath");
            File.WriteAllText(Path.Combine(Application.dataPath, "..", currentDir, fileName), script);
            AssetDatabase.Refresh();
        }
    }

    public static class UIScriptCreator
    {
        private const string PendingAttachKey = "UIFrame.PendingPrefabScriptAttach";

        public static Dictionary<string, List<string>> GetCodeSnippets(GameObject prefab)
        {
            var assembly = Assembly.GetAssembly(typeof(CodeSnippetGenerator));
            var generatorList = assembly.GetTypes()
                        .Where(item => item.IsSubclassOf(typeof(CodeSnippetGenerator)))
                        .Select(item => Activator.CreateInstance(item) as CodeSnippetGenerator)
                        .OrderByDescending(item => item.GetPriority());

            var finishedSet = new HashSet<GameObject>();

            var functionCodes = new List<string>();
            var fieldCodes = new List<string>();

            foreach (var generator in generatorList)
            {
                var objects = generator.GetGameObjects(prefab);
                foreach (var gameObject in objects)
                {
                    if (finishedSet.Contains(gameObject)) continue;

                    var fieldCode = generator.GenerateField(gameObject);
                    var functionCode = generator.GenerateFunction(gameObject);
                    if (fieldCode != null)
                    {
                        fieldCodes.AddRange(fieldCode);
                    }
                    if (functionCode != null)
                    {
                        functionCodes.AddRange(functionCode);
                    }
                    finishedSet.Add(gameObject);
                }
            }
            return new Dictionary<string, List<string>>()
            {
                {"Functions", functionCodes },
                {"Fields", fieldCodes }
            };
        }

        public static string AddCodeSnippetToTemplate(string template, string fileName, Dictionary<string, List<string>> codeSnippets)
        {
            var fieldIndent = Regex.Match(template, " *#FIELDS#").Value.Replace("#FIELDS#", "").Length;
            var functionIndent = Regex.Match(template, " *#FUNCTIONS#").Value.Replace("#FUNCTIONS#", "").Length;
            var fieldBuilder = new StringBuilder();
            var functionBuilder = new StringBuilder();
            foreach (var item in codeSnippets["Fields"])
            {
                fieldBuilder.AppendLine(new string(' ', fieldIndent) + item);
            }
            foreach (var item in codeSnippets["Functions"])
            {
                functionBuilder.AppendLine(new string(' ', functionIndent) + item);
            }
            var script = template;
            script = script.Replace("#SCRIPTNAME#", fileName);
            script = Regex.Replace(script, " *#FIELDS#", fieldBuilder.ToString());
            script = Regex.Replace(script, " *#FUNCTIONS#", functionBuilder.ToString().TrimEnd('\r', '\n'));
            script = Regex.Replace(script, @"^ +\n", "\n", RegexOptions.Multiline);
            script = Regex.Replace(script, @"^ +\r\n", "\r\n", RegexOptions.Multiline);
            return script;
        }

        public static void DoCreate(string fileName, string template)
        {
            if (Selection.objects.Length > 0)
            {
                var prefab = Selection.objects[0] as GameObject;
                if (prefab != null)
                {
                    var currentDir = ReflactionUtils.RunClassFunc<string>(typeof(ProjectWindowUtil), "GetActiveFolderPath");
                    var codeSnippets = GetCodeSnippets(prefab);
                    var result = AddCodeSnippetToTemplate(template, prefab.name, codeSnippets);
                    File.WriteAllText(Path.Combine(Application.dataPath, "..", currentDir, $"{prefab.name}.cs"), result);
                    
                    // 生成绑定partial脚本
                    GenerateBindingPartialScript(prefab, currentDir);
                    
                    // 等待脚本编译完成后添加组件到Prefab
                    var prefabPath = AssetDatabase.GetAssetPath(prefab);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        QueuePrefabAttach(prefabPath, $"Feif.UI.{prefab.name}");
                    }
                    
                    AssetDatabase.Refresh();
                    return;
                }
            }
            var projectBrowserIfExists = ReflactionUtils.RunClassFunc(typeof(ProjectWindowUtil), "GetProjectBrowserIfExists");
            if (projectBrowserIfExists != null)
            {
                TestEndAction endAction = ScriptableObject.CreateInstance<TestEndAction>();
                ReflactionUtils.RunInstanceFunc(projectBrowserIfExists, "Focus");
                ReflactionUtils.RunInstanceFunc(projectBrowserIfExists, "BeginPreimportedNameEditing", 0, endAction, fileName, EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D, template, true);
                ReflactionUtils.RunInstanceFunc(projectBrowserIfExists, "Repaint");
            }
        }

        /// <summary>
        /// 生成绑定partial脚本
        /// </summary>
        private static void GenerateBindingPartialScript(GameObject prefab, string currentDir)
        {
            var bindingCodes = AutoBindCodeSnippetGenerator.GenerateBindCode(prefab);
            if (bindingCodes.Count == 0)
                return;

            var className = prefab.name;
            var bindingScript = new StringBuilder();
            bindingScript.AppendLine("using UnityEngine;");
            bindingScript.AppendLine("using UnityEngine.UI;");
            bindingScript.AppendLine("using TMPro;");
            bindingScript.AppendLine();
            bindingScript.AppendLine("namespace Feif.UI");
            bindingScript.AppendLine("{");
            bindingScript.AppendLine($"    public partial class {className}");
            bindingScript.AppendLine("    {");
            bindingScript.AppendLine("        #region Auto Bind References");
            
            foreach (var line in bindingCodes)
            {
                if (line.StartsWith("#region") || line.StartsWith("#endregion"))
                {
                    bindingScript.AppendLine("        " + line);
                }
                else if (line.StartsWith("[SerializeField]"))
                {
                    bindingScript.AppendLine("        " + line);
                }
                else if (line == "{" || line == "}")
                {
                    bindingScript.AppendLine("        " + line);
                }
                else if (line.StartsWith("private void"))
                {
                    bindingScript.AppendLine("        " + line);
                }
                else if (line == "")
                {
                    bindingScript.AppendLine();
                }
                else
                {
                    bindingScript.AppendLine("            " + line);
                }
            }

            bindingScript.AppendLine("        #endregion");
            bindingScript.AppendLine("    }");
            bindingScript.AppendLine("}");

            var scriptPath = Path.Combine(Application.dataPath, "..", currentDir, $"{className}.Binding.cs");
            File.WriteAllText(scriptPath, bindingScript.ToString());
        }

        /// <summary>
        /// 重新生成选中Prefab的绑定脚本
        /// </summary>
        [MenuItem("Assets/UIFrame/重新生成绑定脚本", false, 1)]
        public static void RegenerateBindingScript()
        {
            if (Selection.objects.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先选择一个Prefab", "确定");
                return;
            }

            var prefab = Selection.objects[0] as GameObject;
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("提示", "请选择一个Prefab", "确定");
                return;
            }

            var currentDir = ReflactionUtils.RunClassFunc<string>(typeof(ProjectWindowUtil), "GetActiveFolderPath");
            GenerateBindingPartialScript(prefab, currentDir);
            
            // 确保脚本组件已添加到Prefab
            var prefabPath = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(prefabPath))
            {
                QueuePrefabAttach(prefabPath, $"Feif.UI.{prefab.name}");
            }
            
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("成功", $"已重新生成 {prefab.name}.Binding.cs 并更新组件", "确定");
        }

        [MenuItem("Assets/UIFrame/创建/UIBase", false, 10)]
        public static void CreateUIBase()
        {
            DoCreate("NewUIBase.cs", UIFrameSetting.Instance.UIBaseTemplate.text);
        }

        [MenuItem("Assets/UIFrame/创建/UIComponent", false, 10)]
        public static void CreateUIComponent()
        {
            DoCreate("NewUIComponent.cs", UIFrameSetting.Instance.UIComponentTemplate.text);
        }

        [MenuItem("Assets/UIFrame/创建/UIPanel", false, 10)]
        public static void CreateUIPanel()
        {
            DoCreate("NewUIPanel.cs", UIFrameSetting.Instance.UIPanelTemplate.text);
        }

        [MenuItem("Assets/UIFrame/创建/UIWindow", false, 10)]
        public static void CreateUIWindow()
        {
            DoCreate("NewUIWindow.cs", UIFrameSetting.Instance.UIWindowTemplate.text);
        }

        /// <summary>
        /// 将生成的脚本组件添加到Prefab根节点
        /// </summary>
        private static void AddScriptToPrefab(GameObject prefab, string scriptName)
        {
            var queuedPrefabPath = AssetDatabase.GetAssetPath(prefab);
            if (!string.IsNullOrEmpty(queuedPrefabPath))
            {
                QueuePrefabAttach(queuedPrefabPath, $"Feif.UI.{scriptName}");
            }
            return;
            // 多次尝试查找脚本类型，因为编译可能需要时间
            Type scriptType = null;
            string fullScriptName = $"Feif.UI.{scriptName}";
            
            // 首先尝试直接查找
            scriptType = Type.GetType(fullScriptName + ", Assembly-CSharp");
            
            // 如果找不到，尝试在所有程序集中查找
            if (scriptType == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    scriptType = assembly.GetType(fullScriptName);
                    if (scriptType != null)
                        break;
                }
            }
            
            // 如果还是找不到，尝试通过MonoScript查找
            if (scriptType == null)
            {
                string[] guids = AssetDatabase.FindAssets($"{scriptName} t:MonoScript");
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                    if (monoScript != null && monoScript.GetClass() != null && monoScript.GetClass().Name == scriptName)
                    {
                        scriptType = monoScript.GetClass();
                        break;
                    }
                }
            }

            if (scriptType == null)
            {
                Debug.LogWarning($"无法找到脚本类型: {fullScriptName}，将稍后重试...");
                // 如果还是找不到，延迟再次尝试
                EditorApplication.delayCall += () => AddScriptToPrefab(prefab, scriptName);
                return;
            }

            // 检查是否已经有这个组件
            var existingComponent = prefab.GetComponent(scriptType);
            if (existingComponent == null)
            {
                // 添加组件
                var component = prefab.AddComponent(scriptType);
                if (component != null)
                {
                    Debug.Log($"已将 {scriptName} 组件添加到 Prefab: {prefab.name}");
                    
                    // 标记Prefab为dirty以便保存
                    EditorUtility.SetDirty(prefab);
                    
                    // 保存Prefab
                    var prefabPath = AssetDatabase.GetAssetPath(prefab);
                    if (!string.IsNullOrEmpty(prefabPath))
                    {
                        AssetDatabase.SaveAssets();
                        Debug.Log($"Prefab 已保存: {prefabPath}");
                    }
                }
                else
                {
                    Debug.LogError($"无法将 {scriptName} 组件添加到 Prefab: {prefab.name}");
                }
            }
            else
            {
                Debug.Log($"Prefab {prefab.name} 已经包含 {scriptName} 组件");
            }
        }

        private static void QueuePrefabAttach(string prefabPath, string scriptTypeName)
        {
            var pending = new PendingPrefabScriptAttach()
            {
                PrefabPath = prefabPath,
                ScriptTypeName = scriptTypeName
            };
            SessionState.SetString(PendingAttachKey, JsonUtility.ToJson(pending));
        }

        private static Type FindType(string fullName)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly => assembly.GetType(fullName))
                .FirstOrDefault(type => type != null);
        }

        [DidReloadScripts]
        private static void AttachGeneratedScriptToPrefab()
        {
            var json = SessionState.GetString(PendingAttachKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return;

            SessionState.EraseString(PendingAttachKey);
            var pending = JsonUtility.FromJson<PendingPrefabScriptAttach>(json);
            if (pending == null || string.IsNullOrEmpty(pending.PrefabPath) || string.IsNullOrEmpty(pending.ScriptTypeName)) return;

            var componentType = FindType(pending.ScriptTypeName);
            if (componentType == null)
            {
                Debug.LogWarning($"鏃犳硶鎵惧埌鑴氭湰绫诲瀷: {pending.ScriptTypeName}");
                return;
            }

            var prefabRoot = PrefabUtility.LoadPrefabContents(pending.PrefabPath);
            try
            {
                if (prefabRoot.GetComponent(componentType) == null)
                {
                    prefabRoot.AddComponent(componentType);
                }

                UIAutoReference.SetReferenceForPrefab(prefabRoot);
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, pending.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }

            AssetDatabase.Refresh();
        }
    }
}
