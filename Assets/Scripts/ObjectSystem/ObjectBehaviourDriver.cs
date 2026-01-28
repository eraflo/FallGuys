using System.Collections.Generic;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.ObjectSystem
{
    /// <summary>
    /// Component responsible for bridging the Common ObjectSystem with project-specific logic.
    /// It resolves the logic key via the LogicRegistry and automatically adds the required 
    /// components (StateMachine vs SimpleBehaviour) based on the resolved asset type.
    /// </summary>
    [RequireComponent(typeof(BaseObject))]
    public class ObjectBehaviourDriver : MonoBehaviour
    {
        private BaseObject _baseObject;
        private NetworkStateMachine _stateMachine;
        private LogicIdentitySO _config;
        private Blackboard _localBlackboard;

        private void Start()
        {
            _baseObject = GetComponent<BaseObject>();
            if (_baseObject == null)
            {
                Debug.LogError("ObjectBehaviourDriver: BaseObject not found");
                this.gameObject.SetActive(false);
                return;
            }

            InitializeLinker();
            InitializeBlackboard();
        }

        private void Update()
        {
            // Simple behaviour logic (non-state-machine)
            if (_config is SimpleBehaviourSO simpleSO)
            {
                simpleSO.OnUpdate(_baseObject, _localBlackboard);
            }
        }

        /// <summary>
        /// Resolves the logic asset and automatically injects components based on the asset Type.
        /// </summary>
        private void InitializeLinker()
        {
            var objectConfig = _baseObject.RuntimeData.Config;

            if (!string.IsNullOrEmpty(objectConfig.LogicKey))
            {
                // Resolve logic from registry (Key -> ScriptableObject)
                var registry = Resources.Load<LogicRegistrySO>("LogicRegistry");
                if (registry != null)
                {
                    _config = registry.GetLogic(objectConfig.LogicKey);
                }
            }

            if (_config == null)
            {
                Debug.LogError($"ObjectBehaviourDriver: Logic mapping not found for {gameObject.name} (Key: {objectConfig.LogicKey})");
                this.gameObject.SetActive(false);
                return;
            }

            // POLYMORPHIC INJECTION:
            // 1. If it's a StateConfig, we need a State Machine
            if (_config is StateConfigSO stateConfigSO)
            {
                _stateMachine = gameObject.AddComponent<NetworkStateMachine>();
                _stateMachine.AddConfig(stateConfigSO);
            }
            // 2. If it's a SimpleBehaviour, we run it locally via OnStart
            else if (_config is SimpleBehaviourSO simpleBehaviourConfigSO)
            {
                _localBlackboard = new Blackboard(_baseObject.gameObject);
                simpleBehaviourConfigSO.OnStart(_baseObject, _localBlackboard);
            }
        }

        /// <summary>
        /// Populates the blackboard (State Machine or Local) with parameters from the ObjectSO.
        /// </summary>
        private void InitializeBlackboard()
        {
            Blackboard targetBlackboard = (_stateMachine != null) ? _stateMachine.Blackboard : _localBlackboard;
            if (targetBlackboard == null) return;

            ObjectSO config = _baseObject.RuntimeData.Config;
            var overrides = _baseObject.RuntimeData.Overrides;

            // Use reflection to find all [LevelEditable] fields and inject them into the runtime blackboard
            var fields = ParameterReflector.GetEditableFields(config);

            foreach (var field in fields)
            {
                string paramName = field.Name;
                object finalValue = field.GetValue(config);

                var overrideData = overrides.Find(x => x.Name == paramName);
                if (overrideData != null)
                {
                    finalValue = ParameterReflector.ParseValue(overrideData.StringValue, overrideData.TypeName);
                }

                targetBlackboard.Set(paramName, finalValue);
            }
        }
    }
}