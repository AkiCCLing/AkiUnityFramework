using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System;
using System.Collections;
using Client.UIFramework;
using UnityObject = UnityEngine.Object;
using Client.Extensions;

# if UNITY_2020_1_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace AkiFramework.Editor
{
    public static class UIAutoReference
    {
        [InitializeOnLoadMethod]
        public static void AddListeners()
        {
            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;
        }

        private static void OnPrefabSaving(GameObject prefab)
        {
            SetReferenceForPrefab(prefab);
        }

        public static void SetReferenceForPrefab(GameObject prefab)
        {
            var uibase = prefab.GetComponent<UIBase>();
            if (uibase == null) return;

            var guid = AssetDatabase.FindAssets("t:UIFrameSetting").FirstOrDefault();
            if (guid == null) return;
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var setting = AssetDatabase.LoadAssetAtPath<UIFrameSetting>(path);
            if (setting == null || setting.AutoReference == false) return;

            var uibases = prefab.transform.BreadthTraversal()
                 .Where(item => item.GetComponent<UIBase>() != null)
                 .Select(item => item.GetComponent<UIBase>());
            foreach (var item in uibases)
            {
                item.Parent = null;
                item.Children.Clear();
            }
            foreach (var item in uibases)
            {
                var parent = item.GetComponentsInParent<UIBase>().FirstOrDefault(p => p != item);
                if (parent == null) continue;
                item.Parent = parent;
                parent.Children.Add(item);
            }

            foreach (var item in uibase.transform.BreadthTraversal().Where(item => item.GetComponent<UIBase>() != null))
            {
                SetReference(item.GetComponent<UIBase>());
            }
        }

        public static List<string> SetReference(UnityObject script)
        {
            var result = new List<string>();
            var type = script.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                  .Where(item => Attribute.IsDefined(item, typeof(SerializeField)));
            Transform scriptTransform;
            foreach (var field in fields)
            {
                bool isGameObject = field.FieldType.IsEquivalentTo(typeof(GameObject));
                var fieldType = isGameObject ? typeof(Transform) : field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    var list = field.GetValue(script) as IList;

                    scriptTransform = (script as Component).transform;
                    var content = scriptTransform.BreadthTraversal(except: t => t != scriptTransform && t.GetComponent<UIBase>() != null)
                        .FirstOrDefault(item => NormalizeName(item.name) == NormalizeName(field.Name));

                    if (content == null) continue;
                    if (!content.name.StartsWith("@")) continue;

                    field.SetValue(script, Activator.CreateInstance(fieldType));
                    list = field.GetValue(script) as IList;
                    list.Clear();

                    var genericType = fieldType.GetGenericArguments().First();
                    result.Add(field.Name);
                    for (int i = 0; i < content.childCount; i++)
                    {
                        if (genericType.IsEquivalentTo(typeof(GameObject)))
                        {
                            list.Add(content.GetChild(i).gameObject);
                        }
                        else
                        {
                            list.Add(content.GetChild(i).GetComponent(genericType.Name));
                        }
                    }
                    continue;
                }
                if (!fieldType.IsSubclassOf(typeof(Component))) continue;

                scriptTransform = (script as Component).transform;
                var target = scriptTransform.BreadthTraversal(except: t => t != scriptTransform && t.GetComponent<UIBase>() != null)
                    .Where(item => item.GetComponent(fieldType) != null)
                    .Select(item => item.GetComponent(fieldType))
                    .FirstOrDefault(item =>
                    {
                        if (!item.name.StartsWith("@")) return false;
                        return NormalizeName(field.Name) == NormalizeName(item.name);
                    });

                if (target == null) continue;

                result.Add(field.Name);
                field.SetValue(script, isGameObject ? target.gameObject as UnityObject : target);
            }
            EditorUtility.SetDirty(script);
            return result;
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;

            var chars = value.TrimStart('@')
                .Where(char.IsLetterOrDigit)
                .Select(char.ToUpperInvariant)
                .ToArray();
            return new string(chars);
        }

        public static List<string> SetReference(string path)
        {
            var result = new List<string>();
            var paths = AssetDatabase.GetAllAssetPaths().Where(item => item.StartsWith(path) && item.EndsWith(".prefab"));
            foreach (var item in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(item);
                if (asset == null) continue;
                var uibase = asset.GetComponent<UIBase>();
                if (uibase == null) continue;
                SetReferenceForPrefab(asset);
                result.Add(item);
            }
            return result;
        }
    }
}
