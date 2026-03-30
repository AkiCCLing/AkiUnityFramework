using System.Collections.Generic;
using System.Linq;
using Feif.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace AkiFramework.Editor
{
    /// <summary>
    /// 自动生成partial脚本用于绑定UI组件
    /// </summary>
    public class AutoBindCodeSnippetGenerator
    {
        public static List<string> GenerateBindCode(GameObject prefab)
        {
            var bindCodes = new List<string>();
            var allTransforms = prefab.transform.BreadthTraversal()
                .Where(t => t.gameObject.name.StartsWith("@"))
                .ToList();

            if (allTransforms.Count == 0)
            {
                return bindCodes;
            }

            // 生成字段声明
            bindCodes.Add("#region Field Declarations");
            foreach (var transform in allTransforms)
            {
                var name = transform.gameObject.name;
                var fieldName = ExtractFieldName(name);
                var fieldDeclaration = GenerateFieldDeclaration(transform.gameObject);
                if (!string.IsNullOrEmpty(fieldDeclaration))
                {
                    bindCodes.Add(fieldDeclaration);
                }
            }
            bindCodes.Add("#endregion");
            bindCodes.Add("");

            bindCodes.Add("private void BindReferences()");
            bindCodes.Add("{");

            foreach (var transform in allTransforms)
            {
                var name = transform.gameObject.name;
                var fieldName = ExtractFieldName(name);
                var relativePath = GetRelativePath(prefab.transform, transform);

                // Text -> TextMeshProUGUI
                if (name.StartsWith("@txt_", System.StringComparison.OrdinalIgnoreCase))
                {
                    bindCodes.Add($"{fieldName} = transform.Find(\"{relativePath}\").GetComponent<TextMeshProUGUI>();");
                    continue;
                }

                // Image
                if (name.StartsWith("@img_", System.StringComparison.OrdinalIgnoreCase))
                {
                    bindCodes.Add($"{fieldName} = transform.Find(\"{relativePath}\").GetComponent<Image>();");
                    continue;
                }

                // Button
                if (name.StartsWith("@btn_", System.StringComparison.OrdinalIgnoreCase))
                {
                    bindCodes.Add($"{fieldName} = transform.Find(\"{relativePath}\").GetComponent<Button>();");
                    continue;
                }

                // InputField
                if (name.StartsWith("@input_", System.StringComparison.OrdinalIgnoreCase))
                {
                    bindCodes.Add($"{fieldName} = transform.Find(\"{relativePath}\").GetComponent<InputField>();");
                    continue;
                }

                // RawImage
                if (name.StartsWith("@raw_", System.StringComparison.OrdinalIgnoreCase))
                {
                    bindCodes.Add($"{fieldName} = transform.Find(\"{relativePath}\").GetComponent<RawImage>();");
                    continue;
                }

                // Generic GameObject
                if (name.StartsWith("@", System.StringComparison.OrdinalIgnoreCase))
                {
                    bindCodes.Add($"{fieldName} = transform.Find(\"{relativePath}\").gameObject;");
                }
            }

            bindCodes.Add("}");
            bindCodes.Add("");

            return bindCodes;
        }

        private static string GenerateFieldDeclaration(GameObject gameObject)
        {
            var name = gameObject.name;
            var fieldName = ExtractFieldName(name);

            // Text -> TextMeshProUGUI
            if (name.StartsWith("@txt_", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"[SerializeField] private TextMeshProUGUI {fieldName};";
            }

            // Image
            if (name.StartsWith("@img_", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"[SerializeField] private Image {fieldName};";
            }

            // Button
            if (name.StartsWith("@btn_", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"[SerializeField] private Button {fieldName};";
            }

            // InputField
            if (name.StartsWith("@input_", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"[SerializeField] private InputField {fieldName};";
            }

            // RawImage
            if (name.StartsWith("@raw_", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"[SerializeField] private RawImage {fieldName};";
            }

            // Generic GameObject
            if (name.StartsWith("@", System.StringComparison.OrdinalIgnoreCase))
            {
                return $"[SerializeField] private GameObject {fieldName};";
            }

            return null;
        }

        private static string GetRelativePath(Transform root, Transform target)
        {
            if (root == null || target == null)
            {
                return string.Empty;
            }

            if (root == target)
            {
                return target.name;
            }

            var pathParts = new List<string>();
            var current = target;
            while (current != null && current != root)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }

            pathParts.Reverse();
            return string.Join("/", pathParts);
        }

        private static string ExtractFieldName(string goName)
        {
            var name = goName.Replace("@", "");
            
            // 处理驼峰命名：移除下划线并将下划线后的首字母大写
            var parts = name.Split('_');
            if (parts.Length > 1)
            {
                // 第一部分保持原样（通常是前缀，如 txt, img, btn）
                var result = parts[0].ToLower();
                for (int i = 1; i < parts.Length; i++)
                {
                    if (parts[i].Length > 0)
                    {
                        result += parts[i].Substring(0, 1).ToUpper() + parts[i].Substring(1).ToLower();
                    }
                }
                return result;
            }
            else
            {
                // 没有下划线，直接首字母小写
                return name.Substring(0, 1).ToLower() + name.Substring(1);
            }
        }
    }
}


