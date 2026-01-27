using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using FallGuys.Networking;
using UnityEngine.EventSystems;

/// <summary>
/// FACTORY FRENZY UI GENERATOR
/// This script builds the entire Menu UI from scratch to ensure 100% functionality.
/// </summary>
public class LobbyUIFactory : EditorWindow
{
    [MenuItem("Tools/FallGuys/🚀 GENERATE FULL UI (Clean & Linked) 🚀")]
    public static void GenerateFullUI()
    {
        // 0. SAFETY CHECK: Ask before deleting
        if (!EditorUtility.DisplayDialog("GENERATE UI", 
            "This will DELETE 'MainCanvas' and 'LobbyUI' settings to rebuild everything from scratch.\n\nAre you sure?", 
            "YES, DO IT", "Cancel")) return;

        // 1. FIND MANAGER & AUTO-SETUP
        LobbyManager mgr = FindFirstObjectByType<LobbyManager>();
        if (mgr == null)
        {
            Debug.LogError("❌ No 'LobbyManager' found in scene! Please create an empty object and add 'LobbyManager' script.");
            return;
        }

        // Ensure LobbyUI exists
        LobbyUI lobbyUI = mgr.GetComponent<LobbyUI>();
        if (lobbyUI == null)
        {
            lobbyUI = mgr.gameObject.AddComponent<LobbyUI>();
            Undo.RegisterCreatedObjectUndo(lobbyUI, "Add LobbyUI");
        }
        
        // Ensure LobbyListUI exists (for browser)
        LobbyListUI listUI = mgr.GetComponent<LobbyListUI>();
        if (listUI == null)
        {
            listUI = mgr.gameObject.AddComponent<LobbyListUI>();
            Undo.RegisterCreatedObjectUndo(listUI, "Add LobbyListUI");
        }

        // Ensure LanDiscoveryManager exists (CRITICAL for List to work)
        LanDiscoveryManager discovery = mgr.GetComponent<LanDiscoveryManager>();
        if (discovery == null)
        {
             discovery = mgr.gameObject.AddComponent<LanDiscoveryManager>();
             Undo.RegisterCreatedObjectUndo(discovery, "Add LanDiscoveryManager");
        }

        // 2. ENSURE EVENT SYSTEM (Crucial for buttons)
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // 3. CANVAS SETUP
        // We look for existing MainCanvas or create new
        GameObject canvasObj = GameObject.Find("MainCanvas");
        if (canvasObj != null) 
        {
            Undo.DestroyObjectImmediate(canvasObj); // Nuke old one to be sure
        }
        
        canvasObj = new GameObject("MainCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObj.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObj.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Canvas");


        // ===================================================================================
        // 4. CONNECTION PANEL
        // ===================================================================================
        GameObject connPanel = CreatePanel(canvasObj, "ConnectionPanel", new Color(0.2f, 0.2f, 0.4f, 1f));
        
        // Title
        CreateText(connPanel, "MENU PRINCIPAL", 50, true, -1, 100);

        // Buttons
        GameObject btnHost = CreateButton(connPanel, "HOST (CRÉER)", Color.green);
        GameObject btnJoin = CreateButton(connPanel, "REJOINDRE (DIRECT)", Color.cyan);
        GameObject btnBrowser = CreateButton(connPanel, "LISTE DES SERVEURS", new Color(1f, 0.8f, 0f)); // Gold

        // Inputs Group
        GameObject inputsGroup = CreateContainer(connPanel, "Inputs", true, 20); // Horizontal
        LayoutElement leInputs = inputsGroup.AddComponent<LayoutElement>();
        leInputs.minHeight = 80; leInputs.preferredHeight = 80; // Force height

        GameObject inputIP = CreateInput(inputsGroup, "ADRESSE IP", "127.0.0.1");
        GameObject inputPort = CreateInput(inputsGroup, "PORT", "7777");

        // ===================================================================================
        // 5. LOBBY PANEL
        // ===================================================================================
        GameObject lobbyPanel = CreatePanel(canvasObj, "LobbyPanel", new Color(0.1f, 0.3f, 0.1f, 1f));
        lobbyPanel.SetActive(false); // Hidden by default
        
        CreateText(lobbyPanel, "SALON D'ATTENTE", 50, true, -1, 80);
        GameObject txtCountdown = CreateText(lobbyPanel, "En attente...", 36, false, -1, 50);

        // Player List
        GameObject playerList = CreateContainer(lobbyPanel, "PlayerListContainer", false, 5);
        // Add background to list for visibility
        Image plBg = playerList.AddComponent<Image>();
        plBg.color = new Color(0,0,0,0.3f);
        LayoutElement leList = playerList.AddComponent<LayoutElement>();
        leList.flexibleHeight = 1; // Fill center space

        // Bottom Buttons
        GameObject lobbyBtns = CreateContainer(lobbyPanel, "BottomButtons", true, 50);
        LayoutElement leLobbyBtns = lobbyBtns.AddComponent<LayoutElement>();
        leLobbyBtns.minHeight = 80; leLobbyBtns.preferredHeight = 80;

        GameObject btnReady = CreateButton(lobbyBtns, "PRÊT", Color.green);
        GameObject btnLeave = CreateButton(lobbyBtns, "QUITTER", Color.red);

        // ===================================================================================
        // 6. BROWSER PANEL
        // ===================================================================================
        GameObject browserPanel = CreatePanel(canvasObj, "BrowserPanel", new Color(0.1f, 0.1f, 0.2f, 1f));
        browserPanel.SetActive(false);

        CreateText(browserPanel, "SERVEURS DISPONIBLES", 40, true, -1, 60);

        // Server List Container
        GameObject serverList = CreateContainer(browserPanel, "ServerList", false, 5);
        
        // VISUAL FIX: Add background so user sees the "box"
        Image slBg = serverList.AddComponent<Image>();
        slBg.color = new Color(0, 0, 0, 0.3f);
        
        LayoutElement leServerList = serverList.AddComponent<LayoutElement>();
        leServerList.flexibleHeight = 1;

        GameObject btnBack = CreateButton(browserPanel, "RETOUR", Color.red);
        LayoutElement leBack = btnBack.GetComponent<LayoutElement>();
        leBack.flexibleHeight = 0; leBack.preferredHeight = 60;

        // Loading Panel (Minimal)
        GameObject loadingPanel = CreatePanel(canvasObj, "LoadingPanel", Color.black);
        CreateText(loadingPanel, "CHARGEMENT...", 60, true);
        loadingPanel.SetActive(false);


        // ===================================================================================
        // 7. PREFABS CREATION (Hidden in Manager)
        // ===================================================================================
        GameObject playerRowPrefab = CreatePrefab(lobbyUI.gameObject, "PlayerRowPrefab", false);
        GameObject serverRowPrefab = CreatePrefab(lobbyUI.gameObject, "ServerRowPrefab", true);


        // ===================================================================================
        // 8. LINKING EVERYTHING (The Magic)
        // ===================================================================================
        SerializedObject sLobby = new SerializedObject(lobbyUI);

        // Panels
        sLobby.FindProperty("_connectionPanel").objectReferenceValue = connPanel;
        sLobby.FindProperty("_lobbyPanel").objectReferenceValue = lobbyPanel;
        sLobby.FindProperty("_browserPanel").objectReferenceValue = browserPanel;
        sLobby.FindProperty("_loadingPanel").objectReferenceValue = loadingPanel;

        // Connection
        sLobby.FindProperty("_hostButton").objectReferenceValue = btnHost.GetComponent<Button>();
        sLobby.FindProperty("_clientButton").objectReferenceValue = btnJoin.GetComponent<Button>();
        sLobby.FindProperty("_browserButton").objectReferenceValue = btnBrowser.GetComponent<Button>();
        sLobby.FindProperty("_ipInput").objectReferenceValue = inputIP.GetComponentInChildren<TMP_InputField>();
        sLobby.FindProperty("_portInput").objectReferenceValue = inputPort.GetComponentInChildren<TMP_InputField>();

        // Lobby
        sLobby.FindProperty("_playerListContainer").objectReferenceValue = playerList.transform;
        sLobby.FindProperty("_countdownText").objectReferenceValue = txtCountdown.GetComponent<TextMeshProUGUI>();
        sLobby.FindProperty("_readyButton").objectReferenceValue = btnReady.GetComponent<Button>();
        sLobby.FindProperty("_readyButtonText").objectReferenceValue = btnReady.GetComponentInChildren<TextMeshProUGUI>();
        sLobby.FindProperty("_leaveButton").objectReferenceValue = btnLeave.GetComponent<Button>();
        sLobby.FindProperty("_playerRowPrefab").objectReferenceValue = playerRowPrefab;

        // Browser
        sLobby.FindProperty("_backToMenuButton").objectReferenceValue = btnBack.GetComponent<Button>();

        // List UI needs separate linking
        if (listUI != null)
        {
            SerializedObject sList = new SerializedObject(listUI);
            sList.FindProperty("_container").objectReferenceValue = serverList.transform;
            sList.FindProperty("_lobbyEntryPrefab").objectReferenceValue = serverRowPrefab;
            sList.ApplyModifiedProperties();
        }

        sLobby.ApplyModifiedProperties();

        Debug.Log("✅✅ UI FACTORY: GENERATION COMPLETE & LINKED! ✅✅");
    }

