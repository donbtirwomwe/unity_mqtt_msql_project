using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AssetUIPrefabCreator
{
    [MenuItem("Tools/Create Asset UI Prefab")]
    public static void CreateAssetUIPrefab()
    {
        // Create root gameobject
        var root = new GameObject("AssetUI_Prefab");

        // Root is a UI container intended to be parented under a scene Canvas
        var rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 1f);
        rootRect.anchorMax = new Vector2(0f, 1f);
        rootRect.pivot = new Vector2(0f, 1f);
        rootRect.anchoredPosition = new Vector2(16, -16);
        rootRect.sizeDelta = new Vector2(640, 1120);
        var rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.92f, 0.92f, 0.92f, 0.96f);

        var headerGO = new GameObject("HeaderPanel");
        headerGO.transform.SetParent(root.transform);
        var headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(0f, 1f);
        headerRect.pivot = new Vector2(0f, 1f);
        headerRect.anchoredPosition = new Vector2(0, 0);
        headerRect.sizeDelta = new Vector2(640, 124);
        var headerImage = headerGO.AddComponent<Image>();
        headerImage.color = new Color(0.84f, 0.86f, 0.88f, 1f);

        // Add AssetLoaderDemo script
        var loader = root.AddComponent<AssetLoaderDemo>();
        var uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Create asset dropdown (populated at runtime)
        var dropdownGO = new GameObject("AssetDropdown");
        dropdownGO.transform.SetParent(root.transform);
        var dropdownRect = dropdownGO.AddComponent<RectTransform>();
        dropdownRect.anchorMin = new Vector2(0f, 1f);
        dropdownRect.anchorMax = new Vector2(0f, 1f);
        dropdownRect.pivot = new Vector2(0f, 1f);
        dropdownRect.anchoredPosition = new Vector2(10, -12);
        dropdownRect.sizeDelta = new Vector2(448, 52);
        var dropdownImage = dropdownGO.AddComponent<Image>();
        dropdownImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);
        var dropdown = dropdownGO.AddComponent<Dropdown>();
        dropdown.targetGraphic = dropdownImage;

        var labelGO = new GameObject("Label");
        labelGO.transform.SetParent(dropdownGO.transform);
        var labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(14, 6);
        labelRect.offsetMax = new Vector2(-28, -6);
        var labelText = labelGO.AddComponent<Text>();
        labelText.font = uiFont;
        labelText.fontSize = 22;
        labelText.alignment = TextAnchor.MiddleLeft;
        labelText.color = Color.black;
        labelText.text = "Select asset";

        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(dropdownGO.transform);
        var arrowRect = arrowGO.AddComponent<RectTransform>();
        arrowRect.anchorMin = new Vector2(1f, 0.5f);
        arrowRect.anchorMax = new Vector2(1f, 0.5f);
        arrowRect.sizeDelta = new Vector2(26, 26);
        arrowRect.anchoredPosition = new Vector2(-10, 0);
        var arrowText = arrowGO.AddComponent<Text>();
        arrowText.font = uiFont;
        arrowText.fontSize = 22;
        arrowText.alignment = TextAnchor.MiddleCenter;
        arrowText.color = Color.black;
        arrowText.text = "v";

        var templateGO = new GameObject("Template");
        templateGO.transform.SetParent(dropdownGO.transform);
        var templateRect = templateGO.AddComponent<RectTransform>();
        // Open the options list to the right with a wider panel so long labels remain readable.
        templateRect.anchorMin = new Vector2(0, 0);
        templateRect.anchorMax = new Vector2(0, 0);
        templateRect.pivot = new Vector2(0, 1);
        // Open options toward the right side so they stay out of the main detail area.
        templateRect.anchoredPosition = new Vector2(620, -2);
        templateRect.sizeDelta = new Vector2(560, 360);
        var templateImage = templateGO.AddComponent<Image>();
        templateImage.color = Color.white;
        var scrollRect = templateGO.AddComponent<ScrollRect>();
        templateGO.SetActive(false);

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(templateGO.transform);
        var viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.sizeDelta = Vector2.zero;
        var viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        var viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        var dropdownContentGO = new GameObject("Content");
        dropdownContentGO.transform.SetParent(viewportGO.transform);
        var dropdownContentRect = dropdownContentGO.AddComponent<RectTransform>();
        dropdownContentRect.anchorMin = new Vector2(0, 1);
        dropdownContentRect.anchorMax = new Vector2(1, 1);
        dropdownContentRect.pivot = new Vector2(0.5f, 1);
        dropdownContentRect.anchoredPosition = Vector2.zero;
        dropdownContentRect.sizeDelta = new Vector2(0, 46);
        var contentLayout = dropdownContentGO.AddComponent<VerticalLayoutGroup>();
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.spacing = 4f;
        dropdownContentGO.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var itemGO = new GameObject("Item");
        itemGO.transform.SetParent(dropdownContentGO.transform);
        var itemRect = itemGO.AddComponent<RectTransform>();
        itemRect.anchorMin = new Vector2(0, 1f);
        itemRect.anchorMax = new Vector2(1, 1f);
        itemRect.pivot = new Vector2(0.5f, 1f);
        itemRect.sizeDelta = new Vector2(0, 46);
        var itemLayout = itemGO.AddComponent<LayoutElement>();
        itemLayout.minHeight = 46;
        itemLayout.preferredHeight = 46;
        var itemToggle = itemGO.AddComponent<Toggle>();

        var itemBg = itemGO.AddComponent<Image>();
        itemBg.color = Color.white;
        itemToggle.targetGraphic = itemBg;

        var itemCheckGO = new GameObject("Item Checkmark");
        itemCheckGO.transform.SetParent(itemGO.transform);
        var itemCheckRect = itemCheckGO.AddComponent<RectTransform>();
        itemCheckRect.anchorMin = new Vector2(0, 0.5f);
        itemCheckRect.anchorMax = new Vector2(0, 0.5f);
        itemCheckRect.sizeDelta = new Vector2(20, 20);
        itemCheckRect.anchoredPosition = new Vector2(10, 0);
        var itemCheck = itemCheckGO.AddComponent<Image>();
        itemCheck.color = new Color(0.2f, 0.5f, 0.9f, 1f);
        itemToggle.graphic = itemCheck;

        var itemLabelGO = new GameObject("Item Label");
        itemLabelGO.transform.SetParent(itemGO.transform);
        var itemLabelRect = itemLabelGO.AddComponent<RectTransform>();
        itemLabelRect.anchorMin = Vector2.zero;
        itemLabelRect.anchorMax = Vector2.one;
        itemLabelRect.offsetMin = new Vector2(36, 6);
        itemLabelRect.offsetMax = new Vector2(-12, -6);
        var itemLabelText = itemLabelGO.AddComponent<Text>();
        itemLabelText.font = uiFont;
        itemLabelText.fontSize = 20;
        itemLabelText.alignment = TextAnchor.MiddleLeft;
        itemLabelText.color = Color.black;
        itemLabelText.text = "Option";

        scrollRect.content = dropdownContentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        dropdown.template = templateRect;
        dropdown.captionText = labelText;
        dropdown.itemText = itemLabelText;
        dropdown.options = new System.Collections.Generic.List<Dropdown.OptionData>
        {
            new Dropdown.OptionData("Select asset")
        };

        // Create input field for asset ID
        var inputGO = new GameObject("AssetIdInput");
        inputGO.transform.SetParent(root.transform);
        var inputRect = inputGO.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 1f);
        inputRect.anchorMax = new Vector2(0f, 1f);
        inputRect.pivot = new Vector2(0f, 1f);
        inputRect.anchoredPosition = new Vector2(10, -72);
        inputRect.sizeDelta = new Vector2(448, 52);
        var inputImage = inputGO.AddComponent<Image>();
        inputImage.color = Color.white;
        var inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.text = "leaktest"; // default demo asset
        var inputText = new GameObject("Text").AddComponent<TextMeshProUGUI>();
        inputText.transform.SetParent(inputGO.transform);
        inputText.rectTransform.anchoredPosition = Vector2.zero;
        inputText.rectTransform.sizeDelta = new Vector2(432, 42);
        inputText.fontSize = 22;
        inputText.color = Color.black;
        inputText.alignment = TextAlignmentOptions.Left;
        inputField.textComponent = inputText;
        var inputPlaceholder = new GameObject("Placeholder").AddComponent<TextMeshProUGUI>();
        inputPlaceholder.transform.SetParent(inputGO.transform);
        inputPlaceholder.text = "Asset ID";
        inputPlaceholder.fontSize = 20;
        inputPlaceholder.color = Color.gray;
        inputField.placeholder = inputPlaceholder;

        // Create asset button
        var buttonGO = new GameObject("LoadAssetButton");
        buttonGO.transform.SetParent(root.transform);
        var buttonRect = buttonGO.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
        buttonRect.anchoredPosition = new Vector2(468, -12);
        buttonRect.sizeDelta = new Vector2(162, 52);
        
        var button = buttonGO.AddComponent<Button>();
        var buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = Color.white;
        button.targetGraphic = buttonImage;
        
        var buttonTextGO = new GameObject("Text");
        buttonTextGO.transform.SetParent(buttonGO.transform);
        var buttonTextRect = buttonTextGO.AddComponent<RectTransform>();
        buttonTextRect.anchoredPosition = Vector2.zero;
        buttonTextRect.sizeDelta = new Vector2(148, 42);
        var buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Load";
        buttonText.fontSize = 22;
        buttonText.color = Color.black;
        buttonText.alignment = TextAlignmentOptions.Center;

        // Create content panel (initially hidden)
        var contentGO = new GameObject("DetailsPanel");
        contentGO.transform.SetParent(root.transform);
        var contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = new Vector2(10, -138);
        contentRect.sizeDelta = new Vector2(620, 960);
        contentGO.SetActive(false); // Hidden by default
        
        var contentImage = contentGO.AddComponent<Image>();
        contentImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        // Create text elements inside content
        float sectionHeight = 210f;
        float sectionGap = 8f;
        float sectionTop = 52f;

        var assetDisplay = CreateTextElement(contentGO.transform, "AssetDisplay", new Vector2(12, -sectionTop), new Vector2(596, sectionHeight));
        var channelDisplay = CreateTextElement(contentGO.transform, "ChannelDisplay", new Vector2(12, -(sectionTop + sectionHeight + sectionGap)), new Vector2(596, sectionHeight));
        var datapointDisplay = CreateTextElement(contentGO.transform, "DataPointDisplay", new Vector2(12, -(sectionTop + ((sectionHeight + sectionGap) * 2f))), new Vector2(596, sectionHeight));

        var datapointListPanel = new GameObject("DataPointListPanel");
        datapointListPanel.transform.SetParent(datapointDisplay.transform, false);
        var listRect = datapointListPanel.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(0f, 1f);
        listRect.pivot = new Vector2(0f, 1f);
        listRect.anchoredPosition = new Vector2(8f, -40f);
        listRect.sizeDelta = new Vector2(580f, 162f);
        var listBg = datapointListPanel.AddComponent<Image>();
        listBg.color = new Color(0.85f, 0.9f, 0.85f, 0.96f);

        var listViewportGO = new GameObject("Viewport");
        listViewportGO.transform.SetParent(datapointListPanel.transform, false);
        var listViewportRect = listViewportGO.AddComponent<RectTransform>();
        listViewportRect.anchorMin = new Vector2(0f, 0f);
        listViewportRect.anchorMax = new Vector2(1f, 1f);
        listViewportRect.offsetMin = new Vector2(8f, 8f);
        listViewportRect.offsetMax = new Vector2(-24f, -8f);
        var listViewportImage = listViewportGO.AddComponent<Image>();
        listViewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        var listViewportMask = listViewportGO.AddComponent<Mask>();
        listViewportMask.showMaskGraphic = false;

        var listContentGO = new GameObject("DataPointListContent");
        listContentGO.transform.SetParent(listViewportGO.transform, false);
        var listContentRect = listContentGO.AddComponent<RectTransform>();
        listContentRect.anchorMin = new Vector2(0f, 1f);
        listContentRect.anchorMax = new Vector2(1f, 1f);
        listContentRect.pivot = new Vector2(0.5f, 1f);
        listContentRect.anchoredPosition = Vector2.zero;
        listContentRect.sizeDelta = new Vector2(0f, 162f);

        var listScroll = datapointListPanel.AddComponent<ScrollRect>();
        listScroll.horizontal = false;
        listScroll.vertical = true;
        listScroll.viewport = listViewportRect;
        listScroll.content = listContentRect;
        listScroll.movementType = ScrollRect.MovementType.Clamped;
        listScroll.scrollSensitivity = 22f;

        var listScrollbarGO = new GameObject("ScrollbarV");
        listScrollbarGO.transform.SetParent(datapointListPanel.transform, false);
        var listScrollbarRect = listScrollbarGO.AddComponent<RectTransform>();
        listScrollbarRect.anchorMin = new Vector2(1f, 0f);
        listScrollbarRect.anchorMax = new Vector2(1f, 1f);
        listScrollbarRect.pivot = new Vector2(1f, 0.5f);
        listScrollbarRect.sizeDelta = new Vector2(12f, -8f);
        listScrollbarRect.anchoredPosition = new Vector2(-6f, 0f);
        var listScrollbarTrack = listScrollbarGO.AddComponent<Image>();
        listScrollbarTrack.color = new Color(0.78f, 0.8f, 0.84f, 1f);
        var listScrollbar = listScrollbarGO.AddComponent<Scrollbar>();
        listScrollbar.direction = Scrollbar.Direction.BottomToTop;
        var listHandleGO = new GameObject("Handle");
        listHandleGO.transform.SetParent(listScrollbarGO.transform, false);
        var listHandleRect = listHandleGO.AddComponent<RectTransform>();
        listHandleRect.anchorMin = Vector2.zero;
        listHandleRect.anchorMax = Vector2.one;
        listHandleRect.offsetMin = Vector2.zero;
        listHandleRect.offsetMax = Vector2.zero;
        var listHandleImage = listHandleGO.AddComponent<Image>();
        listHandleImage.color = new Color(0.38f, 0.44f, 0.53f, 1f);
        listScrollbar.targetGraphic = listHandleImage;
        listScrollbar.handleRect = listHandleRect;
        listScroll.verticalScrollbar = listScrollbar;
        listScroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        var fileDisplay = CreateTextElement(contentGO.transform, "FileDisplay", new Vector2(12, -(sectionTop + ((sectionHeight + sectionGap) * 3f))), new Vector2(596, sectionHeight));

        // Create subscribe button in content
        var subscribeButtonGO = new GameObject("SubscribeButton");
        subscribeButtonGO.transform.SetParent(contentGO.transform);
        var subButtonRect = subscribeButtonGO.AddComponent<RectTransform>();
        subButtonRect.anchorMin = new Vector2(0f, 1f);
        subButtonRect.anchorMax = new Vector2(0f, 1f);
        subButtonRect.pivot = new Vector2(0f, 1f);
        subButtonRect.anchoredPosition = new Vector2(482, -10);
        subButtonRect.sizeDelta = new Vector2(100, 36);
        var subButton = subscribeButtonGO.AddComponent<Button>();
        var subImage = subscribeButtonGO.AddComponent<Image>();
        subImage.color = new Color(0.42f, 0.58f, 0.42f, 1f);
        subButton.targetGraphic = subImage;
        var subTextGO = new GameObject("Text");
        subTextGO.transform.SetParent(subscribeButtonGO.transform);
        var subTextRect = subTextGO.AddComponent<RectTransform>();
        subTextRect.anchoredPosition = Vector2.zero;
        subTextRect.sizeDelta = new Vector2(92, 30);
        var subText = subTextGO.AddComponent<TextMeshProUGUI>();
        subText.text = "ReSub";
        subText.fontSize = 16;
        subText.color = Color.white;
        subText.alignment = TextAlignmentOptions.Center;

        // Exit details button
        var exitButtonGO = new GameObject("ExitDetailsButton");
        exitButtonGO.transform.SetParent(contentGO.transform);
        var exitRect = exitButtonGO.AddComponent<RectTransform>();
        exitRect.anchorMin = new Vector2(0f, 1f);
        exitRect.anchorMax = new Vector2(0f, 1f);
        exitRect.pivot = new Vector2(0f, 1f);
        exitRect.anchoredPosition = new Vector2(588, -10);
        exitRect.sizeDelta = new Vector2(30, 30);
        var exitButton = exitButtonGO.AddComponent<Button>();
        var exitImage = exitButtonGO.AddComponent<Image>();
        exitImage.color = new Color(0.55f, 0.55f, 0.55f, 1f);
        exitButton.targetGraphic = exitImage;
        var exitTextGO = new GameObject("Text");
        exitTextGO.transform.SetParent(exitButtonGO.transform);
        var exitTextRect = exitTextGO.AddComponent<RectTransform>();
        exitTextRect.anchoredPosition = Vector2.zero;
        exitTextRect.sizeDelta = new Vector2(24, 24);
        var exitText = exitTextGO.AddComponent<TextMeshProUGUI>();
        exitText.text = "X";
        exitText.fontSize = 14;
        exitText.color = Color.white;
        exitText.alignment = TextAlignmentOptions.Center;

        // Assign to loader
        loader.assetDisplayText = assetDisplay.GetComponentInChildren<TextMeshProUGUI>(true);
        loader.datapointDisplayText = datapointDisplay.GetComponentInChildren<TextMeshProUGUI>(true);
        loader.filesDisplayText = fileDisplay.GetComponentInChildren<TextMeshProUGUI>(true);
        loader.channelsDisplayText = channelDisplay.GetComponentInChildren<TextMeshProUGUI>(true);
        loader.detailsPanel = contentGO;
        loader.assetIdInput = inputField;
        loader.assetDropdown = dropdown;
        loader.loadButton = button;
        loader.subscribeButton = subButton;
        loader.exitDetailsButton = exitButton;

        // Keep the prefab clean; runtime script wires listeners.

        // Save as prefab
        string prefabPath = "Assets/AssetUI_Prefab.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log("Asset UI prefab created at " + prefabPath + " with expandable details.");
    }

    private static GameObject CreateTextElement(Transform parent, string name, Vector2 pos, Vector2 size)
    {
        const float headerContentOffset = 48f;

        var go = new GameObject(name);
        go.transform.SetParent(parent);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var bg = go.AddComponent<Image>();
        bg.color = new Color(0.96f, 0.97f, 0.98f, 0.96f);

        string headerLabel = name;
        if (name == "AssetDisplay") headerLabel = "Asset";
        else if (name == "ChannelDisplay") headerLabel = "Telemetry";
        else if (name == "DataPointDisplay") headerLabel = "Datapoints";
        else if (name == "FileDisplay") headerLabel = "Files";

        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(go.transform, false);
        var headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, 0f);
        headerRect.sizeDelta = new Vector2(-12f, 30f);
        var headerText = headerGO.AddComponent<TextMeshProUGUI>();
        headerText.text = headerLabel;
        headerText.fontSize = 15;
        headerText.color = new Color(0.16f, 0.2f, 0.28f, 1f);
        headerText.alignment = TextAlignmentOptions.Left;

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(go.transform, false);
        var viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(8f, 8f);
        viewportRect.offsetMax = new Vector2(-24f, -headerContentOffset);
        var viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        var viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        var dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(go.transform, false);
        var dividerRect = dividerGO.AddComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -34f);
        dividerRect.sizeDelta = new Vector2(-12f, 2f);
        var dividerImage = dividerGO.AddComponent<Image>();
        dividerImage.color = new Color(0.75f, 0.78f, 0.84f, 1f);

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        var contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(size.x * 1.8f, size.y * 2.2f);

        var text = contentGO.AddComponent<TextMeshProUGUI>();
        text.text = name;
        text.fontSize = 22;
        text.color = Color.black;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Masking;
        text.rectTransform.anchorMin = new Vector2(0f, 1f);
        text.rectTransform.anchorMax = new Vector2(0f, 1f);
        text.rectTransform.pivot = new Vector2(0f, 1f);
        text.rectTransform.anchoredPosition = Vector2.zero;
        text.rectTransform.sizeDelta = contentRect.sizeDelta;

        var scrollRect = go.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 18f;

        var verticalScrollbarGO = new GameObject("ScrollbarV");
        verticalScrollbarGO.transform.SetParent(go.transform, false);
        var vRect = verticalScrollbarGO.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(1f, 0f);
        vRect.anchorMax = new Vector2(1f, 1f);
        vRect.pivot = new Vector2(1f, 0.5f);
        vRect.sizeDelta = new Vector2(12f, -8f);
        vRect.anchoredPosition = new Vector2(-6f, 0f);
        var vTrack = verticalScrollbarGO.AddComponent<Image>();
        vTrack.color = new Color(0.78f, 0.8f, 0.84f, 1f);
        var vScrollbar = verticalScrollbarGO.AddComponent<Scrollbar>();
        vScrollbar.direction = Scrollbar.Direction.BottomToTop;
        var vHandle = new GameObject("Handle");
        vHandle.transform.SetParent(verticalScrollbarGO.transform, false);
        var vHandleRect = vHandle.AddComponent<RectTransform>();
        vHandleRect.anchorMin = Vector2.zero;
        vHandleRect.anchorMax = Vector2.one;
        vHandleRect.offsetMin = Vector2.zero;
        vHandleRect.offsetMax = Vector2.zero;
        var vHandleImage = vHandle.AddComponent<Image>();
        vHandleImage.color = new Color(0.38f, 0.44f, 0.53f, 1f);
        vScrollbar.targetGraphic = vHandleImage;
        vScrollbar.handleRect = vHandleRect;
        scrollRect.verticalScrollbar = vScrollbar;

        var horizontalScrollbarGO = new GameObject("ScrollbarH");
        horizontalScrollbarGO.transform.SetParent(go.transform, false);
        var hRect = horizontalScrollbarGO.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 0f);
        hRect.anchorMax = new Vector2(1f, 0f);
        hRect.pivot = new Vector2(0.5f, 0f);
        hRect.sizeDelta = new Vector2(-24f, 12f);
        hRect.anchoredPosition = new Vector2(-6f, 6f);
        var hTrack = horizontalScrollbarGO.AddComponent<Image>();
        hTrack.color = new Color(0.78f, 0.8f, 0.84f, 1f);
        var hScrollbar = horizontalScrollbarGO.AddComponent<Scrollbar>();
        hScrollbar.direction = Scrollbar.Direction.LeftToRight;
        var hHandle = new GameObject("Handle");
        hHandle.transform.SetParent(horizontalScrollbarGO.transform, false);
        var hHandleRect = hHandle.AddComponent<RectTransform>();
        hHandleRect.anchorMin = Vector2.zero;
        hHandleRect.anchorMax = Vector2.one;
        hHandleRect.offsetMin = Vector2.zero;
        hHandleRect.offsetMax = Vector2.zero;
        var hHandleImage = hHandle.AddComponent<Image>();
        hHandleImage.color = new Color(0.38f, 0.44f, 0.53f, 1f);
        hScrollbar.targetGraphic = hHandleImage;
        hScrollbar.handleRect = hHandleRect;
        scrollRect.horizontalScrollbar = hScrollbar;

        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
        scrollRect.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;

        // Keep header visuals above scrolling content so section titles stay fixed.
        dividerGO.transform.SetAsLastSibling();
        headerGO.transform.SetAsLastSibling();
        return go;
    }
}