using System.Collections.Generic;
using System.Linq;
using Client.Extensions;
using UnityEngine;

namespace AkiFramework.Editor
{
    public class TextMeshProCodeSnippetGenerator : CodeSnippetGenerator
    {
        public override int GetPriority()
        {
            return 125;
        }

        public override List<GameObject> GetGameObjects(GameObject prefab)
        {
            return prefab.transform.BreadthTraversal(t => t != prefab.transform && t.GetComponent<Client.UIFramework.UIBase>() != null)
                .Where(item => item.name.StartsWith("@txt_", System.StringComparison.OrdinalIgnoreCase))
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

