using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Feif.Extensions;
using System.Linq;

namespace AkiFramework.Editor
{
    public class UGUIImageCodeSnippetGenerator : CodeSnippetGenerator
    {
        public override int GetPriority()
        {
            return 120;
        }

        public override List<GameObject> GetGameObjects(GameObject prefab)
        {
            return prefab.transform.BreadthTraversal(t => t != prefab.transform && t.GetComponent<Feif.UIFramework.UIBase>() != null)
                .Where(item => item.GetComponent<Image>() != null && item.name.StartsWith("@"))
                .Select(item => item.gameObject)
                .ToList();
        }

        public override List<string> GenerateField(GameObject gameObject)
        {
            // 瀛楁澹版槑鐜板湪鐢?AutoBindCodeSnippetGenerator 缁熶竴鐢熸垚
            return null;
        }
    }
}
