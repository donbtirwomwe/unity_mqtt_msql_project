using UnityEngine;
using TMPro;
using UnityEngine.UI; // Required for Button component
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;
using System.Text;
using System;
using System.Linq;
using System.IO;

public class AssetLoaderDemo : MonoBehaviour
{
    private enum AssetMessagingRole
    {
        Subscriber,
        Publisher,
        Both
    }

    [Header("Configuration")]
    public DBConfig dbConfig;
    
    [Header("UI Elements")]
    public TMP_Text assetDisplayText;
    public TMP_Text datapointDisplayText;
    public TMP_Text filesDisplayText;
    public TMP_Text channelsDisplayText;
    public TMP_InputField assetIdInput;
    public Dropdown assetDropdown;
    public GameObject detailsPanel;
    public Button loadButton;
    public Button subscribeButton;
    public Button exitDetailsButton;

    [Header("Status Colors")]
    public Color normalColor = Color.black;
    public Color onlineColor = Color.blue;
    public Color offlineColor = Color.gray;
    public Color alarmColor = Color.red;

    private Asset currentAsset;
    private DataPoint currentDataPoint;
    private DataPoint previousDataPoint;
    private MqttClient mqttClient;
    private GameObject datapointPopupPanel;
    private TMP_Text datapointPopupSummaryText;
    private TMP_Text datapointPopupTelemetryText;
    private RectTransform datapointPopupFilesPanel;
    private RectTransform filesDisplayLinksPanel;
    private RectTransform datapointListPanel;
    private RectTransform datapointListContent;
    private RectTransform sceneImpressionPanel;
    private readonly List<string> dropdownAssetIds = new List<string>();
    private float lastDisplayUpdateTime = 0f;
    private const float DisplayUpdateInterval = 0.1f; 
    private const float SectionHeaderContentOffset = 48f;
    private static readonly Color LinkButtonColor = new Color(0.24f, 0.46f, 0.7f, 0.95f);

    private AssetMessagingRole currentRole = AssetMessagingRole.Both;

    void Start()
    {
        if (dbConfig == null)
            dbConfig = Resources.Load<DBConfig>("DBConfig");

        AutoBindUiReferences();
        EnsureDatapointPopupPanel();
        EnsureDraggablePanels();
        WireButtons();
        RefreshAssetDropdown();
    }

    void OnEnable()
    {
        AutoBindUiReferences();
        EnsureDraggablePanels();
        WireButtons();
    }

    private void EnsureDraggablePanels()
    {
        if (detailsPanel != null)
            EnsureDraggableRect(detailsPanel.GetComponent<RectTransform>());

        if (datapointPopupPanel != null)
            EnsureDraggableRect(datapointPopupPanel.GetComponent<RectTransform>());

        if (sceneImpressionPanel != null)
            EnsureDraggableRect(sceneImpressionPanel);

        if (assetDropdown != null && assetDropdown.template != null)
            EnsureDraggableRect(assetDropdown.template);
    }

    private void EnsureDraggableRect(RectTransform rect)
    {
        if (rect == null)
            return;

        var drag = rect.GetComponent<UIDragPanel>();
        if (drag == null)
            drag = rect.gameObject.AddComponent<UIDragPanel>();

        if (drag.target == null)
            drag.target = rect;
    }

