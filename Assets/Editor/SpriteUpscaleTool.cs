using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-time utility: nearest-neighbor rescale Walk PNGs to Idle canvas size,
/// then reapply pixel-art sprite import settings (matching Idle PPU).
/// Overwrites Walk PNGs in place so existing sprite references keep working.
/// </summary>
public static class SpriteUpscaleTool
{
    const string IdleFolder = "Assets/Art/Sprites/Cat/Idle";
    const string WalkFolderPreferred = "Assets/Art/Sprites/Cat/Walk";
    const string WalkFolderFallback = "Assets/Art/Animations/Cat";

    [MenuItem("Tools/Sprite Utilities/Upscale Walk Sprites to Match Idle")]
    public static void UpscaleWalkSpritesToMatchIdle()
    {
        string idlePath = FindFirstPng(IdleFolder);
        if (string.IsNullOrEmpty(idlePath))
        {
            Debug.LogError($"[SpriteUpscaleTool] No PNG found in {IdleFolder}");
            return;
        }

        Texture2D idleTex = LoadPngFromDisk(idlePath);
        if (idleTex == null)
        {
            Debug.LogError($"[SpriteUpscaleTool] Failed to load idle reference: {idlePath}");
            return;
        }

        int targetW = idleTex.width;
        int targetH = idleTex.height;
        Object.DestroyImmediate(idleTex);

        float idlePpu = ReadSpritePpu(idlePath, 48f);

        string walkRoot = ResolveWalkRoot();
        if (string.IsNullOrEmpty(walkRoot))
        {
            Debug.LogError(
                $"[SpriteUpscaleTool] No Walk PNGs found in {WalkFolderPreferred} or {WalkFolderFallback}");
            return;
        }

        string[] walkPngs = Directory.GetFiles(walkRoot, "*.png", SearchOption.AllDirectories);
        if (walkPngs.Length == 0)
        {
            Debug.LogError($"[SpriteUpscaleTool] No PNGs under {walkRoot}");
            return;
        }

        int processed = 0;
        int skipped = 0;
        var scaleNotes = new HashSet<string>();

        foreach (string absolutePath in walkPngs)
        {
            string assetPath = ToAssetPath(absolutePath);
            if (string.IsNullOrEmpty(assetPath))
                continue;

            Texture2D source = LoadPngFromDisk(assetPath);
            if (source == null)
            {
                Debug.LogWarning($"[SpriteUpscaleTool] Could not load {assetPath}");
                continue;
            }

            int srcW = source.width;
            int srcH = source.height;

            if (srcW == targetW && srcH == targetH)
            {
                skipped++;
                Object.DestroyImmediate(source);
                continue;
            }

            float scaleX = (float)targetW / srcW;
            float scaleY = (float)targetH / srcH;
            scaleNotes.Add($"{srcW}x{srcH} → {targetW}x{targetH} (scaleX={scaleX:F4}, scaleY={scaleY:F4})");

            Texture2D scaled = ScalePointNearestNeighbor(source, targetW, targetH);
            Object.DestroyImmediate(source);

            byte[] png = scaled.EncodeToPNG();
            Object.DestroyImmediate(scaled);
            File.WriteAllBytes(Path.GetFullPath(assetPath), png);
            processed++;
        }

        AssetDatabase.Refresh();

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string absolutePath in walkPngs)
            {
                string assetPath = ToAssetPath(absolutePath);
                if (!string.IsNullOrEmpty(assetPath))
                    ApplyFinalImportSettings(assetPath, idlePpu);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        Debug.Log(
            $"[SpriteUpscaleTool] Done. Overwrote Walk PNGs in place under '{walkRoot}'.\n" +
            $"Idle reference: {idlePath} ({targetW}x{targetH}), Idle PPU={idlePpu}\n" +
            $"Resized: {processed}, Already matching size: {skipped}, Total PNGs: {walkPngs.Length}\n" +
            (scaleNotes.Count == 0
                ? "Scale factors used: (none — all already matched)"
                : "Scale factors used:\n - " + string.Join("\n - ", scaleNotes)));
    }

    static string ResolveWalkRoot()
    {
        if (Directory.Exists(WalkFolderPreferred) &&
            Directory.GetFiles(WalkFolderPreferred, "*.png", SearchOption.AllDirectories).Length > 0)
            return WalkFolderPreferred;

        if (Directory.Exists(WalkFolderFallback) &&
            Directory.GetFiles(WalkFolderFallback, "*.png", SearchOption.AllDirectories).Length > 0)
        {
            Debug.LogWarning(
                $"[SpriteUpscaleTool] '{WalkFolderPreferred}' not found or empty — using fallback '{WalkFolderFallback}'.");
            return WalkFolderFallback;
        }

        return null;
    }

    static string FindFirstPng(string folder)
    {
        if (!Directory.Exists(folder))
            return null;

        string[] files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
        if (files.Length == 0)
            files = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);

        if (files.Length == 0)
            return null;

        System.Array.Sort(files);
        return ToAssetPath(files[0]);
    }

    static string ToAssetPath(string path)
    {
        path = path.Replace('\\', '/');
        int idx = path.IndexOf("Assets/");
        if (idx < 0)
            return null;
        return path.Substring(idx);
    }

    static float ReadSpritePpu(string assetPath, float fallback)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        return importer != null ? importer.spritePixelsPerUnit : fallback;
    }

    static void ApplyFinalImportSettings(string assetPath, float idlePpu)
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
        importer.spritePixelsPerUnit = idlePpu;
        importer.isReadable = false;
        // Single mode replaces any multi-sprite sheet data; do not touch obsolete TextureImporter.spritesheet.

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Center;
        settings.spritePivot = new Vector2(0.5f, 0.5f);
        importer.SetTextureSettings(settings);

        var standalone = new TextureImporterPlatformSettings
        {
            name = "Standalone",
            overridden = false,
            maxTextureSize = 2048,
            textureCompression = TextureImporterCompression.Uncompressed
        };
        importer.SetPlatformTextureSettings(standalone);

        var def = new TextureImporterPlatformSettings
        {
            name = "DefaultTexturePlatform",
            overridden = false,
            maxTextureSize = 2048,
            textureCompression = TextureImporterCompression.Uncompressed
        };
        importer.SetPlatformTextureSettings(def);

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    static Texture2D LoadPngFromDisk(string assetPath)
    {
        string full = Path.GetFullPath(assetPath);
        if (!File.Exists(full))
            return null;

        byte[] bytes = File.ReadAllBytes(full);
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        if (!tex.LoadImage(bytes, false))
        {
            Object.DestroyImmediate(tex);
            return null;
        }

        return tex;
    }

    /// <summary>
    /// Nearest-neighbor resize (point sampling only — no bilinear).
    /// </summary>
    static Texture2D ScalePointNearestNeighbor(Texture2D source, int targetW, int targetH)
    {
        var result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        result.filterMode = FilterMode.Point;

        Color32[] src = source.GetPixels32();
        int srcW = source.width;
        int srcH = source.height;
        var dst = new Color32[targetW * targetH];

        for (int y = 0; y < targetH; y++)
        {
            int srcY = Mathf.Clamp(Mathf.FloorToInt((y + 0.5f) * srcH / (float)targetH), 0, srcH - 1);
            for (int x = 0; x < targetW; x++)
            {
                int srcX = Mathf.Clamp(Mathf.FloorToInt((x + 0.5f) * srcW / (float)targetW), 0, srcW - 1);
                dst[y * targetW + x] = src[srcY * srcW + srcX];
            }
        }

        result.SetPixels32(dst);
        result.Apply(false, false);
        return result;
    }
}