    // ---------------------------------------------------------
    // HELPERS
    // ---------------------------------------------------------

    private static GameObject CreatePanel(GameObject parent, string name, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        Stretch(go);
        
        Image img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = true; // Block clicks passing through

        VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(50, 50, 50, 50);
        vlg.spacing = 20;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false; // Important: Elements define their height
        vlg.childForceExpandHeight = false;

        return go;
    }

    private static GameObject CreateContainer(GameObject parent, string name, bool horizontal, float spacing)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent.transform, false);
        
        if (horizontal)
        {
            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = spacing;
            hlg.childControlWidth = true; hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = false;
        }
        else
        {
            VerticalLayoutGroup vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = spacing;
            vlg.childControlWidth = true; vlg.childControlHeight = true; // Use LayoutElement Height!
            vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false; // Don't stretch indefinitely
        }
        return go;
    }

    private static GameObject CreateText(GameObject parent, string content, float size, bool bold, float width = -1, float height = -1)
    {
        GameObject go = new GameObject("Txt_" + content, typeof(TextMeshProUGUI));
        go.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        t.color = Color.white;
        t.raycastTarget = false; // Texts shouldn't block buttons

        LayoutElement le = go.AddComponent<LayoutElement>();
        if (height > 0) { le.minHeight = height; le.preferredHeight = height; }
        else { le.preferredHeight = size + 10; le.minHeight = size + 10; } // Auto height

        return go;
    }

    private static GameObject CreateButton(GameObject parent, string label, Color color)
    {
        GameObject go = new GameObject("Btn_" + label.Split('(')[0].Trim(), typeof(Image), typeof(Button));
        go.transform.SetParent(parent.transform, false);
        
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true; // MUST be true for clicks

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.minHeight = 60; le.preferredHeight = 60;
        le.flexibleWidth = 1;

        // Text Child
        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(go.transform, false);
        Stretch(txtObj);

        TextMeshProUGUI t = txtObj.GetComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 24;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.black;
        t.raycastTarget = false; // Important so it doesn't block the button click

        return go;
    }

    private static GameObject CreateInput(GameObject parent, string label, string defaultVal)
    {
        GameObject container = new GameObject("Input_" + label.Split(' ')[0], typeof(RectTransform));
        container.transform.SetParent(parent.transform, false);
        
        LayoutElement leCont = container.AddComponent<LayoutElement>();
        leCont.flexibleWidth = 1;

        VerticalLayoutGroup vlg = container.AddComponent<VerticalLayoutGroup>();
        vlg.childControlWidth = true; vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

        // Label
        CreateText(container, label, 18, false, -1, 30);

        // Field
        GameObject field = new GameObject("Field", typeof(Image), typeof(TMP_InputField));
        field.transform.SetParent(container.transform, false);
        
        Image img = field.GetComponent<Image>();
        img.color = new Color(0.9f, 0.9f, 0.9f);
        img.raycastTarget = true;

        LayoutElement leField = field.AddComponent<LayoutElement>();
        leField.minHeight = 40; leField.preferredHeight = 40;

        TMP_InputField input = field.GetComponent<TMP_InputField>();
        input.text = defaultVal;
        input.image = img;
        
        // Text Area
        GameObject textArea = new GameObject("TextArea", typeof(RectMask2D));
        textArea.transform.SetParent(field.transform, false);
        Stretch(textArea);
        
        // Placeholder
        GameObject placeholder = new GameObject("Placeholder", typeof(TextMeshProUGUI));
        placeholder.transform.SetParent(textArea.transform, false);
        Stretch(placeholder);
        
        TextMeshProUGUI tPlace = placeholder.GetComponent<TextMeshProUGUI>();
        tPlace.text = "Entrez " + label + "...";
        tPlace.fontSize = 20; tPlace.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        tPlace.fontStyle = FontStyles.Italic;
        tPlace.alignment = TextAlignmentOptions.Left;

        // Text
        GameObject text = new GameObject("Text", typeof(TextMeshProUGUI));
        text.transform.SetParent(textArea.transform, false);
        Stretch(text);
        
        TextMeshProUGUI t = text.GetComponent<TextMeshProUGUI>();
        t.fontSize = 20; t.color = Color.black;
        t.alignment = TextAlignmentOptions.Left;
        
        input.textViewport = textArea.GetComponent<RectTransform>();
        input.textComponent = t;
        input.placeholder = tPlace;

        return container; // Return container for layout, but get Field in linking
    }

    private static GameObject CreatePrefab(GameObject root, string name, bool isServer)
    {
        // Check existing to destroy (clean slate)
        Transform t = root.transform.Find(name);
        if (t != null) Undo.DestroyObjectImmediate(t.gameObject);

        GameObject go = new GameObject(name, typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(root.transform, false);
        go.SetActive(false); // Template

        go.GetComponent<Image>().color = new Color(0,0,0,0.5f);
        LayoutElement le = go.GetComponent<LayoutElement>();
        le.minHeight = 60; le.preferredHeight = 60;
        
        HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(20,20,10,10);
        hlg.spacing = 20;
        hlg.childControlWidth = true; // Essential for flexibleWidth children!
        hlg.childForceExpandWidth = true;

        // Name
        GameObject txtName = CreateText(go, "Name", 24, true);
        txtName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        txtName.GetComponent<LayoutElement>().flexibleWidth = 1;

        // Info
        if (isServer)
        {
             GameObject txtCount = CreateText(go, "0/4", 24, false);
             txtCount.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineRight;
             txtCount.GetComponent<LayoutElement>().minWidth = 50;
             
             GameObject btn = CreateButton(go, "JOIN", Color.cyan);
             btn.GetComponent<LayoutElement>().minWidth = 100;
             btn.GetComponent<LayoutElement>().flexibleWidth = 0;
             
             // Setup Component
             LobbyEntryUI ui = go.AddComponent<LobbyEntryUI>();
             SerializedObject s = new SerializedObject(ui);
             s.FindProperty("_serverNameText").objectReferenceValue = txtName.GetComponent<TextMeshProUGUI>();
             s.FindProperty("_playerCountText").objectReferenceValue = txtCount.GetComponent<TextMeshProUGUI>();
             s.FindProperty("_joinButton").objectReferenceValue = btn.GetComponent<Button>();
             s.ApplyModifiedProperties();
        }
        else
        {
             GameObject icon = new GameObject("ReadyIcon", typeof(Image));
             icon.transform.SetParent(go.transform, false);
             icon.GetComponent<Image>().color = Color.gray;
             LayoutElement leIcon = icon.AddComponent<LayoutElement>();
             leIcon.minWidth = 40; leIcon.preferredWidth = 40; leIcon.minHeight = 40; leIcon.preferredHeight = 40;

             // Setup Component
             LobbyPlayerCard card = go.AddComponent<LobbyPlayerCard>();
             SerializedObject s = new SerializedObject(card);
             s.FindProperty("_playerNameText").objectReferenceValue = txtName.GetComponent<TextMeshProUGUI>();
             s.FindProperty("_readyStatusImage").objectReferenceValue = icon.GetComponent<Image>();
             s.ApplyModifiedProperties();
        }

        return go;
    }

    private static void Stretch(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
