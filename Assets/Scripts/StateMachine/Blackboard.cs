using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace FallGuys.StateMachine
{
    /// <summary>
    /// Specialized data container providing instance-specific storage for states.
    /// Prevents shared ScriptableObject assets from leaking data between different entities.
    /// </summary>
    public class Blackboard
    {
        private Dictionary<string, object> data = new Dictionary<string, object>();
        private GameObject owner;
        private NetworkObject networkObject;

        /// <summary>
        /// Initializes a new blackboard for a specific owner.
        /// Automatically discovers networking references if not provided.
        /// </summary>
        /// <param name="owner">The GameObject this state machine belongs to.</param>
        /// <param name="networkRef">Optional reference to a network component.</param>
        public Blackboard(GameObject owner, NetworkBehaviour networkRef = null)
        {
            this.owner = owner;

            if (networkRef != null)
            {
                this.networkObject = networkRef.NetworkObject;
            }
            else
            {
                this.networkObject = owner.GetComponent<NetworkObject>();
            }
        }

        /// <summary>
        /// True if this logic is running on the Server.
        /// </summary>
        public bool IsServer => networkObject != null && networkObject.NetworkManager != null && networkObject.NetworkManager.IsServer;

        /// <summary>
        /// True if this logic is running on a Client.
        /// </summary>
        public bool IsClient => networkObject != null && networkObject.NetworkManager != null && networkObject.NetworkManager.IsClient;

        /// <summary>
        /// True if this client is the one owning/controlling this object.
        /// Useful for local UI or input-specific logic.
        /// </summary>
        public bool IsOwner => networkObject != null && networkObject.IsOwner;

        /// <summary>
        /// Helper to retrieve a component attached to the owner.
        /// </summary>
        public T GetOwner<T>() where T : Component
        {
            return owner.GetComponent<T>();
        }

        /// <summary>
        /// Returns the raw owner GameObject.
        /// </summary>
        public GameObject GetOwnerObject() => owner;

        /// <summary>
        /// Stores a value in the blackboard.
        /// </summary>
        public void Set<T>(string key, T value)
        {
            data[key] = value;
        }

        /// <summary>
        /// Retrieves a value from the blackboard.
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            if (data.TryGetValue(key, out object value))
            {
                return (T)value;
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks if a key exists in the blackboard.
        /// </summary>
        public bool Has(string key) => data.ContainsKey(key);
    }
}
