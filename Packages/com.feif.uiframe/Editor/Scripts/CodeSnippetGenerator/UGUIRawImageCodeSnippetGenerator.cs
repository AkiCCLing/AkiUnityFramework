using System.Collections.Generic;
using System.Linq;
using Feif.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace Feif.UIFramework.Editor
{
    public class UGUIRawImageCodeSnippetGenerator : CodeSnippetGenerator
    {
        public override int GetPriority()
        {
            return 100;
        }

        public override List<GameObject> GetGameObjects(GameObject prefab)
        {
            return prefab.transform.BreadthTraversal(t => t != prefab.transform && t.GetComponent<UIBase>() != null)
                .Where(item => item.GetComponent<RawImage>() != null && item.name.StartsWith("@"))
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