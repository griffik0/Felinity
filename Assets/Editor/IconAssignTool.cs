#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Matches ItemData assets to icon PNGs under Assets/Art/Sprites/Icons/.
/// </summary>
public static class IconAssignTool
{
    const string IconsFolder = "Assets/Art/Sprites/Icons";

    [MenuItem("Tools/Sprite Utilities/Auto-Assign Item Icons")]
    public static void AutoAssignItemIcons()
    {
        if (!AssetDatabase.IsValidFolder(IconsFolder))
        {
            Debug.LogWarning($"[IconAssignTool] Icons folder missing: {IconsFolder}");
            return;
        }

        var iconByKey = BuildIconLookup();
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemData");
        var matched = new List<string>();
        var unmatched = new List<string>();

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string guid in itemGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null)
                    continue;

                string displayName = string.IsNullOrWhiteSpace(item.itemName) ? item.name : item.itemName;
                string key = NormalizeKey(displayName);

                if (!iconByKey.TryGetValue(key, out Sprite icon) || icon == null)
                {
                    // Also try asset file name (e.g. WildBerry.asset → wildberry).
                    key = NormalizeKey(item.name);
                    iconByKey.TryGetValue(key, out icon);
                }

                if (icon == null)
                {
                    unmatched.Add($"{displayName} ({path})");
                    continue;
                }

                if (item.icon != icon)
                {
                    item.icon = icon;
                    EditorUtility.SetDirty(item);
                }

                matched.Add($"{displayName} ← {AssetDatabase.GetAssetPath(icon)}");
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
        }

        var report = new StringBuilder();
        report.AppendLine("[IconAssignTool] Auto-Assign Item Icons");
        report.AppendLine($"Icons folder: {IconsFolder}");
        report.AppendLine($"ItemData assets scanned: {itemGuids.Length}");
        report.AppendLine($"Matched: {matched.Count}");
        foreach (string line in matched)
            report.AppendLine($"  ✓ {line}");
        report.AppendLine($"Unmatched: {unmatched.Count}");
        foreach (string line in unmatched)
            report.AppendLine($"  ✗ {line}");

        // CatCoin is HUD-only (not ItemData) — note it explicitly.
        string catCoinHudPath = "Assets/Art/Sprites/Props/CatCoin.png";
        string catCoinIconPath = Path.Combine(IconsFolder, "CatCoin.png").Replace('\\', '/');
        var hudSprite = AssetDatabase.LoadAssetAtPath<Sprite>(catCoinHudPath);
        var iconsSprite = AssetDatabase.LoadAssetAtPath<Sprite>(catCoinIconPath);
        report.AppendLine();
        report.AppendLine("CatCoin HUD check:");
        report.AppendLine($"  Props/CatCoin.png present: {hudSprite != null} (used by CatCoinsUI)");
        report.AppendLine($"  Icons/CatCoin.png present: {iconsSprite != null} (not an ItemData — left alone)");
        report.AppendLine("  ItemData assign pass does not touch CatCoinsUI references.");

        Debug.Log(report.ToString());
    }

    static Dictionary<string, Sprite> BuildIconLookup()
    {
        var map = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { IconsFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                continue;

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                // Ensure Single sprite import, then retry.
                EnsureSpriteImport(path);
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            if (sprite == null)
                continue;

            string key = NormalizeKey(Path.GetFileNameWithoutExtension(path));
            map[key] = sprite;
        }

        return map;
    }

    static void EnsureSpriteImport(string assetPath)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var sb = new StringBuilder(value.Length);
        foreach (char c in value)
        {
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
#endif
