using System.Collections.Generic;
using System.Linq;
using Client.Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace AkiFramework.Editor
{
    public class UIBaseCodeSnippetGenerator : CodeSnippetGenerator
    {
        public override int GetPriority()
        {
            return 160;
        }

        public override List<GameObject> GetGameObjects(GameObject prefab)
        {
            return prefab.transform.BreadthTraversal(t => t != prefab.transform && t.GetComponent<Client.UIFramework.UIBase>() != null)
                .Where(item => item.GetComponent<Client.UIFramework.UIBase>() != null && item.name.StartsWith("@"))
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
