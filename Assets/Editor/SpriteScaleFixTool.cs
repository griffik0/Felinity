#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Per-direction walk sprite fix: match each direction's idle canvas (and content scale),
/// write "_fixed" copies for review (does not overwrite originals).
/// </summary>
public static class SpriteScaleFixTool
{
    const string IdleFolder = "Assets/Art/Sprites/Cat/Idle";
    const string WalkFolderPreferred = "Assets/Art/Sprites/Cat/Walk";
    const string WalkFolderFallback = "Assets/Art/Animations/Cat";

    struct DirMap
    {
        public string Key;
        public string IdleFile;
        public string WalkFolderName;
    }

    static readonly DirMap[] Directions =
    {
        new DirMap { Key = "N",  IdleFile = "cat_idle_n.png",  WalkFolderName = "walk_n" },
        new DirMap { Key = "S",  IdleFile = "cat_idle_s.png",  WalkFolderName = "walk_s" },
        new DirMap { Key = "E",  IdleFile = "cat_idle_e.png",  WalkFolderName = "walk_e" },
        new DirMap { Key = "W",  IdleFile = "cat_idle_w.png",  WalkFolderName = "walk_w" },
        new DirMap { Key = "NE", IdleFile = "cat_idle_ne.png", WalkFolderName = "walk_ne" },
        new DirMap { Key = "NW", IdleFile = "cat_idle_nw.png", WalkFolderName = "walk_nw" },
        new DirMap { Key = "SE", IdleFile = "cat_idle_se.png", WalkFolderName = "walk_se" },
        new DirMap { Key = "SW", IdleFile = "cat_idle_sw.png", WalkFolderName = "walk_sw" },
    };

