using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using System.IO;

public static class AutoAddressablesUI
{
    private static readonly string[] UiRoots =
    {
        "Assets/Prefabs/UI"
    };

    private const string PANEL_GROUP = "UIPanelController";
    private const string WINDOW_GROUP = "UIWindowController";
    private const string COMPONENT_GROUP = "UIComponentController";
    private const string COMMON_GROUP = "UICommonController";

    [MenuItem("Anii Tools/Addressables/Auto Add UI Prefabs")]
    public static void AutoAddUIPrefabs()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found");
            return;
        }

        var panelGroup = GetOrCreateGroup(settings, PANEL_GROUP);
        var windowGroup = GetOrCreateGroup(settings, WINDOW_GROUP);
        var componentGroup = GetOrCreateGroup(settings, COMPONENT_GROUP);
        var commonGroup = GetOrCreateGroup(settings, COMMON_GROUP);

        var guids = AssetDatabase.FindAssets("t:Prefab", UiRoots);

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
                continue;

            var group = GetGroupByPath(path, panelGroup, windowGroup, componentGroup, commonGroup);
            if (group == null)
                continue;

            var entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = Path.GetFileNameWithoutExtension(path);
            entry.SetLabel("UI", true);

            Debug.Log($"[Addressables] Add [{group.Name}] {entry.address}");
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("UI Prefabs Auto Addressables Done");
    }

    private static AddressableAssetGroup GetGroupByPath(
        string path,
        AddressableAssetGroup panelGroup,
        AddressableAssetGroup windowGroup,
        AddressableAssetGroup componentGroup,
        AddressableAssetGroup commonGroup)
    {
        if (path.Contains("/Panels/"))
            return panelGroup;

        if (path.Contains("/Windows/"))
            return windowGroup;

        if (path.Contains("/Components/"))
            return componentGroup;

        if (path.Contains("/Common/"))
            return commonGroup;

        return null;
    }

    private static AddressableAssetGroup GetOrCreateGroup(
        AddressableAssetSettings settings,
        string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group != null)
            return group;

        group = settings.CreateGroup(
            groupName,
            false,
            false,
            false,
            new List<AddressableAssetGroupSchema>
            {
                settings.DefaultGroup.Schemas[0],
                settings.DefaultGroup.Schemas[1]
            }
        );

        Debug.Log($"[Addressables] Create Group: {groupName}");
        return group;
    }
}
