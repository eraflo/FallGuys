using Unity.Netcode;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace FallGuys.StateMachine
{
    /// <summary>
    /// Central component for managing a server-authoritative state machine synchronized over the network.
    /// Leverages ScriptableObjects for state logic and a Blackboard for instance data.
    /// </summary>
    public class NetworkStateMachine : NetworkBehaviour
    {
        [Header("Configuration")]
        [SerializeField, Tooltip("The registry of all valid states for this machine.")]
        private StateConfigSO config;
        
        [Header("Sync State")]
        [SerializeField, Tooltip("Synchronized index of the current state.")]
        private NetworkVariable<int> currentID = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private StateBaseSO currentState;
        private Blackboard blackboard;
        private CancellationTokenSource stateCTS;

        /// <summary>
        /// Initializes the state machine when spawned on the network.
        /// </summary>
        public override void OnNetworkSpawn()
        {
            blackboard = new Blackboard(gameObject, this);
            currentID.OnValueChanged += OnStateIDChanged;

            // Handle initial state or late joining sync
            if (currentID.Value != -1)
            {
                SwitchToState(currentID.Value);
            }
            else if (IsServer && config != null && config.states.Count > 0)
            {
                // Server defines the starting state (typically index 0)
                ChangeState(0);
            }
        }

        /// <summary>
        /// Cleans up the state machine when despawned.
        /// </summary>
        public override void OnNetworkDespawn()
        {
            currentID.OnValueChanged -= OnStateIDChanged;
            ExitCurrentState();
        }

        /// <summary>
        /// Main update loop handling different lifecycle hooks based on network role.
        /// </summary>
        private void Update()
        {
            if (!IsSpawned || currentState == null) return;

            // 1. Shared Update (Everyone)
            currentState.OnUpdate(blackboard);

            // 2. Role-specific Updates
            if (IsServer)
            {
                currentState.OnServerUpdate(blackboard);
                ProcessTransitions();
            }
            else
            {
                currentState.OnClientUpdate(blackboard);
            }
        }

        /// <summary>
        /// Evaluates all transitions of the current state.
        /// Only executed on the Server.
        /// </summary>
        private void ProcessTransitions()
        {
            if (!IsServer || currentState == null || currentState.transitions == null) return;

            foreach (var transition in currentState.transitions)
            {
                if (transition.targetState != null && transition.Evaluate(blackboard))
                {
                    int nextID = config.GetIDByState(transition.targetState);
                    if (nextID != -1 && nextID != currentID.Value)
                    {
                        ChangeState(nextID);
                        break; // Trigger only the first valid transition
                    }
                }
            }
        }

        /// <summary>
        /// Requests a state change. Internal call for Server logic.
        /// </summary>
        /// <param name="index">The index of the state in the StateConfigSO.</param>
        public void ChangeState(int index)
        {
            if (!IsServer)
            {
                Debug.LogWarning("[NetworkStateMachine] ChangeState called on Client. Only Server can change state.");
                return;
            }

            if (currentID.Value == index) return;

            // Updating the NetworkVariable triggers OnValueChanged on both Server and Clients
            currentID.Value = index;
        }

        /// <summary>
        /// Callback triggered when the state ID changes on the network.
        /// </summary>
        private void OnStateIDChanged(int oldID, int newID)
        {
            SwitchToState(newID);
        }

        /// <summary>
        /// Internal logic to perform the state switch lifecycle (Exit -> Enter).
        /// </summary>
        /// <param name="index">The new state index mapping.</param>
        private void SwitchToState(int index)
        {
            ExitCurrentState();

            currentState = config.GetStateByID(index);
            if (currentState != null)
            {
                stateCTS = new CancellationTokenSource();
                currentState.OnEnter(blackboard, stateCTS.Token);
                Debug.Log($"[NetworkStateMachine] Entered state: {currentState.name} on {(IsServer ? "Server" : "Client")}");
            }
        }

        /// <summary>
        /// Cleans up the previous state, cancels tasks, and triggers OnExit.
        /// </summary>
        private void ExitCurrentState()
        {
            if (currentState != null)
            {
                stateCTS?.Cancel();
                stateCTS?.Dispose();
                stateCTS = null;
                
                currentState.OnExit(blackboard);
                currentState = null;
            }
        }

        #region RPC Methods

        /// <summary>
        /// Generic ServerRpc to allow Clients to request a state change (for testing/authority).
        /// </summary>
        [ServerRpc(RequireOwnership = false)]
        public void RequestStateChangeServerRpc(int index)
        {
            Debug.Log($"[NetworkStateMachine] State change to {index} requested by Client {OwnerClientId}");
            ChangeState(index);
        }

        /// <summary>
        /// Generic ServerRpc to allow Clients to send intents to the current state logic.
        /// The Server state will receive this action through OnActionReceived.
        /// </summary>
        /// <param name="actionName">Identifier for the action (e.g., "Jump", "Interact").</param>
        [ServerRpc(RequireOwnership = false)]
        public void RequestActionServerRpc(string actionName)
        {
            if (!IsServer || currentState == null) return;

            Debug.Log($"[NetworkStateMachine] Action '{actionName}' received from Client {OwnerClientId}. Routing to {currentState.name}");
            
            // Route the intent directly to the current server-side state logic
            currentState.OnActionReceived(blackboard, actionName);
        }

        #endregion
    }
}