    private void AutoBindUiReferences()
    {
        if (detailsPanel == null)
            detailsPanel = FindChildObject("DetailsPanel");

        if (assetDisplayText == null)
            assetDisplayText = FindChildComponent<TMP_Text>("AssetDisplay");

        if (datapointDisplayText == null)
            datapointDisplayText = FindChildComponent<TMP_Text>("DataPointDisplay");

        if (filesDisplayText == null)
            filesDisplayText = FindChildComponent<TMP_Text>("FileDisplay");

        if (channelsDisplayText == null)
            channelsDisplayText = FindChildComponent<TMP_Text>("ChannelDisplay");

        if (datapointListPanel == null)
            datapointListPanel = FindChildComponent<RectTransform>("DataPointListPanel");

        if (datapointListContent == null)
            datapointListContent = FindChildComponent<RectTransform>("DataPointListContent");

        if (assetIdInput == null)
        {
            assetIdInput = FindChildComponent<TMP_InputField>("AssetIdInput");
            if (assetIdInput == null)
            {
                var inputs = GetComponentsInChildren<TMP_InputField>(true);
                if (inputs.Length > 0)
                    assetIdInput = inputs[0];
            }
        }

        if (assetDropdown == null)
            assetDropdown = FindChildComponent<Dropdown>("AssetDropdown");

        if (loadButton == null)
            loadButton = FindChildComponent<Button>("LoadAssetButton") ?? FindChildComponent<Button>("AssetButton");

        if (subscribeButton == null)
            subscribeButton = FindChildComponent<Button>("SubscribeButton");

        if (exitDetailsButton == null)
            exitDetailsButton = FindChildComponent<Button>("ExitDetailsButton");

        if (loadButton == null || subscribeButton == null || exitDetailsButton == null)
        {
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var button in buttons)
            {
                if (loadButton == null && (button.name == "LoadAssetButton" || button.name == "AssetButton"))
                    loadButton = button;

                if (subscribeButton == null && button.name == "SubscribeButton")
                    subscribeButton = button;

                if (exitDetailsButton == null && button.name == "ExitDetailsButton")
                    exitDetailsButton = button;
            }
        }
    }

    private T FindChildComponent<T>(string childName) where T : Component
    {
        var child = transform.Find(childName);
        if (child != null)
        {
            var direct = child.GetComponent<T>();
            if (direct != null)
                return direct;

            var nested = child.GetComponentInChildren<T>(true);
            if (nested != null)
                return nested;
        }

        if (detailsPanel != null)
        {
            child = detailsPanel.transform.Find(childName);
            if (child != null)
            {
                var direct = child.GetComponent<T>();
                if (direct != null)
                    return direct;

                var nested = child.GetComponentInChildren<T>(true);
                if (nested != null)
                    return nested;
            }
        }

        var components = GetComponentsInChildren<T>(true);
        foreach (var component in components)
        {
            if (component.name == childName)
                return component;
        }

        return null;
    }

    private GameObject FindChildObject(string childName)
    {
        var child = transform.Find(childName);
        if (child != null)
            return child.gameObject;

        foreach (var rectTransform in GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform.name == childName)
                return rectTransform.gameObject;
        }

        return null;
    }

    private void WireButtons()
    {
        if (loadButton != null)
        {
            loadButton.onClick.RemoveAllListeners();
            loadButton.onClick.AddListener(LoadAssetFromUI);
            Debug.Log("Load Button Linked.");
        }
        else
        {
            Debug.LogWarning($"Load button not found on AssetLoaderDemo prefab instance '{name}'.");
        }

        if (subscribeButton != null)
        {
            subscribeButton.onClick.RemoveAllListeners();
            subscribeButton.onClick.AddListener(SubscribeToChannels);
            Debug.Log("Subscribe Button Linked.");
        }
        else
        {
            Debug.LogWarning($"Subscribe button not found on AssetLoaderDemo prefab instance '{name}'.");
        }

        if (exitDetailsButton != null)
        {
            exitDetailsButton.onClick.RemoveAllListeners();
            exitDetailsButton.onClick.AddListener(ExitDetailsView);
            Debug.Log("Exit Button Linked.");
        }
        else
        {
            Debug.LogWarning($"Exit button not found on AssetLoaderDemo prefab instance '{name}'.");
        }

        if (assetDropdown != null)
        {
            assetDropdown.onValueChanged.RemoveAllListeners();
            assetDropdown.onValueChanged.AddListener(OnAssetDropdownChanged);
        }
    }

    public void LoadAssetFromUI()
    {
        if (assetIdInput == null)
        {
            Debug.LogWarning("Please enter an Asset ID.");
            return;
        }

        string requested = GetRequestedAssetFromUi();
        if (string.IsNullOrEmpty(requested))
        {
            Debug.LogWarning("Please enter an Asset ID.");
            return;
        }

        Debug.Log($"Load button clicked for '{requested}'.");
        LoadAsset(requested);
        if (detailsPanel != null) detailsPanel.SetActive(true);
    }

    private string GetRequestedAssetFromUi()
    {
        if (assetDropdown != null && dropdownAssetIds.Count > 0)
        {
            int idx = Mathf.Clamp(assetDropdown.value, 0, dropdownAssetIds.Count - 1);
            return dropdownAssetIds[idx];
        }

        return assetIdInput != null && assetIdInput.text != null ? assetIdInput.text.Trim() : string.Empty;
    }

    private void OnAssetDropdownChanged(int idx)
    {
        if (idx < 0 || idx >= dropdownAssetIds.Count) return;
        if (assetIdInput != null)
            assetIdInput.text = dropdownAssetIds[idx];
    }

    public void RefreshAssetDropdown()
    {
        if (assetDropdown == null) return;

        if (dbConfig == null)
            dbConfig = Resources.Load<DBConfig>("DBConfig");

        if (dbConfig == null) return;

        string conString = $"Server={dbConfig.serverIp},{dbConfig.port};Database={dbConfig.database};User Id={dbConfig.userId};Password={dbConfig.password};TrustServerCertificate=True;";

        dropdownAssetIds.Clear();
        var labels = new List<Dropdown.OptionData>();

        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                string q = "SELECT ID, Name, Description FROM ASSETS ORDER BY ID";
                using (var cmd = new System.Data.SqlClient.SqlCommand(q, conn))
                using (var r = cmd.ExecuteReader())
                {
                    while (r.Read())
                    {
                        string id = r["ID"]?.ToString();
                        string name = r["Name"]?.ToString();
                        string description = r["Description"]?.ToString();
                        if (string.IsNullOrEmpty(id)) continue;
                        dropdownAssetIds.Add(id);
                        string label = !string.IsNullOrEmpty(description) ? description : name;
                        labels.Add(new Dropdown.OptionData(string.IsNullOrEmpty(label) ? id : $"{id} - {label}"));
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"RefreshAssetDropdown warning: {e.Message}");
        }

        assetDropdown.ClearOptions();
        if (labels.Count == 0)
        {
            labels.Add(new Dropdown.OptionData("No assets"));
        }
        assetDropdown.AddOptions(labels);
        assetDropdown.value = 0;
        assetDropdown.RefreshShownValue();

        if (dropdownAssetIds.Count > 0 && assetIdInput != null)
            assetIdInput.text = dropdownAssetIds[0];
    }

    public void LoadAsset(string assetId)
    {
        Debug.Log($"Loading Asset ID: {assetId}");
        if (dbConfig == null)
        {
            dbConfig = Resources.Load<DBConfig>("DBConfig");
        }

        if (dbConfig == null)
        {
            Debug.LogError("DBConfig is missing. Create/assign Assets/Resources/DBConfig.asset.");
            if (assetDisplayText) assetDisplayText.text = "Missing DBConfig";
            return;
        }

        if (mqttClient != null && mqttClient.IsConnected && previousDataPoint != null)
        {
            previousDataPoint.UnsubscribeChannels(mqttClient);
            previousDataPoint = null;
        }

        // Uses the connection parameters from your DBConfig asset
        string conString = $"Server={dbConfig.serverIp},{dbConfig.port};Database={dbConfig.database};User Id={dbConfig.userId};Password={dbConfig.password};TrustServerCertificate=True;";

        try 
        {
            string resolvedAssetId = ResolveAssetIdByName(conString, assetId);
            currentAsset = new Asset(resolvedAssetId);
            currentAsset.PopulateData(conString);

            if (string.IsNullOrEmpty(currentAsset.name))
            {
                int assetCount = GetAssetCount(conString);
                string available = GetAssetPreview(conString, 10);
                string msg = $"Asset not found: '{assetId}'\nDB: {dbConfig.database}\nASSETS rows: {assetCount}\nAvailable: {available}";
                if (assetDisplayText) assetDisplayText.text = msg;
                Debug.LogWarning(msg);
                return;
            }

            if (assetDisplayText)
            {
                currentRole = GetMessagingRole(currentAsset);
                assetDisplayText.text = $"<b>{currentAsset.name}</b>\n{currentAsset.description}\nRole: {GetRoleLabel(currentRole)}\nDatapoints: {currentAsset.dataPoints.Count}";
                SetColor(assetDisplayText, currentAsset.status);
            }

            ApplyRoleUiState();
            SetButtonLabel(loadButton, "Load");
            
            // Display list of datapoints
            if (datapointDisplayText)
            {
                datapointDisplayText.text = currentAsset.dataPoints.Count == 0
                    ? "<b>Datapoints:</b>\nNo datapoints found for this asset."
                    : "<b>Datapoints:</b>\nClick any datapoint button below or on the 3D map.";
            }

            if (channelsDisplayText)
                channelsDisplayText.text = "<b>Live Telemetry:</b>\nSelect a datapoint to view channel values.";

            if (filesDisplayText)
                filesDisplayText.text = "<b>Files:</b>\nSelect a datapoint to view related files.";

            // Create clickable buttons for datapoints
            CreateDatapointButtons();
            CreateSceneDatapointButtons();

            // Auto-subscribe on load using the first datapoint if available.
            AutoSubscribeOnLoad(conString);

            // Show related assets as buttons if applicable
            ShowRelatedAssetButtons(currentAsset.id, conString);
        }
        catch (Exception e)
        {
            Debug.LogError($"SQL Error: {e.Message}");
        }
    }

    private void CreateDatapointButtons()
    {
        if (detailsPanel == null || currentAsset == null) return;

        if (datapointListPanel == null)
            datapointListPanel = FindChildComponent<RectTransform>("DataPointListPanel");

        if (datapointListContent == null)
            datapointListContent = FindChildComponent<RectTransform>("DataPointListContent");

        var listParent = datapointListContent != null ? datapointListContent : datapointListPanel;
        if (listParent == null) return;

        // Clear previous buttons
        foreach (Transform child in listParent.transform)
        {
            if (child.name.StartsWith("DatapointButton_"))
            {
                Destroy(child.gameObject);
            }
        }

        float baseY = -8f;
        float rowHeight = 42f;
        float containerWidth = datapointListPanel != null ? datapointListPanel.rect.width : listParent.rect.width;
        float listWidth = Mathf.Max(220f, containerWidth - 20f);

        float contentHeight = Mathf.Max(datapointListPanel != null ? datapointListPanel.rect.height : 220f, (currentAsset.dataPoints.Count * rowHeight) + 12f);
        listParent.sizeDelta = new Vector2(listParent.sizeDelta.x, contentHeight);

        for (int i = 0; i < currentAsset.dataPoints.Count; i++)
        {
            var buttonGO = new GameObject($"DatapointButton_{i}");
            buttonGO.transform.SetParent(listParent, false);
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(0f, 1f);
            buttonRect.pivot = new Vector2(0f, 1f);
            buttonRect.anchoredPosition = new Vector2(6, baseY - (i * rowHeight));
            buttonRect.sizeDelta = new Vector2(listWidth, 38);

            var button = buttonGO.AddComponent<Button>();
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.42f, 0.58f, 0.42f, 1f); // dull green
            button.targetGraphic = image;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(listWidth - 10f, 32);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = $"{currentAsset.dataPoints[i].id} - {currentAsset.dataPoints[i].name}";
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            int index = i;
            button.onClick.AddListener(() => SelectDataPoint(index));
        }
    }

    private void CreateSceneDatapointButtons()
    {
        if (currentAsset == null)
            return;

        EnsureSceneImpressionPanel();
        if (sceneImpressionPanel == null)
            return;

        foreach (Transform child in sceneImpressionPanel.transform)
        {
            if (child.name.StartsWith("SceneDatapointButton_"))
                Destroy(child.gameObject);
        }

        var sortedPoints = currentAsset.dataPoints
            .OrderBy(dp => dp != null ? dp.id : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = 0; i < sortedPoints.Count; i++)
        {
            var buttonGO = new GameObject($"SceneDatapointButton_{i}");
            buttonGO.transform.SetParent(sceneImpressionPanel, false);
            var buttonRect = buttonGO.AddComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRect.pivot = new Vector2(0.5f, 0.5f);
            buttonRect.anchoredPosition = GetSceneDatapointAnchorPosition(i);
            buttonRect.sizeDelta = new Vector2(120, 30);

            var button = buttonGO.AddComponent<Button>();
            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.3f, 0.53f, 0.35f, 0.96f);
            button.targetGraphic = image;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(buttonGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchoredPosition = Vector2.zero;
            textRect.sizeDelta = new Vector2(112, 24);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = GetDatapointButtonLabel(sortedPoints[i]);
            text.fontSize = 10;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.Center;

            var selected = sortedPoints[i];
            int index = currentAsset.dataPoints.IndexOf(selected);
            button.onClick.AddListener(() => SelectDataPoint(index));
        }
    }

    private void EnsureSceneImpressionPanel()
    {
        if (sceneImpressionPanel != null)
            return;

        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
            return;

        var existing = canvas.transform.Find("SceneAssetImpressionPanel");
        if (existing != null)
        {
            sceneImpressionPanel = existing.GetComponent<RectTransform>();
            EnsureDraggablePanels();
            return;
        }

        var panelGO = new GameObject("SceneAssetImpressionPanel");
        panelGO.transform.SetParent(canvas.transform, false);
        sceneImpressionPanel = panelGO.AddComponent<RectTransform>();
        // Initial placement: centered and roughly half-screen size.
        sceneImpressionPanel.anchorMin = new Vector2(0.25f, 0.25f);
        sceneImpressionPanel.anchorMax = new Vector2(0.75f, 0.75f);
        sceneImpressionPanel.pivot = new Vector2(0.5f, 0.5f);
        sceneImpressionPanel.anchoredPosition = Vector2.zero;
        sceneImpressionPanel.offsetMin = Vector2.zero;
        sceneImpressionPanel.offsetMax = Vector2.zero;

        var panelImage = panelGO.AddComponent<Image>();
        panelImage.color = new Color(0.86f, 0.88f, 0.92f, 0.96f);

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panelGO.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(10, -8);
        titleRect.sizeDelta = new Vector2(360, 36);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "3D Asset Map (Demo)";
        titleText.fontSize = 18;
        titleText.color = new Color(0.12f, 0.15f, 0.25f, 1f);
        titleText.alignment = TextAlignmentOptions.Left;

        var deckGO = new GameObject("Deck");
        deckGO.transform.SetParent(panelGO.transform, false);
        var deckRect = deckGO.AddComponent<RectTransform>();
        deckRect.anchorMin = new Vector2(0f, 0f);
        deckRect.anchorMax = new Vector2(1f, 0f);
        deckRect.pivot = new Vector2(0.5f, 0f);
        deckRect.anchoredPosition = new Vector2(0, 14);
        deckRect.sizeDelta = new Vector2(-26, 46);
        var deckImage = deckGO.AddComponent<Image>();
        deckImage.color = new Color(0.67f, 0.71f, 0.78f, 0.92f);

        var bodyGO = new GameObject("Body");
        bodyGO.transform.SetParent(panelGO.transform, false);
        var bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        bodyRect.pivot = new Vector2(0.5f, 0.5f);
        bodyRect.anchoredPosition = new Vector2(0, 12);
        bodyRect.sizeDelta = new Vector2(320, 140);
        var bodyImage = bodyGO.AddComponent<Image>();
        bodyImage.color = new Color(0.74f, 0.78f, 0.85f, 0.92f);

        EnsureDraggablePanels();
    }

    private Vector2 GetSceneDatapointAnchorPosition(int index)
    {
        const int columns = 3;
        const float spacingX = 132f;
        const float spacingY = 52f;
        const float topOffset = 56f;

        int col = index % columns;
        int row = index / columns;

        float startX = -((columns - 1) * spacingX) * 0.5f;
        float x = startX + (col * spacingX);
        float y = topOffset - (row * spacingY);
        return new Vector2(x, y);
    }

    public void SelectDataPoint(int index)
    {
        if (currentAsset == null || index < 0 || index >= currentAsset.dataPoints.Count) return;

        string conString = $"Server={dbConfig.serverIp},{dbConfig.port};Database={dbConfig.database};User Id={dbConfig.userId};Password={dbConfig.password};TrustServerCertificate=True;";

        if (mqttClient != null && mqttClient.IsConnected && previousDataPoint != null)
        {
            previousDataPoint.UnsubscribeChannels(mqttClient);
        }
        
        currentDataPoint = currentAsset.dataPoints[index];
        currentDataPoint.PopulateItems(conString, currentAsset.id, dbConfig);
        previousDataPoint = currentDataPoint;
        SetButtonLabel(loadButton, "Load");

        RenderCurrentDataPointDetails();

        EnsureMqttConnected();
        if (mqttClient != null && mqttClient.IsConnected && currentRole != AssetMessagingRole.Publisher)
            currentDataPoint.SubscribeChannels(mqttClient);

        ShowDatapointPopup();
    }

    private void AutoSubscribeOnLoad(string conString)
    {
        if (currentAsset == null || currentAsset.dataPoints.Count == 0) return;

        if (mqttClient != null && mqttClient.IsConnected && previousDataPoint != null)
        {
            previousDataPoint.UnsubscribeChannels(mqttClient);
        }

        var first = currentAsset.dataPoints[0];
        first.PopulateItems(conString, currentAsset.id, dbConfig);
        previousDataPoint = first;
        currentDataPoint = first;

        EnsureMqttConnected();
        if (mqttClient != null && mqttClient.IsConnected && currentRole != AssetMessagingRole.Publisher)
        {
            first.SubscribeChannels(mqttClient);
            Debug.Log($"Auto-subscribed on load to datapoint '{first.name}'.");
        }

        RenderCurrentDataPointDetails();

        // Also open the datapoint details pop-out for the first datapoint on load.
        ShowDatapointPopup();
    }

    private void RenderCurrentDataPointDetails()
    {
        if (currentDataPoint == null)
            return;

        if (datapointDisplayText)
            datapointDisplayText.text = "<b>Datapoints:</b>\nClick any datapoint button below or on the 3D map.";

        if (channelsDisplayText)
        {
            string channelText = "<b>Live Telemetry:</b>\n";
            if (currentDataPoint.channels.Count == 0)
            {
                channelText += "No channels";
            }
            else
            {
                foreach (var ch in currentDataPoint.channels)
                {
                    channelText += $"• {ch.name}: {ch.value}\n";
                }
            }

            channelText += "\n<b>" + GetTopicSectionTitle(currentRole) + ":</b>\n";
            if (currentDataPoint.channels.Count == 0)
            {
                channelText += "none\n";
            }
            else
            {
                foreach (var ch in currentDataPoint.channels)
                {
                    if (!string.IsNullOrEmpty(ch.target))
                        channelText += $"• {ch.target}  =>  {ch.value}\n";
                }
            }
            channelsDisplayText.text = channelText;
        }

        if (filesDisplayText)
        {
            filesDisplayText.text = currentDataPoint.files.Count == 0
                ? "<b>Files:</b>\nNo files"
                : "<b>Files:</b>\nClick a document:";

            EnsureFilesDisplayLinksPanel();
            RenderFilesDisplayLinks();
        }
    }

    private void EnsureFilesDisplayLinksPanel()
    {
        if (filesDisplayText == null || filesDisplayLinksPanel != null)
            return;

        var parent = filesDisplayText.rectTransform.parent as RectTransform;
        if (parent == null)
            return;

        var panelGO = new GameObject("FileLinksPanel");
        panelGO.transform.SetParent(parent, false);
        filesDisplayLinksPanel = panelGO.AddComponent<RectTransform>();
        filesDisplayLinksPanel.anchorMin = new Vector2(0f, 1f);
        filesDisplayLinksPanel.anchorMax = new Vector2(0f, 1f);
        filesDisplayLinksPanel.pivot = new Vector2(0f, 1f);
        filesDisplayLinksPanel.anchoredPosition = new Vector2(0f, -SectionHeaderContentOffset);
        filesDisplayLinksPanel.sizeDelta = new Vector2(Mathf.Max(320f, parent.rect.width), Mathf.Max(130f, parent.rect.height - SectionHeaderContentOffset));
    }

    private void RenderFilesDisplayLinks()
    {
        if (filesDisplayLinksPanel == null)
            return;

        foreach (Transform child in filesDisplayLinksPanel)
        {
            if (child.name.StartsWith("FileLinkButton_"))
                Destroy(child.gameObject);
        }

        if (currentDataPoint == null || currentDataPoint.files == null)
            return;

        float rowHeight = 34f;
        float width = Mathf.Max(320f, filesDisplayLinksPanel.rect.width - 10f);
        float minHeight = Mathf.Max(130f, (currentDataPoint.files.Count * rowHeight) + 8f);
        filesDisplayLinksPanel.sizeDelta = new Vector2(filesDisplayLinksPanel.sizeDelta.x, minHeight);

        for (int i = 0; i < currentDataPoint.files.Count; i++)
        {
            var file = currentDataPoint.files[i];
            if (file == null || string.IsNullOrWhiteSpace(file.link))
                continue;

            var btnGO = new GameObject($"FileLinkButton_{i}");
            btnGO.transform.SetParent(filesDisplayLinksPanel, false);
            var rect = btnGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f - (i * rowHeight));
            rect.sizeDelta = new Vector2(width, 30f);

            var image = btnGO.AddComponent<Image>();
            image.color = LinkButtonColor;
            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = image;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.text = string.IsNullOrWhiteSpace(file.name) ? "Open document" : file.name;

            string link = file.link;
            string display = string.IsNullOrWhiteSpace(file.name) ? file.id : file.name;
            button.onClick.AddListener(() => OpenDataFileLink(link, display));
        }
    }

    private void ShowRelatedAssetButtons(string assetId, string conString)
    {
        if (detailsPanel == null) return;

        // Clear previous related asset buttons
        foreach (Transform child in detailsPanel.transform)
        {
            if (child.name.StartsWith("RelatedAssetButton_"))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void LoadRelatedAsset(string assetId)
    {
        LoadAsset(assetId);
        if (currentAsset != null && currentAsset.dataPoints.Count > 0)
            SelectDataPoint(0);
    }

    private void EnsureMqttConnected()
    {
        if (mqttClient != null && mqttClient.IsConnected) return;

        string mqttHost = !string.IsNullOrEmpty(dbConfig.mqttServerIp) ? dbConfig.mqttServerIp : dbConfig.serverIp;
        mqttClient = new MqttClient(mqttHost, dbConfig.mqttPort, false, null, null, MqttSslProtocols.None);
        mqttClient.MqttMsgPublishReceived += (s, e) =>
        {
            string msg = Encoding.UTF8.GetString(e.Message);
            Debug.Log($"MQTT Pulse: {e.Topic} >> {msg}");
        };
        mqttClient.Connect(Guid.NewGuid().ToString());
        Debug.Log($"Connected to MQTT broker at {mqttHost}:{dbConfig.mqttPort}");
    }

    private void EnsureDatapointPopupPanel()
    {
        if (detailsPanel == null || datapointPopupPanel != null) return;

        datapointPopupPanel = new GameObject("DatapointPopupPanel");
        datapointPopupPanel.transform.SetParent(detailsPanel.transform, false);

        var popupRect = datapointPopupPanel.AddComponent<RectTransform>();
        popupRect.anchorMin = new Vector2(0f, 1f);
        popupRect.anchorMax = new Vector2(0f, 1f);
        popupRect.pivot = new Vector2(0f, 1f);
        popupRect.anchoredPosition = new Vector2(18, -68);
        popupRect.sizeDelta = new Vector2(560, 760);

        var popupBg = datapointPopupPanel.AddComponent<Image>();
        popupBg.color = new Color(1f, 1f, 1f, 0.97f);

        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(datapointPopupPanel.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(12, -12);
        titleRect.sizeDelta = new Vector2(420, 34);
        var titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = "Datapoint Details";
        titleText.fontSize = 20;
        titleText.color = Color.black;

        var closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(datapointPopupPanel.transform, false);
        var closeRect = closeGO.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(0f, 1f);
        closeRect.anchorMax = new Vector2(0f, 1f);
        closeRect.pivot = new Vector2(0f, 1f);
        closeRect.anchoredPosition = new Vector2(516, -12);
        closeRect.sizeDelta = new Vector2(32, 30);
        var closeBtn = closeGO.AddComponent<Button>();
        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        closeBtn.targetGraphic = closeImg;
        var closeTextGO = new GameObject("Text");
        closeTextGO.transform.SetParent(closeGO.transform, false);
        var closeTextRect = closeTextGO.AddComponent<RectTransform>();
        closeTextRect.anchoredPosition = Vector2.zero;
        closeTextRect.sizeDelta = new Vector2(26, 24);
        var closeText = closeTextGO.AddComponent<TextMeshProUGUI>();
        closeText.text = "X";
        closeText.fontSize = 14;
        closeText.color = Color.black;
        closeText.alignment = TextAlignmentOptions.Center;
        closeBtn.onClick.AddListener(HideDatapointPopup);

        datapointPopupSummaryText = CreatePopupSectionText(
            datapointPopupPanel.transform,
            "SummarySection",
            "Datapoint",
            new Vector2(14, -56),
            new Vector2(532, 140),
            out _);

        datapointPopupTelemetryText = CreatePopupSectionText(
            datapointPopupPanel.transform,
            "TelemetrySection",
            "Telemetry",
            new Vector2(14, -208),
            new Vector2(532, 266),
            out _);

        CreatePopupSectionText(
            datapointPopupPanel.transform,
            "DocumentsSection",
            "Documents",
            new Vector2(14, -486),
            new Vector2(532, 250),
            out datapointPopupFilesPanel);

        datapointPopupPanel.SetActive(false);
        EnsureDraggablePanels();
    }

    private TMP_Text CreatePopupSectionText(Transform parent, string sectionName, string header, Vector2 position, Vector2 size, out RectTransform contentRect)
    {
        var sectionGO = new GameObject(sectionName);
        sectionGO.transform.SetParent(parent, false);
        var sectionRect = sectionGO.AddComponent<RectTransform>();
        sectionRect.anchorMin = new Vector2(0f, 1f);
        sectionRect.anchorMax = new Vector2(0f, 1f);
        sectionRect.pivot = new Vector2(0f, 1f);
        sectionRect.anchoredPosition = position;
        sectionRect.sizeDelta = size;

        var sectionImage = sectionGO.AddComponent<Image>();
        sectionImage.color = new Color(0.94f, 0.95f, 0.97f, 0.98f);

        var headerGO = new GameObject("Header");
        headerGO.transform.SetParent(sectionGO.transform, false);
        var headerRect = headerGO.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, 0f);
        headerRect.sizeDelta = new Vector2(-16f, 30f);
        var headerText = headerGO.AddComponent<TextMeshProUGUI>();
        headerText.text = header;
        headerText.fontSize = 15;
        headerText.color = new Color(0.16f, 0.2f, 0.28f, 1f);
        headerText.alignment = TextAlignmentOptions.Left;

        var dividerGO = new GameObject("Divider");
        dividerGO.transform.SetParent(sectionGO.transform, false);
        var dividerRect = dividerGO.AddComponent<RectTransform>();
        dividerRect.anchorMin = new Vector2(0f, 1f);
        dividerRect.anchorMax = new Vector2(1f, 1f);
        dividerRect.pivot = new Vector2(0.5f, 1f);
        dividerRect.anchoredPosition = new Vector2(0f, -34f);
        dividerRect.sizeDelta = new Vector2(-12f, 2f);
        var dividerImage = dividerGO.AddComponent<Image>();
        dividerImage.color = new Color(0.75f, 0.78f, 0.84f, 1f);

        var viewportGO = new GameObject("Viewport");
        viewportGO.transform.SetParent(sectionGO.transform, false);
        var viewportRect = viewportGO.AddComponent<RectTransform>();
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.offsetMin = new Vector2(10f, 10f);
        viewportRect.offsetMax = new Vector2(-28f, -SectionHeaderContentOffset);
        var viewportImage = viewportGO.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        var viewportMask = viewportGO.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        var scrollRect = sectionGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = true;
        scrollRect.viewport = viewportRect;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 18f;

        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(viewportGO.transform, false);
        contentRect = contentGO.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(0f, 1f);
        contentRect.pivot = new Vector2(0f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(size.x * 1.8f, size.y * 2.2f);

        scrollRect.content = contentRect;

        var contentText = contentGO.AddComponent<TextMeshProUGUI>();
        contentText.fontSize = 14;
        contentText.color = Color.black;
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.textWrappingMode = TextWrappingModes.NoWrap;
        contentText.overflowMode = TextOverflowModes.Masking;
        contentText.rectTransform.anchorMin = new Vector2(0f, 1f);
        contentText.rectTransform.anchorMax = new Vector2(0f, 1f);
        contentText.rectTransform.pivot = new Vector2(0f, 1f);
        contentText.rectTransform.anchoredPosition = Vector2.zero;
        contentText.rectTransform.sizeDelta = contentRect.sizeDelta;

        var verticalScrollbarGO = new GameObject("ScrollbarV");
        verticalScrollbarGO.transform.SetParent(sectionGO.transform, false);
        var vRect = verticalScrollbarGO.AddComponent<RectTransform>();
        vRect.anchorMin = new Vector2(1f, 0f);
        vRect.anchorMax = new Vector2(1f, 1f);
        vRect.pivot = new Vector2(1f, 0.5f);
        vRect.sizeDelta = new Vector2(12f, -58f);
        vRect.anchoredPosition = new Vector2(-6f, -16f);
        var vImage = verticalScrollbarGO.AddComponent<Image>();
        vImage.color = new Color(0.78f, 0.8f, 0.84f, 1f);
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
        horizontalScrollbarGO.transform.SetParent(sectionGO.transform, false);
        var hRect = horizontalScrollbarGO.AddComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0f, 0f);
        hRect.anchorMax = new Vector2(1f, 0f);
        hRect.pivot = new Vector2(0.5f, 0f);
        hRect.sizeDelta = new Vector2(-28f, 12f);
        hRect.anchoredPosition = new Vector2(-8f, 6f);
        var hImage = horizontalScrollbarGO.AddComponent<Image>();
        hImage.color = new Color(0.78f, 0.8f, 0.84f, 1f);
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

        // Keep header visuals above scrolling content so text never overlays the header area.
        dividerGO.transform.SetAsLastSibling();
        headerGO.transform.SetAsLastSibling();
        return contentText;
    }

    private void ShowDatapointPopup()
    {
        if (detailsPanel != null && !detailsPanel.activeSelf)
            detailsPanel.SetActive(true);

        EnsureDatapointPopupPanel();
        if (datapointPopupPanel == null || datapointPopupSummaryText == null || datapointPopupTelemetryText == null || currentDataPoint == null)
            return;

        var summary = new StringBuilder();
        summary.AppendLine($"<b>{currentDataPoint.name}</b>");
        summary.AppendLine(currentDataPoint.description);
        summary.AppendLine();
        summary.AppendLine($"Channels: {currentDataPoint.channels.Count}");
        summary.AppendLine($"Documents: {currentDataPoint.files.Count}");

        var telemetry = new StringBuilder();
        if (currentDataPoint.channels.Count == 0)
        {
            telemetry.AppendLine("No telemetry channels.");
        }
        else
        {
            foreach (var ch in currentDataPoint.channels)
            {
                telemetry.AppendLine($"• {ch.name}: {ch.value}");
            }
        }

        datapointPopupSummaryText.text = summary.ToString();
        datapointPopupTelemetryText.text = telemetry.ToString();
        RenderDatapointFileButtons();
        datapointPopupPanel.transform.SetAsLastSibling();
        datapointPopupPanel.SetActive(true);
        Debug.Log($"Datapoint pop-out opened for '{currentDataPoint.name}'.");
    }

    private void RenderDatapointFileButtons()
    {
        if (datapointPopupFilesPanel == null)
            return;

        foreach (Transform child in datapointPopupFilesPanel)
            Destroy(child.gameObject);

        if (currentDataPoint == null || currentDataPoint.files == null || currentDataPoint.files.Count == 0)
            return;

        int maxButtons = currentDataPoint.files.Count;
        float rowHeight = 42f;
        for (int i = 0; i < maxButtons; i++)
        {
            var file = currentDataPoint.files[i];
            if (file == null || string.IsNullOrWhiteSpace(file.link))
                continue;

            var btnGO = new GameObject($"FileOpenButton_{i}");
            btnGO.transform.SetParent(datapointPopupFilesPanel, false);
            var rect = btnGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(0f, -4f - (i * rowHeight));
            rect.sizeDelta = new Vector2(512f, 36f);

            var image = btnGO.AddComponent<Image>();
            image.color = LinkButtonColor;

            var button = btnGO.AddComponent<Button>();
            button.targetGraphic = image;

            var textGO = new GameObject("Text");
            textGO.transform.SetParent(btnGO.transform, false);
            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 2f);
            textRect.offsetMax = new Vector2(-6f, -2f);

            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.fontSize = 14;
            text.color = Color.white;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.text = string.IsNullOrWhiteSpace(file.name) ? "Open document" : file.name;

            string link = file.link;
            string display = string.IsNullOrWhiteSpace(file.name) ? file.id : file.name;
            button.onClick.AddListener(() => OpenDataFileLink(link, display));
        }
    }

    private void OpenDataFileLink(string resolvedLink, string displayName)
    {
        if (string.IsNullOrWhiteSpace(resolvedLink))
            return;

        try
        {
            string trimmed = resolvedLink.Trim();

            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absUri)
                && (absUri.Scheme == Uri.UriSchemeHttp || absUri.Scheme == Uri.UriSchemeHttps || absUri.Scheme == Uri.UriSchemeFtp))
            {
                Application.OpenURL(absUri.AbsoluteUri);
                Debug.Log($"Opening file link '{displayName}': {absUri.AbsoluteUri}");
                return;
            }

            if (trimmed.StartsWith("\\\\", StringComparison.Ordinal) || Path.IsPathRooted(trimmed))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(trimmed) { UseShellExecute = true });
                Debug.Log($"Opening server file '{displayName}': {trimmed}");
                return;
            }

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(trimmed) { UseShellExecute = true });
            Debug.Log($"Opening link '{displayName}': {trimmed}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Failed to open link for '{displayName}': {ex.Message}");
        }
    }

    private void HideDatapointPopup()
    {
        if (datapointPopupPanel != null)
            datapointPopupPanel.SetActive(false);
    }

    private void ExitDetailsView()
    {
        if (mqttClient != null && mqttClient.IsConnected)
        {
            if (previousDataPoint != null)
                previousDataPoint.UnsubscribeChannels(mqttClient);
            else if (currentDataPoint != null)
                currentDataPoint.UnsubscribeChannels(mqttClient);
        }

        previousDataPoint = null;
        currentDataPoint = null;
        HideDatapointPopup();
        if (detailsPanel != null)
            detailsPanel.SetActive(false);

        SetButtonLabel(loadButton, "Load");
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;
        var text = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (text != null)
            text.text = string.IsNullOrEmpty(label) ? "Load" : label;
    }

    private bool AssetMatchesSearchTerm(Asset asset, string term)
    {
        if (asset == null || string.IsNullOrEmpty(term))
            return false;

        return StringContains(asset.id, term)
            || StringContains(asset.name, term)
            || StringContains(asset.description, term);
    }

    private bool StringContains(string value, string term)
    {
        return !string.IsNullOrEmpty(value)
            && value.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private string ResolveAssetIdByName(string conString, string input)
    {
        if (string.IsNullOrEmpty(input)) return input;

        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                string q = "SELECT TOP 1 ID FROM ASSETS WHERE ID = @input OR Name = @input OR Description LIKE @description ORDER BY CASE WHEN ID = @input THEN 0 WHEN Name = @input THEN 1 ELSE 2 END";
                using (var cmd = new System.Data.SqlClient.SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@input", input);
                    cmd.Parameters.AddWithValue("@description", "%" + input + "%");
                    var result = cmd.ExecuteScalar();
                    if (result != null)
                        return result.ToString();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"ResolveAssetIdByName warning: {e.Message}");
        }

        return input;
    }

    private int GetAssetCount(string conString)
    {
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                using (var cmd = new System.Data.SqlClient.SqlCommand("SELECT COUNT(1) FROM ASSETS", conn))
                {
                    object result = cmd.ExecuteScalar();
                    return (result != null) ? Convert.ToInt32(result) : 0;
                }
            }
        }
        catch
        {
            return -1;
        }
    }

    private string GetAssetPreview(string conString, int limit)
    {
        try
        {
            using (var conn = new System.Data.SqlClient.SqlConnection(conString))
            {
                conn.Open();
                string q = "SELECT TOP (@limit) ID, Name FROM ASSETS ORDER BY ID";
                using (var cmd = new System.Data.SqlClient.SqlCommand(q, conn))
                {
                    cmd.Parameters.AddWithValue("@limit", limit);
                    using (var r = cmd.ExecuteReader())
                    {
                        var items = new List<string>();
                        while (r.Read())
                        {
                            string id = r["ID"]?.ToString();
                            string name = r["Name"]?.ToString();
                            if (string.IsNullOrEmpty(name))
                                items.Add(id);
                            else
                                items.Add($"{id} ({name})");
                        }

                        if (items.Count == 0) return "none";
                        return string.Join(", ", items);
                    }
                }
            }
        }
        catch
        {
            return "unavailable";
        }
    }

    private string GetDatapointButtonLabel(DataPoint dp)
    {
        if (dp == null) return "Datapoint";

        // Prefer compact IDs for tight portrait list buttons.
        if (!string.IsNullOrEmpty(dp.id))
        {
            return dp.id.Length > 12 ? dp.id.Substring(0, 12) : dp.id;
        }

        if (string.IsNullOrEmpty(dp.name)) return "Datapoint";
        return dp.name.Length > 18 ? dp.name.Substring(0, 18) + "..." : dp.name;
    }

    public void SubscribeToChannels()
    {
        try
        {
            if (currentRole == AssetMessagingRole.Publisher)
            {
                Debug.Log("Current asset is publish-only for this demo. Subscribe action skipped.");
                return;
            }

            EnsureMqttConnected();

            // subscribe to all channels of selected data point, or if none selected, all channels of asset
            if (currentDataPoint != null)
            {
                currentDataPoint.SubscribeChannels(mqttClient);
            }
            else if (currentAsset != null)
            {
                foreach (var dp in currentAsset.dataPoints)
                {
                    dp.SubscribeChannels(mqttClient);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"MQTT Error: {e.Message}");
        }
    }

    void Update()
    {
        if (currentDataPoint == null) return;

        lastDisplayUpdateTime += Time.deltaTime;
        if (lastDisplayUpdateTime < DisplayUpdateInterval) return;

        lastDisplayUpdateTime = 0;

        if (channelsDisplayText)
        {
            string channelText = "<b>Live Telemetry:</b>\n";
            foreach (var ch in currentDataPoint.channels)
            {
                channelText += $"• {ch.name}: {ch.value}\n";
            }

            channelText += "\n<b>" + GetTopicSectionTitle(currentRole) + ":</b>\n";
            foreach (var ch in currentDataPoint.channels)
            {
                if (!string.IsNullOrEmpty(ch.target))
                    channelText += $"• {ch.target}  =>  {ch.value}\n";
            }

            channelsDisplayText.text = channelText;
        }
    }

    private void SetColor(TMP_Text textElement, StatusType status)
    {
        if (textElement == null) return;
        switch (status)
        {
            case StatusType.Online: textElement.color = onlineColor; break;
            case StatusType.Alarm: textElement.color = alarmColor; break;
            default: textElement.color = normalColor; break;
        }
    }

    void OnDestroy()
    {
        if (mqttClient != null && mqttClient.IsConnected && previousDataPoint != null)
            previousDataPoint.UnsubscribeChannels(mqttClient);

        if (mqttClient != null && mqttClient.IsConnected) mqttClient.Disconnect();
    }

    private AssetMessagingRole GetMessagingRole(Asset asset)
    {
        if (asset == null)
            return AssetMessagingRole.Both;

        if (asset.messagingRoleCode.HasValue)
        {
            switch (asset.messagingRoleCode.Value)
            {
                case 0: return AssetMessagingRole.Subscriber;
                case 1: return AssetMessagingRole.Publisher;
                case 2: return AssetMessagingRole.Both;
            }
        }

        if (!string.IsNullOrEmpty(asset.messagingRole))
        {
            string dbRole = asset.messagingRole.Trim().ToLowerInvariant();
            if (dbRole == "publish" || dbRole == "publisher" || dbRole == "pub")
                return AssetMessagingRole.Publisher;
            if (dbRole == "subscribe" || dbRole == "subscriber" || dbRole == "sub")
                return AssetMessagingRole.Subscriber;
            if (dbRole == "both" || dbRole == "pubsub" || dbRole == "publish+subscribe")
                return AssetMessagingRole.Both;
        }

        string blob = $"{asset.id} {asset.name} {asset.description}".ToLowerInvariant();

        if (blob.Contains("pressure01") || blob.Contains("pressure sensor") || blob.Contains("sensor"))
            return AssetMessagingRole.Publisher;

        if (blob.Contains("leaktest") || blob.Contains("leak test"))
            return AssetMessagingRole.Subscriber;

        return AssetMessagingRole.Both;
    }

    private string GetRoleLabel(AssetMessagingRole role)
    {
        switch (role)
        {
            case AssetMessagingRole.Publisher: return "Publish Only";
            case AssetMessagingRole.Subscriber: return "Subscribe";
            default: return "Publish + Subscribe";
        }
    }

    private string GetTopicSectionTitle(AssetMessagingRole role)
    {
        switch (role)
        {
            case AssetMessagingRole.Publisher: return "Published Topics";
            case AssetMessagingRole.Subscriber: return "Subscribed Topics";
            default: return "Published + Subscribed Topics";
        }
    }

    private void ApplyRoleUiState()
    {
        if (subscribeButton == null)
            return;

        var text = subscribeButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (currentRole == AssetMessagingRole.Publisher)
        {
            subscribeButton.interactable = false;
            if (text != null) text.text = "Pub";
        }
        else
        {
            subscribeButton.interactable = true;
            if (text != null) text.text = "ReSub";
        }
    }
}