using System.Collections.Generic;
using FallGuys.Networking;
using TMPro;
using UnityEngine;

public class LobbyListUI : MonoBehaviour
{
    [SerializeField] private Transform _container;
    [SerializeField] private GameObject _lobbyEntryPrefab;

    private Dictionary<string, LobbyEntry> _discoveredLobbies = new Dictionary<string, LobbyEntry>();

    private void OnEnable()
    {
        var discovery = LanDiscoveryManager.Singleton;

        // Fallback: Try to find it in scene if Singleton isn't ready (Execution Order issue)
        if (discovery == null)
        {
            discovery = FindFirstObjectByType<LanDiscoveryManager>();
        }

        if (discovery != null)
        {
            discovery.OnLobbyFound += HandleLobbyFound;
            discovery.StartListening();
        }
        else
        {
            Debug.LogError("[LobbyListUI] LanDiscoveryManager not found! Please ensure 'LanDiscoveryManager' is attached to your NetworkManager.");
        }
    }

    private void OnDisable()
    {
        if (LanDiscoveryManager.Singleton != null)
        {
            LanDiscoveryManager.Singleton.OnLobbyFound -= HandleLobbyFound;
            LanDiscoveryManager.Singleton.StopListening();
        }
    }

    // DEBUG VISUAL: Add a fake server so the user sees the UI works
    private void Start()
    {
        // Don't auto-invoke, wait for panel open or explicit call
    }

    public void SimulateTestServer()
    {
        // Only if empty to avoid clutter
        if (_discoveredLobbies.Count == 0)
        {
            // Create fake entry
            LobbyEntry fake = new LobbyEntry("127.0.0.1", 7777, "TEST SERVER (SIMULATION)", 1, 4);
            HandleLobbyFound(fake);

            // Also force layout rebuild in case it's wonky
            if (_container != null)
            {
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(_container.GetComponent<RectTransform>());
            }
        }
    }

    private void HandleLobbyFound(LobbyEntry entry)
    {
        // Unique Key: IP + Port (Allows multiple servers on localhost)
        string key = $"{entry.IpAddress}:{entry.Port}";

        // Add or Update entry
        if (!_discoveredLobbies.ContainsKey(key))
        {
            _discoveredLobbies.Add(key, entry);
            CreateEntryUI(entry);
        }
        else
        {
            _discoveredLobbies[key] = entry;
            // UpdateUI(); 
        }
    }

    private void CreateEntryUI(LobbyEntry entry)
    {
        Debug.Log($"[LobbyListUI] Creating Entry for {entry.HostName}...");
        if (_lobbyEntryPrefab == null)
        {
            Debug.LogError("[LobbyListUI] PREFAB IS NULL! Please run Tools > Generate Full UI to fix references.");
            return;
        }

        GameObject go = Instantiate(_lobbyEntryPrefab, _container);
        go.transform.localScale = Vector3.one;
        go.SetActive(true); // Fix: Ensure it's visible!

        LobbyEntryUI ui = go.GetComponent<LobbyEntryUI>();
        if (ui != null)
        {
            ui.Initialize(entry);
        }
    }

    private void UpdateUI()
    {
        // Simple full rebuild or targeted update could go here if we tracked instances
        // For simplicity, we just add new ones in HandleLobbyFound
    }

    public void RefreshList()
    {
        foreach (Transform child in _container)
        {
            Destroy(child.gameObject);
        }
        _discoveredLobbies.Clear();
    }
}
