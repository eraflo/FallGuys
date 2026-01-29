using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FallGuys.Core
{
    /// <summary>
    /// UI displayed at the end of a race showing the leaderboard and return-to-lobby button.
    /// </summary>
    public class EndRaceUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private RectTransform _leaderboardContainer;
        [SerializeField] private GameObject _leaderboardEntryPrefab;
        [SerializeField] private Button _returnToLobbyButton;
        [SerializeField] private TMP_Text _titleText;

        private List<GameObject> _spawnedEntries = new List<GameObject>();

        private void Awake()
        {
            if (_returnToLobbyButton != null)
            {
                _returnToLobbyButton.onClick.AddListener(OnReturnToLobby);
            }

            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        /// <summary>
        /// Shows the end race panel with the given leaderboard entries.
        /// </summary>
        public void Show(List<LeaderboardEntry> entries)
        {
            if (_panel == null) return;

            // Clear previous entries
            foreach (var go in _spawnedEntries)
            {
                Destroy(go);
            }
            _spawnedEntries.Clear();

            // Populate leaderboard
            foreach (var entry in entries)
            {
                if (_leaderboardEntryPrefab == null || _leaderboardContainer == null) continue;

                var entryGO = Instantiate(_leaderboardEntryPrefab, _leaderboardContainer);
                var text = entryGO.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = $"{entry.Rank}. {entry.PlayerName} - {entry.FinishTime:F2}s";
                }
                _spawnedEntries.Add(entryGO);
            }

            // Show title
            if (_titleText != null)
            {
                _titleText.text = "Race Finished!";
            }

            _panel.SetActive(true);
        }

        /// <summary>
        /// Hides the end race panel.
        /// </summary>
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.SetActive(false);
            }
        }

        private void OnReturnToLobby()
        {
            Hide();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToLobby();
            }
        }
    }
}
