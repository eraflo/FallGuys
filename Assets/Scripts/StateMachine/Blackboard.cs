using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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
        private NetworkBehaviour networkRef;

        /// <summary>
        /// Initializes a new blackboard for a specific owner.
        /// </summary>
        /// <param name="owner">The GameObject this state machine belongs to.</param>
        /// <param name="networkRef">Reference to the network component for role checking.</param>
        public Blackboard(GameObject owner, NetworkBehaviour networkRef)
        {
            this.owner = owner;
            this.networkRef = networkRef;
        }

        /// <summary>
        /// True if this logic is running on the Server.
        /// </summary>
        public bool IsServer => networkRef != null && networkRef.IsServer;

        /// <summary>
        /// True if this logic is running on a Client.
        /// </summary>
        public bool IsClient => networkRef != null && networkRef.IsClient;

        /// <summary>
        /// True if this client is the one owning/controlling this object.
        /// Useful for local UI or input-specific logic.
        /// </summary>
        public bool IsOwner => networkRef != null && networkRef.IsOwner;

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
