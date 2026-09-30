#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using PixelLab.MapExport;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Builds the MainWorld "Water" collision tilemap from the PixelLab terrain tilemap.
/// PixelLab bakes every material into one Tilemap, so water cells are detected per tile and
/// each water corner becomes a half-cell blocker (shorelines follow the painted edge).
/// Bridges under Grid/Map Objects/Bridges are carved out; a loop collider fences the map edge.
/// </summary>
public static class WaterCollisionBuilder
{
    const string GridName = "Grid";
    const string TerrainName = "Terrain";
    const string WaterGridName = "Water Grid";
    const string WaterName = "Water";
    const string MapBoundsName = "Map Bounds";
    const string BridgesPath = "Map Objects/Bridges";
    const string TileFolder = "Assets/Tilemaps";
    const string SpritePath = TileFolder + "/WaterCollision.png";
    const string TilePath = TileFolder + "/WaterCollisionTile.asset";

    // PixelLab standard 4x4 Wang layout, indexed by tileIndex.
    // Bits: NW=1, NE=2, SW=4, SE=8, set where that corner is the lower terrain (water).
    static readonly int[] LowerCornerMasks = { 4, 10, 13, 12, 9, 14, 15, 7, 2, 3, 11, 5, 0, 8, 6, 1 };

    [MenuItem("Tools/Felinity/Rebuild Water Collision")]
    public static void Rebuild()
    {
        var gridGo = GameObject.Find(GridName);
        var terrainTf = gridGo != null ? gridGo.transform.Find(TerrainName) : null;
        var terrain = terrainTf != null ? terrainTf.GetComponent<Tilemap>() : null;
        if (terrain == null)
        {
            Debug.LogError("[WaterCollisionBuilder] Expected Grid/Terrain tilemap in the open scene.");
            return;
        }

        var water = EnsureWaterLayer(gridGo.transform);
        var collisionTile = LoadOrCreateCollisionTile();
        var bridgeRects = CollectBridgeRects(gridGo.transform);

        terrain.CompressBounds();
        var positions = new List<Vector3Int>();
        int waterCells = 0;
        int carved = 0;
        foreach (var cell in terrain.cellBounds.allPositionsWithin)
        {
            var tile = terrain.GetTile<PixelLabPaintTile>(cell);
            if (!IsWaterTile(tile))
                continue;

            waterCells++;
            int mask = LowerCornerMasks[tile.tileIndex];
            int hx = cell.x * 2;
            int hy = cell.y * 2;
            if ((mask & 1) != 0) positions.Add(new Vector3Int(hx, hy + 1, 0));
            if ((mask & 2) != 0) positions.Add(new Vector3Int(hx + 1, hy + 1, 0));
            if ((mask & 4) != 0) positions.Add(new Vector3Int(hx, hy, 0));
            if ((mask & 8) != 0) positions.Add(new Vector3Int(hx + 1, hy, 0));
        }

        water.ClearAllTiles();
        var kept = new List<Vector3Int>(positions.Count);
        foreach (var p in positions)
        {
            Vector3 c = water.GetCellCenterWorld(p);
            if (InsideAny(bridgeRects, c))
            {
                carved++;
                continue;
            }
            kept.Add(p);
        }

        var tiles = new TileBase[kept.Count];
        for (int i = 0; i < tiles.Length; i++)
            tiles[i] = collisionTile;
        water.SetTiles(kept.ToArray(), tiles);

        var tilemapCollider = water.GetComponent<TilemapCollider2D>();
        tilemapCollider.ProcessTilemapChanges();
        water.GetComponent<CompositeCollider2D>().GenerateGeometry();

        BuildMapBounds(gridGo.transform, terrain);

        EditorSceneManager.MarkSceneDirty(gridGo.scene);
        Debug.Log($"[WaterCollisionBuilder] {waterCells} water terrain cells -> {kept.Count} half-cell blockers " +
                  $"({carved} carved for {bridgeRects.Count} bridge(s)).");
    }

