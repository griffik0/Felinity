#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// One-shot builder for CharacterCreation scene + build settings.
/// Menu: Felinity/Build Character Creation Scene
/// </summary>
public static class CharacterCreationSceneBuilder
{
    const string ScenePath = "Assets/Scenes/CharacterCreation.unity";

    [MenuItem("Felinity/Fix Character Creation Coat Arrows")]
    public static void FixCoatArrows()
    {
        EnsureArrowSprites();
        Sprite arrowLeft = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowLeftPath);
        Sprite arrowRight = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowRightPath);

        var scene = EditorSceneManager.OpenScene(ScenePath);
        var ui = Object.FindAnyObjectByType<CharacterCreationUI>();
        if (ui == null)
        {
            Debug.LogError("[CharacterCreationSceneBuilder] CharacterCreationUI not found.");
            return;
        }

        StyleArrowButton(ui.coatPrevButton, arrowLeft);
        StyleArrowButton(ui.coatNextButton, arrowRight);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[CharacterCreationSceneBuilder] Coat arrows updated to sprite triangles.");
    }

    [MenuItem("Felinity/Build Character Creation Scene")]
    public static void Build()
    {
        Material paletteMat = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Sprites/Cat/CatPaletteMaterial.mat");
        if (paletteMat == null)
        {
            Debug.LogError("Missing CatPaletteMaterial");
            return;
        }

        TMP_FontAsset tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (tmpFont == null)
            tmpFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cam.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0, 0, -10);
        camGo.AddComponent<AudioListener>();

        var lightGo = new GameObject("Global Light 2D");
        var lightType = System.Type.GetType(
            "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime");
        if (lightType != null)
        {
            var light = lightGo.AddComponent(lightType);
            var lightTypeProp = lightType.GetProperty("lightType");
            if (lightTypeProp != null)
            {
                object globalVal = System.Enum.Parse(lightTypeProp.PropertyType, "Global");
                lightTypeProp.SetValue(light, globalVal);
            }
        }

        var esGo = new GameObject("EventSystem");
        esGo.AddComponent<EventSystem>();
        esGo.AddComponent<InputSystemUIInputModule>();

        var preview = new GameObject("PreviewCat");
        preview.transform.position = new Vector3(0f, 0.6f, 0f);
        preview.transform.localScale = Vector3.one * 2.5f;
        var sr = preview.AddComponent<SpriteRenderer>();
        sr.sharedMaterial = paletteMat;
        sr.sortingOrder = 10;

        var facing = preview.AddComponent<PlayerFacing>();
        facing.idleSouth = LoadIdle("cat_idle_s");
        facing.idleSouthEast = LoadIdle("cat_idle_se");
        facing.idleEast = LoadIdle("cat_idle_e");
        facing.idleNorthEast = LoadIdle("cat_idle_ne");
        facing.idleNorth = LoadIdle("cat_idle_n");
        facing.idleNorthWest = LoadIdle("cat_idle_nw");
        facing.idleWest = LoadIdle("cat_idle_w");
        facing.idleSouthWest = LoadIdle("cat_idle_sw");
        sr.sprite = facing.idleSouth;

        var appearance = preview.AddComponent<CatAppearance>();
        appearance.enableDebugHotkeys = false;
        appearance.applyPlayerProfileOnStart = false;
        preview.AddComponent<CharacterPreviewIdle>();

        var canvasGo = new GameObject("CharacterCreationCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        var ui = canvasGo.AddComponent<CharacterCreationUI>();

        MakeTMP("Header", canvasGo.transform, "Customize Your Cat", 56,
            new Vector2(0, 420), new Vector2(900, 80), TextAlignmentOptions.Center, tmpFont);
        MakeTMP("CoatLabel", canvasGo.transform, "Coat", 32,
            new Vector2(0, -80), new Vector2(400, 40), TextAlignmentOptions.Center, tmpFont);

        // Coat carousel: sprite arrows (TMP LiberationSans has no ◀/▶ glyphs)
        EnsureArrowSprites();
        Sprite arrowLeft = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/arrow_left.png");
        Sprite arrowRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/UI/arrow_right.png");

        Button coatPrev = MakeButton(
            "CoatPrevButton", canvasGo.transform, string.Empty,
            new Vector2(-220, -140), new Vector2(64, 64),
            Color.white, tmpFont);
        Button coatNext = MakeButton(
            "CoatNextButton", canvasGo.transform, string.Empty,
            new Vector2(220, -140), new Vector2(64, 64),
            Color.white, tmpFont);
        StyleArrowButton(coatPrev, arrowLeft);
        StyleArrowButton(coatNext, arrowRight);

        var swatchGo = MakePanel("CoatSwatch", canvasGo.transform, new Vector2(0, -140), new Vector2(280, 64));
        var swatchImg = swatchGo.AddComponent<Image>();
        swatchImg.color = new Color(0.949f, 0.596f, 0.278f, 1f);

        var coatNameGo = MakePanel("CoatName", swatchGo.transform, Vector2.zero, Vector2.zero);
        var coatNameRt = coatNameGo.GetComponent<RectTransform>();
        coatNameRt.anchorMin = Vector2.zero;
        coatNameRt.anchorMax = Vector2.one;
        coatNameRt.offsetMin = Vector2.zero;
        coatNameRt.offsetMax = Vector2.zero;
        var coatNameText = coatNameGo.AddComponent<TextMeshProUGUI>();
        if (tmpFont != null) coatNameText.font = tmpFont;
        coatNameText.text = "Orange Tabby";
        coatNameText.fontSize = 26;
        coatNameText.alignment = TextAlignmentOptions.Center;
        coatNameText.color = Color.white;

        MakeTMP("NameLabel", canvasGo.transform, "Name", 32,
            new Vector2(0, -250), new Vector2(400, 40), TextAlignmentOptions.Center, tmpFont);
        MakeTMP("PreviewHint", canvasGo.transform, "Preview", 22,
            new Vector2(0, 280), new Vector2(300, 30), TextAlignmentOptions.Center, tmpFont);

        TMP_InputField nameField = MakeNameField(canvasGo.transform, tmpFont);
        Button confirm = MakeButton(
            "ConfirmButton",
            canvasGo.transform,
            "Confirm",
            new Vector2(0, -420),
            new Vector2(280, 64),
            new Color(0.25f, 0.55f, 0.35f, 1f),
            tmpFont);
        confirm.interactable = false;

        ui.previewAppearance = appearance;
        ui.previewFacing = facing;
        ui.coatPrevButton = coatPrev;
        ui.coatNextButton = coatNext;
        ui.coatSwatchImage = swatchImg;
        ui.coatNameText = coatNameText;
        ui.nameField = nameField;
        ui.confirmButton = confirm;
        ui.defaultName = "Whiskers";
        ui.maxNameLength = 16;

        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/TitleScreen.unity", true),
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene("Assets/Scenes/MainWorld.unity", true)
        };

        var mainScene = EditorSceneManager.OpenScene("Assets/Scenes/MainWorld.unity", OpenSceneMode.Single);
        if (Object.FindObjectsByType<MainWorldBootstrap>().Length == 0)
        {
            var boot = new GameObject("MainWorldBootstrap");
            boot.AddComponent<MainWorldBootstrap>();
            EditorSceneManager.MarkSceneDirty(mainScene);
            EditorSceneManager.SaveScene(mainScene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("CharacterCreation scene built. Build order: TitleScreen, CharacterCreation, MainWorld.");
    }

    static Sprite LoadIdle(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(
            "Assets/Art/Sprites/Cat/Idle/" + name + ".png");
    }

    const string ArrowLeftPath = "Assets/Art/UI/arrow_left.png";
    const string ArrowRightPath = "Assets/Art/UI/arrow_right.png";

    static void EnsureArrowSprites()
    {
        string folder = "Assets/Art/UI";
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        if (!File.Exists(Path.GetFullPath(ArrowLeftPath)))
            WriteArrowPng(ArrowLeftPath, true);
        if (!File.Exists(Path.GetFullPath(ArrowRightPath)))
            WriteArrowPng(ArrowRightPath, false);

        ImportArrowSprite(ArrowLeftPath);
        ImportArrowSprite(ArrowRightPath);
    }

    static void WriteArrowPng(string assetPath, bool pointLeft)
    {
        const int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        var pixels = new Color32[size * size];
        var clear = new Color32(0, 0, 0, 0);
        var fill = new Color32(255, 255, 255, 255);
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = clear;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float tipX = pointLeft
                    ? (size - 1 - x) / (float)(size - 1)
                    : x / (float)(size - 1);
                float vy = Mathf.Abs((y / (float)(size - 1)) * 2f - 1f);
                float half = (1f - tipX) * 0.92f;
                if (tipX >= 0.08f && vy <= half)
                    pixels[y * size + x] = fill;
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        File.WriteAllBytes(Path.GetFullPath(assetPath), tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    static void ImportArrowSprite(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
            return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritePixelsPerUnit = 32f;
        importer.SaveAndReimport();
    }

    static void StyleArrowButton(Button btn, Sprite sprite)
    {
        if (btn == null || sprite == null)
            return;

        var img = btn.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
        }

        var labels = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            labels[i].text = string.Empty;
            labels[i].gameObject.SetActive(false);
        }

        var colors = btn.colors;
        colors.normalColor = new Color(0.85f, 0.88f, 0.92f, 1f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.70f, 0.72f, 0.78f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;
    }

    static GameObject MakePanel(string name, Transform parent, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    static TextMeshProUGUI MakeTMP(
        string name, Transform parent, string text, float fontSize,
        Vector2 pos, Vector2 size, TextAlignmentOptions align, TMP_FontAsset font)
    {
        var go = MakePanel(name, parent, pos, size);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;
        return tmp;
    }

    static Button MakeButton(
        string name, Transform parent, string label, Vector2 pos, Vector2 size,
        Color bg, TMP_FontAsset font)
    {
        var go = MakePanel(name, parent, pos, size);
        var img = go.AddComponent<Image>();
        img.color = bg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.disabledColor = new Color(0.35f, 0.35f, 0.38f, 0.85f);
        colors.normalColor = bg;
        colors.highlightedColor = new Color(
            Mathf.Min(1f, bg.r * 1.15f),
            Mathf.Min(1f, bg.g * 1.15f),
            Mathf.Min(1f, bg.b * 1.15f), 1f);
        colors.pressedColor = bg * 0.85f;
        btn.colors = colors;

        var labelGo = MakePanel("Text", go.transform, Vector2.zero, size);
        var labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        var tmp = labelGo.AddComponent<TextMeshProUGUI>();
        if (font != null) tmp.font = font;
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return btn;
    }

    static TMP_InputField MakeNameField(Transform parent, TMP_FontAsset font)
    {
        var nameGo = MakePanel("NameField", parent, new Vector2(0, -310), new Vector2(420, 56));
        var nameImg = nameGo.AddComponent<Image>();
        nameImg.color = new Color(0.18f, 0.2f, 0.24f, 1f);
        var nameField = nameGo.AddComponent<TMP_InputField>();
        nameField.targetGraphic = nameImg;

        var textArea = MakePanel("Text Area", nameGo.transform, Vector2.zero, new Vector2(400, 48));
        var textAreaRt = textArea.GetComponent<RectTransform>();
        textAreaRt.anchorMin = Vector2.zero;
        textAreaRt.anchorMax = Vector2.one;
        textAreaRt.offsetMin = new Vector2(12, 6);
        textAreaRt.offsetMax = new Vector2(-12, -6);
        textArea.AddComponent<RectMask2D>();

        var placeholderGo = MakePanel("Placeholder", textArea.transform, Vector2.zero, Vector2.zero);
        var phRt = placeholderGo.GetComponent<RectTransform>();
        phRt.anchorMin = Vector2.zero;
        phRt.anchorMax = Vector2.one;
        phRt.offsetMin = Vector2.zero;
        phRt.offsetMax = Vector2.zero;
        var placeholder = placeholderGo.AddComponent<TextMeshProUGUI>();
        if (font != null) placeholder.font = font;
        placeholder.text = "Whiskers";
        placeholder.fontSize = 28;
        placeholder.fontStyle = FontStyles.Italic;
        placeholder.color = new Color(1f, 1f, 1f, 0.35f);
        placeholder.alignment = TextAlignmentOptions.Left;

        var inputTextGo = MakePanel("Text", textArea.transform, Vector2.zero, Vector2.zero);
        var itRt = inputTextGo.GetComponent<RectTransform>();
        itRt.anchorMin = Vector2.zero;
        itRt.anchorMax = Vector2.one;
        itRt.offsetMin = Vector2.zero;
        itRt.offsetMax = Vector2.zero;
        var inputText = inputTextGo.AddComponent<TextMeshProUGUI>();
        if (font != null) inputText.font = font;
        inputText.fontSize = 28;
        inputText.color = Color.white;
        inputText.alignment = TextAlignmentOptions.Left;

        nameField.textViewport = textAreaRt;
        nameField.textComponent = inputText;
        nameField.placeholder = placeholder;
        nameField.characterLimit = 16;
        nameField.contentType = TMP_InputField.ContentType.Standard;
        nameField.lineType = TMP_InputField.LineType.SingleLine;
        return nameField;
    }
}
#endif
