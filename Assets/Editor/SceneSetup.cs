using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SceneSetup
{
    [MenuItem("Tools/Setup Asset Viewer Scene")]
    public static void SetupScene()
    {
        Debug.LogWarning("[SceneSetup] Running Asset Viewer setup (v2 cleanup).");

        // Remove previous generated viewer UI so we don't keep seeing stale instances.
        int removed = 0;

        // Deterministic reset for this demo scene: remove all existing Canvas roots.
        var existingCanvases = Object.FindObjectsOfType<Canvas>(true);
        foreach (var c in existingCanvases)
        {
            if (c == null) continue;
            var root = c.transform.root != null ? c.transform.root.gameObject : c.gameObject;
            if (root == null) continue;
            if (root.name == "Main Camera" || root.name == "EventSystem") continue;
            Object.DestroyImmediate(root);
            removed++;
        }

        var oldLoaders = Object.FindObjectsOfType<AssetLoaderDemo>(true);
        foreach (var loader in oldLoaders)
        {
            var root = loader.transform.root != null ? loader.transform.root.gameObject : loader.gameObject;
            if (root != null)
            {
                Object.DestroyImmediate(root);
                removed++;
            }
        }

        // Remove legacy/hardcoded UI roots by signature names (from earlier setup scripts).
        var legacyNames = new HashSet<string>
        {
            "AssetLoader", "AssetDisplay", "DataPointDisplay", "FileDisplay", "ChannelDisplay",
            "LoadAssetButton", "SubscribeButton", "ExitDetailsButton", "AssetIdInput", "AssetDropdown", "DetailsPanel", "AssetUI_Prefab"
        };

        var rootsToDelete = new HashSet<GameObject>();
        var allTransforms = Object.FindObjectsOfType<Transform>(true);
        foreach (var t in allTransforms)
        {
            if (!legacyNames.Contains(t.name)) continue;
            var root = t.root != null ? t.root.gameObject : t.gameObject;
            if (root != null && root.name != "Main Camera" && root.name != "EventSystem")
                rootsToDelete.Add(root);
        }

        foreach (var r in rootsToDelete)
        {
            Object.DestroyImmediate(r);
            removed++;
        }

        // Secondary pass keeps compatibility with older ad-hoc roots not covered by canvas cleanup.

        Debug.LogWarning($"[SceneSetup] Cleanup removed {removed} old viewer root(s).");

        // 0. Create Main Camera
        if (Object.FindObjectsOfType<Camera>().Length == 0)
        {
            var cameraGO = new GameObject("Main Camera");
            var camera = cameraGO.AddComponent<Camera>();
            camera.tag = "MainCamera";
            cameraGO.AddComponent<AudioListener>();
            cameraGO.transform.position = new Vector3(0, 1, -10);
        }

        // 1. Create Canvas
        var canvasGO = new GameObject("Canvas");
        var canvasRect = canvasGO.AddComponent<RectTransform>();
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        
        // 1.5. Create/Fix EventSystem (Critical for Button Clicks)
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // 4. DB Config Lookup
        var dbConfig = Resources.Load<DBConfig>("DBConfig");
        if (dbConfig == null)
        {
            System.IO.Directory.CreateDirectory("Assets/Resources");
            dbConfig = ScriptableObject.CreateInstance<DBConfig>();
            AssetDatabase.CreateAsset(dbConfig, "Assets/Resources/DBConfig.asset");
            AssetDatabase.SaveAssets();
        }

        // Always regenerate prefab to keep scene setup aligned with latest UI script changes
        AssetUIPrefabCreator.CreateAssetUIPrefab();

        // 5. Instantiate Asset UI Prefab
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AssetUI_Prefab.prefab");
        if (prefab != null)
        {
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            instance.transform.SetParent(canvasGO.transform, false);
            var loader = instance.GetComponent<AssetLoaderDemo>();
            if (loader != null)
            {
                loader.dbConfig = dbConfig;
                loader.detailsPanel = FindChildObject(instance.transform, "DetailsPanel");
                loader.assetDisplayText = FindChildComponent<TextMeshProUGUI>(instance.transform, "AssetDisplay");
                loader.datapointDisplayText = FindChildComponent<TextMeshProUGUI>(instance.transform, "DataPointDisplay");
                loader.filesDisplayText = FindChildComponent<TextMeshProUGUI>(instance.transform, "FileDisplay");
                loader.channelsDisplayText = FindChildComponent<TextMeshProUGUI>(instance.transform, "ChannelDisplay");
                loader.assetIdInput = FindChildComponent<TMP_InputField>(instance.transform, "AssetIdInput");
                loader.assetDropdown = FindChildComponent<Dropdown>(instance.transform, "AssetDropdown");
                loader.loadButton = FindChildComponent<Button>(instance.transform, "LoadAssetButton");
                loader.subscribeButton = FindChildComponent<Button>(instance.transform, "SubscribeButton");
                loader.exitDetailsButton = FindChildComponent<Button>(instance.transform, "ExitDetailsButton");
            }

            Selection.activeGameObject = instance;
            Debug.Log("Asset UI Prefab instantiated with DBConfig assigned.");
        }
        else
        {
            Debug.LogError("AssetUI_Prefab.prefab not found. Run Tools > Create Asset UI Prefab first.");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.LogWarning("[SceneSetup] Asset Viewer scene rebuilt successfully.");
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        var named = FindChildObject(root, childName);
        if (named != null)
        {
            var direct = named.GetComponent<T>();
            if (direct != null)
                return direct;

            var nested = named.GetComponentInChildren<T>(true);
            if (nested != null)
                return nested;
        }

        foreach (var component in root.GetComponentsInChildren<T>(true))
        {
            if (component.name == childName)
                return component;
        }

        return null;
    }

    private static GameObject FindChildObject(Transform root, string childName)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
                return child.gameObject;
        }

        return null;
    }

    private static GameObject CreateTextElement(Transform parent, string name, Vector2 pos, float width, float height)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(width, height);
        
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = name;
        text.fontSize = 22;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static Button CreateButton(Transform parent, string name, Vector2 pos, float width, float height, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        
        var rect = go.AddComponent<RectTransform>();
        rect.anchoredPosition = pos;
        rect.sizeDelta = new Vector2(width, height);
        
        // FIXED: Added Raycast Target
        var image = go.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = true; 
        
        var button = go.AddComponent<Button>();
        // FIXED: Linked Target Graphic
        button.targetGraphic = image; 
        
        // Color block to make hover/click visible
        ColorBlock cb = button.colors;
        cb.highlightedColor = Color.gray;
        cb.pressedColor = Color.black;
        button.colors = cb;

        var textGO = new GameObject("Label");
        textGO.transform.SetParent(go.transform);
        var textRect = textGO.AddComponent<RectTransform>();
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(width, height);
        
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 16;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false; // Allow click to pass through text to the button image

        return button;
    }
}