    [MenuItem("Tools/Sprite Utilities/Fix Per-Direction Walk Scaling")]
    public static void FixPerDirectionWalkScaling()
    {
        string walkRoot = ResolveWalkRoot();
        if (string.IsNullOrEmpty(walkRoot))
        {
            Debug.LogError(
                $"[SpriteScaleFixTool] No Walk PNGs found in {WalkFolderPreferred} or {WalkFolderFallback}");
            return;
        }

        string sampleIdle = Path.Combine(IdleFolder, "cat_idle_s.png").Replace('\\', '/');
        float idlePpu = ReadSpritePpu(sampleIdle, 48f);

        var report = new StringBuilder();
        report.AppendLine("[SpriteScaleFixTool] Per-direction walk scale fix");
        report.AppendLine($"Walk root: {walkRoot}");
        report.AppendLine($"Idle PPU (from import settings): {idlePpu}");
        report.AppendLine("Writes *_fixed.png copies only (originals untouched).");
        report.AppendLine();

        var fixedAssetPaths = new List<string>();
        int totalCorrected = 0;
        int totalSkipped = 0;

        foreach (DirMap dir in Directions)
        {
            string idlePath = Path.Combine(IdleFolder, dir.IdleFile).Replace('\\', '/');
            if (!File.Exists(Path.GetFullPath(idlePath)))
            {
                report.AppendLine($"{dir.Key}: MISSING idle '{idlePath}'");
                continue;
            }

            Texture2D idleTex = LoadPngFromDisk(idlePath);
            if (idleTex == null)
            {
                report.AppendLine($"{dir.Key}: FAILED to load idle");
                continue;
            }

            int targetW = idleTex.width;
            int targetH = idleTex.height;
            RectInt idleContent = MeasureOpaqueBounds(idleTex);
            UnityEngine.Object.DestroyImmediate(idleTex);

            string walkDir = Path.Combine(walkRoot, dir.WalkFolderName).Replace('\\', '/');
            if (!Directory.Exists(walkDir))
            {
                report.AppendLine(
                    $"{dir.Key}: idle={targetW}x{targetH} content={idleContent.width}x{idleContent.height} — " +
                    $"MISSING walk folder '{walkDir}'");
                continue;
            }

            string[] walkPngs = Directory.GetFiles(walkDir, "*.png", SearchOption.TopDirectoryOnly);
            Array.Sort(walkPngs);

            int corrected = 0;
            int skipped = 0;
            int inspected = 0;
            var sourceSizes = new SortedSet<string>();
            var scaleNotes = new SortedSet<string>();
            float? sampleScale = null;
            string sampleSourceSize = null;

            foreach (string absolutePath in walkPngs)
            {
                string fileName = Path.GetFileName(absolutePath);
                if (fileName.IndexOf("_fixed", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                string assetPath = ToAssetPath(absolutePath);
                if (string.IsNullOrEmpty(assetPath))
                    continue;

                Texture2D source = LoadPngFromDisk(assetPath);
                if (source == null)
                {
                    Debug.LogWarning($"[SpriteScaleFixTool] Could not load {assetPath}");
                    continue;
                }

                inspected++;
                int srcW = source.width;
                int srcH = source.height;
                sourceSizes.Add($"{srcW}x{srcH}");

                RectInt walkContent = MeasureOpaqueBounds(source);
                bool canvasMismatch = srcW != targetW || srcH != targetH;

                // Uniform scale so walk content is at least as large as idle content
                // (fixes the visible shrink when canvas was already padded to idle size).
                float contentScaleX = walkContent.width > 0
                    ? idleContent.width / (float)walkContent.width
                    : 1f;
                float contentScaleY = walkContent.height > 0
                    ? idleContent.height / (float)walkContent.height
                    : 1f;
                float contentScale = Mathf.Max(contentScaleX, contentScaleY);

                // Treat near-1.0 content scale as already matching (avoid tiny float noise).
                bool contentMismatch = contentScale < 0.98f || contentScale > 1.02f;

                if (!canvasMismatch && !contentMismatch)
                {
                    skipped++;
                    UnityEngine.Object.DestroyImmediate(source);
                    continue;
                }

                float appliedScale = canvasMismatch
                    ? Mathf.Max(targetW / (float)srcW, targetH / (float)srcH)
                    : contentScale;

                sampleScale = appliedScale;
                sampleSourceSize = canvasMismatch
                    ? $"{srcW}x{srcH}"
                    : $"content {walkContent.width}x{walkContent.height}";

                scaleNotes.Add(
                    canvasMismatch
                        ? $"canvas {srcW}x{srcH} → {targetW}x{targetH} (scale≈{appliedScale:F4})"
                        : $"content {walkContent.width}x{walkContent.height} → idle content " +
                          $"{idleContent.width}x{idleContent.height} (scale={appliedScale:F4})");

                Texture2D fixedTex = BuildFixedFrame(
                    source, targetW, targetH, walkContent, idleContent, contentScale, canvasMismatch);
                UnityEngine.Object.DestroyImmediate(source);

                string fixedAssetPath = BuildFixedAssetPath(assetPath);
                byte[] png = fixedTex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(fixedTex);
                File.WriteAllBytes(Path.GetFullPath(fixedAssetPath), png);
                fixedAssetPaths.Add(fixedAssetPath);
                corrected++;
            }

            totalCorrected += corrected;
            totalSkipped += skipped;

            report.AppendLine(
                $"{dir.Key}: idle canvas={targetW}x{targetH}, idle content={idleContent.width}x{idleContent.height}");
            report.AppendLine(
                $"     walk source sizes: {(sourceSizes.Count == 0 ? "(none)" : string.Join(", ", sourceSizes))}");
            report.AppendLine(
                $"     frames inspected={inspected}, corrected={corrected}, already OK={skipped}");
            if (sampleScale.HasValue)
            {
                report.AppendLine(
                    $"     example: {sampleSourceSize} → target {targetW}x{targetH}, " +
                    $"scale factor={sampleScale.Value:F4}");
            }

            if (scaleNotes.Count > 0)
            {
                foreach (string note in scaleNotes)
                    report.AppendLine($"       - {note}");
            }

            report.AppendLine();
        }

        AssetDatabase.Refresh();

        try
        {
            AssetDatabase.StartAssetEditing();
            foreach (string assetPath in fixedAssetPaths)
            {
                if (File.Exists(Path.GetFullPath(assetPath)))
                    ApplyFinalImportSettings(assetPath, idlePpu);
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
        }

        AssetDatabase.Refresh();

        report.AppendLine(
            $"TOTAL: corrected={totalCorrected}, already OK={totalSkipped}, " +
            $"_fixed files written={fixedAssetPaths.Count}");
        Debug.Log(report.ToString());
    }

    /// <summary>
    /// When canvas mismatches: nearest-neighbor stretch to idle canvas.
    /// When only content mismatches: scale opaque pixels by contentScale and center
    /// on the idle content's center within the idle canvas.
    /// </summary>
    static Texture2D BuildFixedFrame(
        Texture2D source,
        int targetW,
        int targetH,
        RectInt walkContent,
        RectInt idleContent,
        float contentScale,
        bool canvasMismatch)
    {
        if (canvasMismatch && !NeedsContentNormalize(contentScale))
        {
            return ScalePointNearestNeighbor(source, targetW, targetH);
        }

        // Content-aware: extract walk content, scale NN, blit centered on idle content center.
        if (walkContent.width <= 0 || walkContent.height <= 0)
            return ScalePointNearestNeighbor(source, targetW, targetH);

        int scaledW = Mathf.Max(1, Mathf.RoundToInt(walkContent.width * contentScale));
        int scaledH = Mathf.Max(1, Mathf.RoundToInt(walkContent.height * contentScale));

        Texture2D cropped = Crop(source, walkContent);
        Texture2D scaled = ScalePointNearestNeighbor(cropped, scaledW, scaledH);
        UnityEngine.Object.DestroyImmediate(cropped);

        var result = new Texture2D(targetW, targetH, TextureFormat.RGBA32, false);
        result.filterMode = FilterMode.Point;
        var clear = new Color32[targetW * targetH];
        result.SetPixels32(clear);

        float idleCenterX = idleContent.xMin + idleContent.width * 0.5f;
        float idleCenterY = idleContent.yMin + idleContent.height * 0.5f;
        int destX = Mathf.RoundToInt(idleCenterX - scaledW * 0.5f);
        int destY = Mathf.RoundToInt(idleCenterY - scaledH * 0.5f);

        Blit(scaled, result, destX, destY);
        UnityEngine.Object.DestroyImmediate(scaled);
        result.Apply(false, false);
        return result;
    }

    static bool NeedsContentNormalize(float contentScale)
    {
        return contentScale < 0.98f || contentScale > 1.02f;
    }

    static string BuildFixedAssetPath(string assetPath)
    {
        string dir = Path.GetDirectoryName(assetPath)?.Replace('\\', '/') ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(assetPath);
        string ext = Path.GetExtension(assetPath);
        if (name.EndsWith("_fixed", StringComparison.OrdinalIgnoreCase))
            return assetPath;
        return $"{dir}/{name}_fixed{ext}";
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
                $"[SpriteScaleFixTool] '{WalkFolderPreferred}' not found or empty — using fallback '{WalkFolderFallback}'.");
            return WalkFolderFallback;
        }

        return null;
    }

    static string ToAssetPath(string path)
    {
        path = path.Replace('\\', '/');
        int idx = path.IndexOf("Assets/", StringComparison.Ordinal);
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
        {
            AssetDatabase.ImportAsset(assetPath);
            importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;
        }

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
            UnityEngine.Object.DestroyImmediate(tex);
            return null;
        }

        return tex;
    }

