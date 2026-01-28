using System.Collections.Generic;
using FallGuys.Networking;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject _connectionPanel;
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private GameObject _browserPanel; // New Browser Panel

    [Header("Connection UI")]
    [SerializeField] private Button _hostButton;
    [SerializeField] private Button _clientButton;     // Direct Connect
    [SerializeField] private Button _browserButton;    // Open Browser

    [Header("Connection Inputs")]
    [SerializeField] private TMP_InputField _ipInput;
    [SerializeField] private TMP_InputField _portInput;

    [Header("Browser UI")]
    [SerializeField] private Button _backToMenuButton;

    [Header("Lobby UI")]
    [SerializeField] private Transform _playerListContainer;
    [SerializeField] private GameObject _playerRowPrefab;
    [SerializeField] private Button _readyButton;
    [SerializeField] private Button _leaveButton; // New Leave Button
    [SerializeField] private TextMeshProUGUI _readyButtonText;
    [SerializeField] private TextMeshProUGUI _countdownText;

    private bool _isReady = false;
    private int _lastPlayerCount = -1;

    private void Awake()
    {
        // AUTO-RECOVERY: If references are lost, try to find them by name
        if (_connectionPanel == null)
        {
            var canvas = GameObject.Find("MainCanvas");
            if (canvas != null)
            {
                var t = canvas.transform.Find("ConnectionPanel");
                if (t != null) _connectionPanel = t.gameObject;

                var tLobby = canvas.transform.Find("LobbyPanel");
                if (tLobby != null) _lobbyPanel = tLobby.gameObject;

                var tBrowser = canvas.transform.Find("BrowserPanel");
                if (tBrowser != null) _browserPanel = tBrowser.gameObject;

                var tLoad = canvas.transform.Find("LoadingPanel");
                if (tLoad != null) _loadingPanel = tLoad.gameObject;
            }
        }

        // AUTO-RECOVER COMPONENTS (Deep Search)
        if (_connectionPanel != null)
        {
            if (_hostButton == null) _hostButton = FindButton(_connectionPanel.transform, "Btn_HOST");
            if (_clientButton == null) _clientButton = FindButton(_connectionPanel.transform, "Btn_REJOINDRE");
            if (_browserButton == null) _browserButton = FindButton(_connectionPanel.transform, "Btn_LISTE");

            if (_ipInput == null) _ipInput = _connectionPanel.transform.GetComponentInChildren<TMP_InputField>(); // Fallback
            // Ideally specific search but simplified for crash prevention
        }

        if (_lobbyPanel != null)
        {
            if (_countdownText == null)
            {
                var t = _lobbyPanel.transform.Find("Txt_Countdown");
                if (t) _countdownText = t.GetComponent<TextMeshProUGUI>();
            }
            if (_readyButton == null) _readyButton = FindButton(_lobbyPanel.transform, "Btn_PRÊT", "Btn_Ready");
            if (_leaveButton == null) _leaveButton = FindButton(_lobbyPanel.transform, "Btn_QUITTER", "Btn_Leave");
        }

        if (_browserPanel != null)
        {
            if (_backToMenuButton == null) _backToMenuButton = FindButton(_browserPanel.transform, "Btn_RETOUR");
        }
    }

    private Button FindButton(Transform root, params string[] names)
    {
        foreach (var n in names)
        {
            var t = root.Find(n);
            if (t == null)
            {
                // Try fuzzy
                foreach (Transform child in root)
                {
                    if (child.name.Contains(n.Split('_')[1])) { t = child; break; }
                }
            }
            if (t != null) return t.GetComponent<Button>();
        }
        return root.GetComponentInChildren<Button>(); // Desperate fallback
    }

    private void Start()
    {
        // Safety Check
        if (_connectionPanel == null)
        {
            Debug.LogError("❌ CRITICAL: UI Panels not found! Run 'Tools > GENERATE FULL UI' to fix.");
            return;
        }

        // Setup initial
        _connectionPanel.SetActive(true);
        if (_lobbyPanel) _lobbyPanel.SetActive(false);
        if (_loadingPanel) _loadingPanel.SetActive(false);
        if (_browserPanel != null) _browserPanel.SetActive(false);

        _countdownText.text = "";

        // Subscribe to Manager events if available
        if (LobbyManager.Singleton != null)
        {
            LobbyManager.Singleton.OnConnectionStarted += OnConnectionStarted;
            LobbyManager.Singleton.OnConnectionSuccess += OnConnectionSuccess;
            LobbyManager.Singleton.OnConnectionFailed += OnConnectionFailed;
            LobbyManager.Singleton.OnLeftLobby += OnLeftLobby;
        }

        if (_hostButton != null)
        {
            _hostButton.onClick.AddListener(() =>
            {
                if (LobbyManager.Singleton != null)
                {
                    string ip = _ipInput != null ? _ipInput.text : "";
                    int port = 0;
                    string portText = _portInput != null ? _portInput.text : "NULL";

                    if (_portInput != null && int.TryParse(_portInput.text, out int p)) port = p;

                    Debug.Log($"[LobbyUI] HOST REQUESTED. IP: '{ip}', Port Input: '{portText}' -> Parsed: {port}");

                    LobbyManager.Singleton.StartHost(ip, port);
                }
                else LogMissingLobbyManager();
            });
        }
        else Debug.LogError("[LobbyUI] '_hostButton' is not assigned! Please assign it in Inspector or run Tools > FallGuys > Nuke & Rebuild.");

        if (_clientButton != null)
        {
            _clientButton.onClick.AddListener(() =>
            {
                if (LobbyManager.Singleton != null)
                {
                    string ip = _ipInput != null ? _ipInput.text : "";
                    int port = 0;
                    if (_portInput != null && int.TryParse(_portInput.text, out int p)) port = p;
                    Debug.Log($"[LobbyUI] Click 'Rejoindre Direct'. Inputs -> IP: '{ip}', Port: '{port}'");
                    LobbyManager.Singleton.StartClient(ip, port);
                }
                else LogMissingLobbyManager();
            });
        }
        else Debug.LogError("[LobbyUI] '_clientButton' is not assigned!");

        if (_browserButton != null)
        {
            _browserButton.onClick.AddListener(() =>
            {
                _connectionPanel.SetActive(false);
                if (_browserPanel != null)
                {
                    _browserPanel.SetActive(true);
                }
            });
        }

        if (_backToMenuButton != null)
        {
            _backToMenuButton.onClick.AddListener(() =>
            {
                if (_browserPanel != null) _browserPanel.SetActive(false);
                _connectionPanel.SetActive(true);
            });
        }

        if (_leaveButton != null)
        {
            _leaveButton.onClick.AddListener(() =>
            {
                if (LobbyManager.Singleton != null)
                {
                    LobbyManager.Singleton.Shutdown();
                }
            });
        }

        if (_readyButton != null)
        {
            _readyButton.onClick.AddListener(() =>
            {
                if (LobbyManager.Singleton == null) return;
                _isReady = !_isReady;
                UpdateReadyButtonState();
                LobbyManager.Singleton.SetPlayerReadyServerRpc(NetworkManager.Singleton.LocalClientId, _isReady);
            });
        }
    }

    private void OnDestroy()
    {
        if (LobbyManager.Singleton != null)
        {
            try
            {
                LobbyManager.Singleton.OnConnectionStarted -= OnConnectionStarted;
                LobbyManager.Singleton.OnConnectionSuccess -= OnConnectionSuccess;
                LobbyManager.Singleton.OnConnectionFailed -= OnConnectionFailed;
                LobbyManager.Singleton.OnLeftLobby -= OnLeftLobby;
            }
            catch { }
        }
        UnsubscribeFromLobbyEvents();
    }

    private void OnConnectionStarted()
    {
        // Show loading state - connecting...
        if (_browserPanel != null) _browserPanel.SetActive(false);
        _connectionPanel.SetActive(false);
        _lobbyPanel.SetActive(false);
        _loadingPanel.SetActive(true);
    }

    private void OnConnectionSuccess()
    {
        // Connection confirmed - hide all other panels and show lobby
        if (_browserPanel != null) _browserPanel.SetActive(false);
        _loadingPanel.SetActive(false);
        _connectionPanel.SetActive(false);
        ShowLobby();
        SubscribeToLobbyEvents();
    }

    private void OnConnectionFailed(string reason)
    {
        // Connection failed - return to connection panel
        _loadingPanel.SetActive(false);
        _lobbyPanel.SetActive(false);
        _connectionPanel.SetActive(true);
        Debug.LogWarning($"[LobbyUI] Connection failed: {reason}");
    }

    private void OnLeftLobby()
    {
        // Reset UI to Connection Panel
        _lobbyPanel.SetActive(false);
        _loadingPanel.SetActive(false);
        if (_browserPanel != null) _browserPanel.SetActive(false);

        _connectionPanel.SetActive(true);

        UnsubscribeFromLobbyEvents();
        _isReady = false;
        _lastPlayerCount = -1;
    }

    private void LogMissingLobbyManager()
    {
        var manager = FindFirstObjectByType<LobbyManager>();
        if (manager != null)
            Debug.LogError($"LobbyManager.Singleton is null, BUT a LobbyManager component WAS FOUND on GameObject '{manager.name}'. This means Awake() hasn't run yet or failed.");
        else
            Debug.LogError("LobbyManager.Singleton is null AND no LobbyManager component was found in the scene. Please add the 'LobbyManager' script to your 'NetworkManager' object.");
    }

    private void SubscribeToLobbyEvents()
    {
        if (LobbyManager.Singleton == null) return;

        // Unsubscribe first to avoid duplicates if called multiple times
        UnsubscribeFromLobbyEvents();

        LobbyManager.Singleton.ConnectedPlayers.OnListChanged += OnPlayerListChanged;
        LobbyManager.Singleton.CurrentLobbyState.OnValueChanged += OnLobbyStateChanged;

        // Initial Refresh
        UpdatePlayerList();
        UpdateLobbyState(LobbyState.Offline, LobbyManager.Singleton.CurrentLobbyState.Value);
    }

    private void UnsubscribeFromLobbyEvents()
    {
        if (LobbyManager.Singleton != null && LobbyManager.Singleton.ConnectedPlayers != null)
        {
            LobbyManager.Singleton.ConnectedPlayers.OnListChanged -= OnPlayerListChanged;
        }
        if (LobbyManager.Singleton != null)
        {
            LobbyManager.Singleton.CurrentLobbyState.OnValueChanged -= OnLobbyStateChanged;
        }
    }



    private void UpdateReadyButtonState()
    {
        _readyButtonText.text = _isReady ? "ANNULER" : "PRÊT !";
        _readyButton.image.color = _isReady ? Color.red : Color.green;
    }

    private void ShowLobby()
    {
        _connectionPanel.SetActive(false);
        _lobbyPanel.SetActive(true);
        UpdateReadyButtonState();
    }

    void Update()
    {
        // Only update countdown or specific things that need per-frame update
        if (LobbyManager.Singleton != null && LobbyManager.Singleton.CurrentLobbyState.Value == LobbyState.Countdown)
        {
            float timeRemaining = LobbyManager.Singleton.CountdownTimer.Value;
            _countdownText.text = $"Démarrage dans {Mathf.CeilToInt(timeRemaining)}s";

            // Simple Pulse Animation based on second tick
            float scale = 1f + Mathf.PingPong(Time.time * 2f, 0.2f);
            _countdownText.transform.localScale = Vector3.one * scale;
        }
    }

    private void OnLobbyStateChanged(LobbyState oldState, LobbyState newState)
    {
        UpdateLobbyState(oldState, newState);
    }

    private void UpdateLobbyState(LobbyState oldState, LobbyState newState)
    {
        if (newState == LobbyState.Countdown)
        {
            // Update handled in Update() for smooth countdown
        }
        else if (newState == LobbyState.GameLoading)
        {
            _lobbyPanel.SetActive(false);
            _loadingPanel.SetActive(true);
        }
        else
        {
            _countdownText.text = "En attente de joueurs...";
            _countdownText.transform.localScale = Vector3.one; // Reset scale
        }
    }

    private void OnPlayerListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        // Safety: don't update if we're being destroyed
        if (this == null || _playerListContainer == null) return;
        UpdatePlayerList();
    }

    private void UpdatePlayerList()
    {
        if (LobbyManager.Singleton == null) return;

        var players = LobbyManager.Singleton.ConnectedPlayers;
        if (players == null) return;

        // Safety check
        if (_playerListContainer == null) return;

        // Clean up existing rows - collect first to avoid foreach-while-destroying errors
        var childrenToDestroy = new List<GameObject>();
        foreach (Transform child in _playerListContainer)
        {
            if (child != null)
                childrenToDestroy.Add(child.gameObject);
        }
        foreach (var child in childrenToDestroy)
        {
            if (child != null)
                Destroy(child);
        }

        // Rebuild list - relies on VerticalLayoutGroup on _playerListContainer
        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            GameObject row = Instantiate(_playerRowPrefab, _playerListContainer);
            row.transform.localScale = Vector3.one;
            row.SetActive(true); // Ensure active before Initialize to allow Coroutines

            // Try to use dedicated component first
            var card = row.GetComponent<LobbyPlayerCard>();
            if (card != null)
            {
                card.Initialize(player);
            }
            else
            {
                // Fallback: Simple Text setting if no component
                var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
                if (texts.Length > 0) texts[0].text = player.PlayerName.ToString();
                if (texts.Length > 1) texts[1].text = player.IsReady ? "READY" : "WAITING";
            }
        }
    }
}