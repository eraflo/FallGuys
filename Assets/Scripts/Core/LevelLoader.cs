using System.Collections.Generic;
using Eraflo.Common.LevelSystem;
using Eraflo.Common.ObjectSystem;
using FallGuys.ObjectSystem;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.Core
{
    /// <summary>
    /// Handles spawning objects from a Level's ObjectData list.
    /// Server-authoritative: only the server spawns objects.
    /// </summary>
    public class LevelLoader : NetworkBehaviour
    {
        [Header("Prefabs")]
        [Tooltip("Base prefab with NetworkObject, BaseObject, and ObjectBehaviourDriver")]
        [SerializeField] private GameObject _baseObjectPrefab;

        private List<NetworkObject> _spawnedObjects = new List<NetworkObject>();

        /// <summary>
        /// Event fired when all level objects have been spawned.
        /// </summary>
        public event System.Action OnLevelLoaded;

        /// <summary>
        /// Spawns all objects defined in the level. Server-only.
        /// </summary>
        public void LoadLevel(Level level)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[LevelLoader] LoadLevel called on client - ignoring.");
                return;
            }

            if (level == null || level.Objects == null)
            {
                Debug.Log("[LevelLoader] Level has no objects to spawn.");
                OnLevelLoaded?.Invoke();
                return;
            }

            Debug.Log($"[LevelLoader] Loading {level.Objects.Count} objects from level '{level.LevelName}'");

            foreach (var objData in level.Objects)
            {
                SpawnObject(objData);
            }

            Debug.Log("[LevelLoader] Level loading complete.");
            OnLevelLoaded?.Invoke();
        }

        private void SpawnObject(ObjectData data)
        {
            if (data.Config == null)
            {
                Debug.LogWarning("[LevelLoader] ObjectData has null Config, skipping.");
                return;
            }

            // Get position and rotation from serializable types
            Vector3 position = data.Position.ToVector3();
            Quaternion rotation = data.Rotation.ToQuaternion();
            Vector3 scale = data.Scale.ToVector3();

            // Instantiate the base object prefab
            GameObject instance = Instantiate(_baseObjectPrefab, position, rotation);
            instance.transform.localScale = scale;
            instance.name = $"LevelObj_{data.Config.Name}";

            // Configure the BaseObject with the ObjectSO
            if (instance.TryGetComponent<BaseObject>(out var baseObj))
            {
                // Set the config via reflection or a public method if available
                // For now, we assume the prefab's BaseObject picks up from RuntimeData
                // which is set during Awake based on a serialized config field

                // We need to set the config before Awake runs, so we do it on the prefab instance
                // This requires BaseObject to expose a way to set config at runtime
                SetBaseObjectConfig(baseObj, data);
            }
            else
            {
                Debug.LogError($"[LevelLoader] BaseObject component not found on prefab: {_baseObjectPrefab.name}");
                Destroy(instance);
                return;
            }

            // Spawn on network
            if (instance.TryGetComponent<NetworkObject>(out var netObj))
            {
                netObj.Spawn();
                _spawnedObjects.Add(netObj);
            }
            else
            {
                Debug.LogError($"[LevelLoader] NetworkObject component not found on prefab: {_baseObjectPrefab.name}");
                Destroy(instance);
            }
        }

        private void SetBaseObjectConfig(BaseObject baseObj, ObjectData data)
        {
            // Create runtime data manually since the object was just instantiated
            // This needs to happen before Start() is called on ObjectBehaviourDriver
            var runtimeDataField = typeof(BaseObject).GetField("_runtimeData",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (runtimeDataField != null)
            {
                var runtimeData = new ObjectData(
                    data.Config,
                    data.Position.ToVector3(),
                    data.Rotation.ToQuaternion(),
                    data.Scale.ToVector3()
                );
                runtimeData.Overrides = data.Overrides;
                runtimeDataField.SetValue(baseObj, runtimeData);
            }
            else
            {
                Debug.LogError("[LevelLoader] Could not find _runtimeData field via reflection.");
            }
        }

        /// <summary>
        /// Clears all spawned objects (e.g., when returning to lobby).
        /// </summary>
        public void UnloadLevel()
        {
            if (!IsServer) return;

            foreach (var netObj in _spawnedObjects)
            {
                if (netObj != null)
                {
                    netObj.Despawn(true);
                }
            }
            _spawnedObjects.Clear();
        }
    }
}
