using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Client.Editor
{
    /// <summary>
    /// 确保样例 UI prefab 注册到 Addressables
    /// </summary>
    [InitializeOnLoad]
    public static class SampleAddressablesSync
    {
        private const string SampleUiRoot = "Packages/com.akigame.framework/Samples/BasicDemo/Prefabs/UI";
        private const string PanelGroupName = "UIPanelController";
        private const string WindowGroupName = "UIWindowController";
        private const string ComponentGroupName = "UIComponentController";
        private const string CommonGroupName = "UICommonController";

        static SampleAddressablesSync()
        {
            EditorApplication.delayCall += Sync;
        }

        private static void Sync()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null || !AssetDatabase.IsValidFolder(SampleUiRoot))
            {
                return;
            }

            var panelGroup = settings.FindGroup(PanelGroupName);
            var windowGroup = settings.FindGroup(WindowGroupName);
            var componentGroup = settings.FindGroup(ComponentGroupName);
            var commonGroup = settings.FindGroup(CommonGroupName);

            if (panelGroup == null || windowGroup == null || componentGroup == null || commonGroup == null)
            {
                return;
            }

            var dirty = false;
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { SampleUiRoot });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var group = ResolveGroup(path, panelGroup, windowGroup, componentGroup, commonGroup);
                if (group == null)
                {
                    continue;
                }

                var entry = settings.FindAssetEntry(guid);
                var address = Path.GetFileNameWithoutExtension(path);
                if (entry != null && entry.parentGroup == group && entry.address == address)
                {
                    continue;
                }

                entry = settings.CreateOrMoveEntry(guid, group);
                entry.address = address;
                entry.SetLabel("UI", true);
                dirty = true;
            }

            if (!dirty)
            {
                return;
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Addressables] 样例 UI 已同步");
        }

        private static AddressableAssetGroup ResolveGroup(
            string path,
            AddressableAssetGroup panelGroup,
            AddressableAssetGroup windowGroup,
            AddressableAssetGroup componentGroup,
            AddressableAssetGroup commonGroup)
        {
            if (path.Contains("/Panels/"))
            {
                return panelGroup;
            }

            if (path.Contains("/Windows/"))
            {
                return windowGroup;
            }

            if (path.Contains("/Components/"))
            {
                return componentGroup;
            }

            if (path.Contains("/Common/"))
            {
                return commonGroup;
            }

            return null;
        }
    }
}