    static bool IsWaterTile(PixelLabPaintTile tile)
    {
        return tile != null
               && tile.tileIndex >= 0
               && tile.tileIndex < LowerCornerMasks.Length
               && !string.IsNullOrEmpty(tile.assetPath)
               && tile.assetPath.IndexOf("water", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    static Tilemap EnsureWaterLayer(Transform grid)
    {
        var waterGridTf = grid.Find(WaterGridName);
        if (waterGridTf == null)
        {
            waterGridTf = new GameObject(WaterGridName).transform;
            waterGridTf.SetParent(grid, false);
        }
        var halfGrid = GetOrAdd<Grid>(waterGridTf.gameObject);
        halfGrid.cellSize = new Vector3(0.5f, 0.5f, 1f);

        var waterTf = waterGridTf.Find(WaterName);
        if (waterTf == null)
        {
            waterTf = new GameObject(WaterName).transform;
            waterTf.SetParent(waterGridTf, false);
        }
        var go = waterTf.gameObject;
        var tilemap = GetOrAdd<Tilemap>(go);
        var renderer = GetOrAdd<TilemapRenderer>(go);
        renderer.enabled = false;

        var body = GetOrAdd<Rigidbody2D>(go);
        body.bodyType = RigidbodyType2D.Static;

        var tilemapCollider = GetOrAdd<TilemapCollider2D>(go);
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        var composite = GetOrAdd<CompositeCollider2D>(go);
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
        return tilemap;
    }

    static List<Rect> CollectBridgeRects(Transform grid)
    {
        var rects = new List<Rect>();
        var bridges = grid.Find(BridgesPath);
        if (bridges == null)
            return rects;
        foreach (var sr in bridges.GetComponentsInChildren<SpriteRenderer>(true))
        {
            var b = sr.bounds;
            rects.Add(Rect.MinMaxRect(b.min.x, b.min.y, b.max.x, b.max.y));
        }
        return rects;
    }

    static bool InsideAny(List<Rect> rects, Vector3 point)
    {
        foreach (var r in rects)
        {
            if (r.Contains(new Vector2(point.x, point.y)))
                return true;
        }
        return false;
    }

    static void BuildMapBounds(Transform grid, Tilemap terrain)
    {
        var boundsTf = grid.Find(MapBoundsName);
        if (boundsTf == null)
        {
            boundsTf = new GameObject(MapBoundsName).transform;
            boundsTf.SetParent(grid, false);
        }
        boundsTf.localPosition = Vector3.zero;
        var edge = GetOrAdd<EdgeCollider2D>(boundsTf.gameObject);

        var b = terrain.cellBounds;
        Vector3 min = terrain.CellToLocal(new Vector3Int(b.xMin, b.yMin, 0)) + terrain.transform.localPosition;
        Vector3 max = terrain.CellToLocal(new Vector3Int(b.xMax, b.yMax, 0)) + terrain.transform.localPosition;
        edge.points = new[]
        {
            new Vector2(min.x, min.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y),
            new Vector2(min.x, max.y),
            new Vector2(min.x, min.y),
        };
    }

    static T GetOrAdd<T>(GameObject go) where T : Component
    {
        var existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    static Tile LoadOrCreateCollisionTile()
    {
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
        if (tile != null)
            return tile;

        if (!AssetDatabase.IsValidFolder(TileFolder))
            AssetDatabase.CreateFolder("Assets", "Tilemaps");

        if (AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath) == null)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            var pixels = new Color32[16 * 16];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            File.WriteAllBytes(SpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
        tile.color = new Color(0.2f, 0.55f, 1f, 0.45f);
        tile.colliderType = Tile.ColliderType.Grid;
        AssetDatabase.CreateAsset(tile, TilePath);
        AssetDatabase.SaveAssets();
        return tile;
    }
}
#endif
