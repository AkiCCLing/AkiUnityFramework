using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using System.IO;

public static class AutoAddressablesTexture
{
    // Root directory to scan
    private const string TEXTURE_ROOT = "Assets/ArtRes/Textures";

    // Addressables group name
    private const string TEXTURE_GROUP = "Texture";

    [MenuItem("Anii Tools/Addressables/Auto Add Textures")]
    public static void AutoAddTextures()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("AddressableAssetSettings not found");
            return;
        }

        var group = GetOrCreateGroup(settings, TEXTURE_GROUP);

        // Find all Texture2D assets.
        var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { TEXTURE_ROOT });

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // Skip SpriteAtlas
            if (path.EndsWith(".spriteatlas"))
                continue;

            var entry = settings.CreateOrMoveEntry(guid, group);

            // Address = file name without extension
            entry.address = Path.GetFileNameWithoutExtension(path);

            entry.SetLabel("Texture", true);

            Debug.Log($"[Addressables] Add Texture: {entry.address}");
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("All Textures Auto Addressables Done");
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
