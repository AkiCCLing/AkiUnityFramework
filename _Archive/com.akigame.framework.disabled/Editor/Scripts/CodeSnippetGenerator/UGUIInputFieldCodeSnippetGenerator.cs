using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Feif.Extensions;
using System.Linq;

namespace Feif.UIFramework.Editor
{
    public class UGUIInputFieldCodeSnippetGenerator : CodeSnippetGenerator
    {
        public override int GetPriority()
        {
            return 140;
        }

        public override List<GameObject> GetGameObjects(GameObject prefab)
        {
            return prefab.transform.BreadthTraversal(t => t != prefab.transform && t.GetComponent<UIBase>() != null)
                .Where(item => item.GetComponent<InputField>() != null && item.name.StartsWith("@"))
                .Select(item => item.gameObject)
                .ToList();
        }

        public override List<string> GenerateField(GameObject gameObject)
        {
            // 字段声明现在由 AutoBindCodeSnippetGenerator 统一生成
            return null;
        }
    }
}