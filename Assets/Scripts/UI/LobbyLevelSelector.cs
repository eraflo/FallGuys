using System.Collections.Generic;
using System.IO;
using Eraflo.Common.LevelSystem;
using FallGuys.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FallGuys.UI
{
    /// <summary>
    /// UI component for selecting a level file in the lobby.
    /// Host-only: allows browsing saved levels and loading them.
    /// </summary>
    public class LobbyLevelSelector : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button _selectButton;
        [SerializeField] private TMP_Text _selectedLevelText;
        [SerializeField] private GameObject _fileBrowserPanel;
        [SerializeField] private RectTransform _fileListContainer;
        [SerializeField] private GameObject _fileEntryPrefab;
        [SerializeField] private Button _closeButton;

        [Header("Settings")]
        [SerializeField] private string _levelsSubfolder = "Saves";

        private List<GameObject> _spawnedEntries = new List<GameObject>();
        private Level _selectedLevel;

        private void Awake()
        {
            if (_selectButton != null)
            {
                _selectButton.onClick.AddListener(ToggleFileBrowser);
            }
            if (_closeButton != null)
            {
                _closeButton.onClick.AddListener(CloseFileBrowser);
            }
            if (_fileBrowserPanel != null)
            {
                _fileBrowserPanel.SetActive(false);
            }

            UpdateSelectedLevelDisplay();
        }

        /// <summary>
        /// Gets the currently selected level.
        /// </summary>
        public Level SelectedLevel => _selectedLevel;

        private void ToggleFileBrowser()
        {
            if (_fileBrowserPanel == null) return;

            bool isActive = _fileBrowserPanel.activeSelf;
            if (!isActive)
            {
                RefreshFileList();
            }
            _fileBrowserPanel.SetActive(!isActive);
        }

        private void CloseFileBrowser()
        {
            if (_fileBrowserPanel != null)
            {
                _fileBrowserPanel.SetActive(false);
            }
        }

        private void RefreshFileList()
        {
            // Clear previous entries
            foreach (var go in _spawnedEntries)
            {
                Destroy(go);
            }
            _spawnedEntries.Clear();

            // Get level files
            string levelsPath = Path.Combine(Application.persistentDataPath, _levelsSubfolder);
            if (!Directory.Exists(levelsPath))
            {
                Directory.CreateDirectory(levelsPath);
            }

            string[] files = Directory.GetFiles(levelsPath, "*.json");

            foreach (var file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                CreateFileEntry(fileName, file);
            }
        }

        private void CreateFileEntry(string displayName, string fullPath)
        {
            if (_fileEntryPrefab == null || _fileListContainer == null) return;

            var entryGO = Instantiate(_fileEntryPrefab, _fileListContainer);
            var text = entryGO.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = displayName;
            }

            var button = entryGO.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => OnFileSelected(fullPath));
            }

            _spawnedEntries.Add(entryGO);
        }

        private void OnFileSelected(string filePath)
        {
            // Load the level from file
            try
            {
                string json = File.ReadAllText(filePath);
                _selectedLevel = Newtonsoft.Json.JsonConvert.DeserializeObject<Level>(json);

                // Set on GameManager
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.SelectedLevel = _selectedLevel;
                }

                UpdateSelectedLevelDisplay();
                CloseFileBrowser();

                Debug.Log($"[LobbyLevelSelector] Selected level: {_selectedLevel.LevelName}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[LobbyLevelSelector] Failed to load level: {ex.Message}");
            }
        }

        private void UpdateSelectedLevelDisplay()
        {
            if (_selectedLevelText != null)
            {
                _selectedLevelText.text = _selectedLevel != null
                    ? _selectedLevel.LevelName
                    : "No level selected";
            }
        }
    }
}
