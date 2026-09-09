#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Places Gerald + DialogueUI into MainWorld.
/// Menu: Felinity/Setup Gerald NPC
/// </summary>
public static class GeraldSceneSetup
{
    const string MainWorldPath = "Assets/Scenes/MainWorld.unity";
    const string WildBerryPath = "Assets/Items/WildBerry.asset";

    [MenuItem("Felinity/Setup Gerald NPC")]
    public static void Setup()
    {
        var scene = EditorSceneManager.OpenScene(MainWorldPath);
        ItemData wildBerry = AssetDatabase.LoadAssetAtPath<ItemData>(WildBerryPath);
        if (wildBerry == null)
        {
            Debug.LogError("WildBerry ItemData missing at " + WildBerryPath);
            return;
        }

        EnsureDialogueUI();
        EnsureGerald(wildBerry);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[GeraldSceneSetup] Gerald + DialogueUI ready in MainWorld.");
    }

    static void EnsureDialogueUI()
    {
        var existing = Object.FindAnyObjectByType<DialogueUI>();
        if (existing != null)
            return;

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        GameObject canvasGo = GameObject.Find("GameUI");
        if (canvasGo == null)
        {
            canvasGo = new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        var host = new GameObject("DialogueUI");
        host.transform.SetParent(canvasGo.transform, false);
        var ui = host.AddComponent<DialogueUI>();

        var root = CreateRect("DialogueBox", host.transform);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(0.5f, 0f);
        rootRt.anchorMax = new Vector2(0.5f, 0f);
        rootRt.pivot = new Vector2(0.5f, 0f);
        rootRt.anchoredPosition = new Vector2(0f, 40f);
        rootRt.sizeDelta = new Vector2(900f, 180f);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.09f, 0.12f, 0.92f);

        var speakerGo = CreateRect("SpeakerText", root.transform);
        var speakerRt = speakerGo.GetComponent<RectTransform>();
        speakerRt.anchorMin = new Vector2(0f, 1f);
        speakerRt.anchorMax = new Vector2(1f, 1f);
        speakerRt.pivot = new Vector2(0.5f, 1f);
        speakerRt.anchoredPosition = new Vector2(0f, -12f);
        speakerRt.sizeDelta = new Vector2(-40f, 36f);
        var speaker = speakerGo.AddComponent<TextMeshProUGUI>();
        if (font != null) speaker.font = font;
        speaker.fontSize = 28;
        speaker.fontStyle = FontStyles.Bold;
        speaker.color = new Color(1f, 0.85f, 0.45f, 1f);
        speaker.text = "Gerald";

        var bodyGo = CreateRect("BodyText", root.transform);
        var bodyRt = bodyGo.GetComponent<RectTransform>();
        bodyRt.anchorMin = new Vector2(0f, 0f);
        bodyRt.anchorMax = new Vector2(1f, 1f);
        bodyRt.offsetMin = new Vector2(20f, 40f);
        bodyRt.offsetMax = new Vector2(-20f, -48f);
        var body = bodyGo.AddComponent<TextMeshProUGUI>();
        if (font != null) body.font = font;
        body.fontSize = 26;
        body.color = Color.white;
        body.text = "...";
        body.textWrappingMode = TextWrappingModes.Normal;

        var promptGo = CreateRect("ContinuePrompt", root.transform);
        var promptRt = promptGo.GetComponent<RectTransform>();
        promptRt.anchorMin = new Vector2(1f, 0f);
        promptRt.anchorMax = new Vector2(1f, 0f);
        promptRt.pivot = new Vector2(1f, 0f);
        promptRt.anchoredPosition = new Vector2(-16f, 10f);
        promptRt.sizeDelta = new Vector2(220f, 28f);
        var prompt = promptGo.AddComponent<TextMeshProUGUI>();
        if (font != null) prompt.font = font;
        prompt.fontSize = 20;
        prompt.alignment = TextAlignmentOptions.Right;
        prompt.color = new Color(0.75f, 0.78f, 0.85f, 1f);
        prompt.text = "[E] Continue";

        ui.root = root;
        ui.speakerText = speaker;
        ui.bodyText = body;
        ui.continuePromptText = prompt;
        root.SetActive(false);
    }

    static void EnsureGerald(ItemData wildBerry)
    {
        GameObject gerald = GameObject.Find("Gerald");
        if (gerald == null)
        {
            gerald = new GameObject("Gerald");
            gerald.transform.position = new Vector3(-3.5f, 2f, 0f);
            gerald.tag = "Untagged";

            var sr = gerald.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 5;
            gerald.transform.localScale = Vector3.one;

            var col = gerald.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            gerald.AddComponent<GeraldController>();
        }

        var controller = gerald.GetComponent<GeraldController>();
        if (controller == null)
            controller = gerald.AddComponent<GeraldController>();

        controller.wildBerry = wildBerry;
        controller.berriesRequired = 3;
        controller.coinReward = 25;
        controller.EnsureDefaults();
        AssignIdleSprites(controller);
        controller.ApplyIdleSprite();
        EditorUtility.SetDirty(controller);

        var renderer = gerald.GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            renderer.color = Color.white;
            renderer.sortingOrder = 5;
            EditorUtility.SetDirty(renderer);
        }

        // Placeholder brown cube used scale 1.2; real sprites are already cat-sized at PPU 48.
        if (controller.idleSouth != null)
            gerald.transform.localScale = Vector3.one;

        // Name lives in dialogue only — remove any leftover world label.
        Transform labelT = gerald.transform.Find("NameLabel");
        if (labelT != null)
            Object.DestroyImmediate(labelT.gameObject);

        CenterTalkCollider(gerald);
    }

    static void CenterTalkCollider(GameObject gerald)
    {
        var col = gerald.GetComponent<CircleCollider2D>();
        if (col == null)
            col = gerald.AddComponent<CircleCollider2D>();

        col.isTrigger = true;
        // Pivot centers the cat art; keep the talk radius on the model origin.
        col.offset = Vector2.zero;

        var sr = gerald.GetComponent<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            Bounds b = sr.sprite.bounds;
            col.radius = Mathf.Clamp(Mathf.Max(b.extents.x, b.extents.y) * 0.9f, 0.45f, 0.75f);
        }
        else
        {
            col.radius = 0.55f;
        }

        EditorUtility.SetDirty(col);
    }

    static void AssignIdleSprites(GeraldController controller)
    {
        const string idleFolder = "Assets/Art/Sprites/Gerald/Idle";
        controller.idleSouth = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/south.png");
        controller.idleSouthEast = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/south-east.png");
        controller.idleEast = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/east.png");
        controller.idleNorthEast = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/north-east.png");
        controller.idleNorth = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/north.png");
        controller.idleNorthWest = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/north-west.png");
        controller.idleWest = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/west.png");
        controller.idleSouthWest = AssetDatabase.LoadAssetAtPath<Sprite>(idleFolder + "/south-west.png");
        controller.idleFacing = PlayerFacing.Facing.South;
    }

    static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
}

#endif
