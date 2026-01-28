using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FallGuys.Networking
{
    /// <summary>
    /// A simple MonoBehaviour (NOT NetworkBehaviour) that monitors client connection
    /// and returns to lobby if disconnected. This runs independently of network state.
    /// </summary>
    public class ClientDisconnectWatcher : MonoBehaviour
    {
        public static ClientDisconnectWatcher Instance { get; private set; }

        private bool _wasConnected = false;
        private bool _isReturningToLobby = false;
        private Coroutine _watcherCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable()
        {
            // Start watching using coroutine which is more resilient
            _watcherCoroutine = StartCoroutine(WatchConnectionCoroutine());
        }

        private void OnDisable()
        {
            if (_watcherCoroutine != null)
            {
                StopCoroutine(_watcherCoroutine);
                _watcherCoroutine = null;
            }
        }

        private IEnumerator WatchConnectionCoroutine()
        {
            // Wait a moment before starting checks
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                yield return new WaitForSeconds(0.1f); // Check every 100ms

                if (_isReturningToLobby) continue;

                try
                {
                    // Track when we become connected (as client, not host)
                    if (!_wasConnected)
                    {
                        if (NetworkManager.Singleton != null &&
                            NetworkManager.Singleton.IsConnectedClient &&
                            !NetworkManager.Singleton.IsServer)
                        {
                            _wasConnected = true;
                        }
                        continue;
                    }

                    // We were connected - check if still connected
                    bool isDisconnected = false;

                    if (NetworkManager.Singleton == null)
                    {
                        isDisconnected = true;
                    }
                    else if (!NetworkManager.Singleton.IsConnectedClient || NetworkManager.Singleton.ShutdownInProgress)
                    {
                        isDisconnected = true;
                    }

                    if (isDisconnected)
                    {
                        _wasConnected = false;
                        _isReturningToLobby = true;
                        Invoke(nameof(ForceReturnToLobby), 0.1f);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ClientDisconnectWatcher] Exception: {e.Message}");
                    // Don't immediately return to lobby on exception, let the next iteration try again
                }
            }
        }

        private void ForceReturnToLobby()
        {
            _isReturningToLobby = false;
            SceneManager.LoadScene("Lobby");
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