    static RectInt MeasureOpaqueBounds(Texture2D tex)
    {
        Color32[] pixels = tex.GetPixels32();
        int w = tex.width;
        int h = tex.height;
        int minX = w;
        int minY = h;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                if (pixels[row + x].a < 16)
                    continue;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < 0)
            return new RectInt(0, 0, 0, 0);

        return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    static Texture2D Crop(Texture2D source, RectInt bounds)
    {
        var result = new Texture2D(bounds.width, bounds.height, TextureFormat.RGBA32, false);
        result.filterMode = FilterMode.Point;
        Color[] pixels = source.GetPixels(bounds.x, bounds.y, bounds.width, bounds.height);
        result.SetPixels(pixels);
        result.Apply(false, false);
        return result;
    }

    static void Blit(Texture2D src, Texture2D dst, int destX, int destY)
    {
        Color32[] srcPixels = src.GetPixels32();
        Color32[] dstPixels = dst.GetPixels32();
        int sw = src.width;
        int sh = src.height;
        int dw = dst.width;
        int dh = dst.height;

        for (int y = 0; y < sh; y++)
        {
            int dy = destY + y;
            if (dy < 0 || dy >= dh)
                continue;
            for (int x = 0; x < sw; x++)
            {
                int dx = destX + x;
                if (dx < 0 || dx >= dw)
                    continue;

                Color32 c = srcPixels[y * sw + x];
                if (c.a < 1)
                    continue;
                dstPixels[dy * dw + dx] = c;
            }
        }

        dst.SetPixels32(dstPixels);
    }

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
#endif
