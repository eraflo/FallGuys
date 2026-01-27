using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FallGuys.Networking;

public class LobbyEntryUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _serverNameText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private TextMeshProUGUI _ipText;
    [SerializeField] private Button _joinButton;

    private LobbyEntry _entry;

    public void Initialize(LobbyEntry entry)
    {
        _entry = entry;

        if (_serverNameText) _serverNameText.text = entry.HostName;
        if (_playerCountText) _playerCountText.text = $"{entry.PlayerCount}/{entry.MaxPlayers}";
        if (_ipText) _ipText.text = entry.IpAddress;

        if (_joinButton)
        {
            _joinButton.onClick.RemoveAllListeners();
            _joinButton.onClick.AddListener(OnJoinPressed);
        }
    }

    private void OnJoinPressed()
    {
        Debug.Log($"[LobbyEntryUI] Requesting Join: {_entry.IpAddress}:{_entry.Port}");
        
        if (Unity.Netcode.NetworkManager.Singleton.IsClient || Unity.Netcode.NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("Already connected! Please disconnect first.");
            return;
        }

        if (LobbyManager.Singleton != null)
        {
            LobbyManager.Singleton.StartClient(_entry.IpAddress, _entry.Port);
        }
        else
        {
             Debug.LogError("LobbyManager not found!");
        }
    }
}
