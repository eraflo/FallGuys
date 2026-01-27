using System.Collections.Generic;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.ObjectSystem
{
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
            if (_config is SimpleBehaviourSO simpleSO)
            {
                simpleSO.OnUpdate(_baseObject, _localBlackboard);
            }
        }

        /// <summary>
        /// Initializes the linker.
        /// </summary>
        private void InitializeLinker()
        {
            _config = _baseObject.RuntimeData.Config.LogicIdentity;
            if (_config == null)
            {
                Debug.LogError("ObjectBehaviourDriver: StateConfigSO not found");
                this.gameObject.SetActive(false);
                return;
            }

            if (_config is StateConfigSO stateConfigSO)
            {
                _stateMachine = gameObject.AddComponent<NetworkStateMachine>();
                _stateMachine.AddConfig(stateConfigSO);
            }
            else if (_config is SimpleBehaviourSO simpleBehaviourConfigSO)
            {
                _localBlackboard = new Blackboard(_baseObject.gameObject);
                simpleBehaviourConfigSO.OnStart(_baseObject, _localBlackboard);
            }
        }

        /// <summary>
        /// Initializes the blackboard by iterating through the list of editable fields.
        /// </summary>
        private void InitializeBlackboard()
        {
            Blackboard targetBlackboard = (_stateMachine != null) ? _stateMachine.Blackboard : _localBlackboard;

            if (targetBlackboard == null) return;

            ObjectSO config = _baseObject.RuntimeData.Config;
            var overrides = _baseObject.RuntimeData.Overrides;

            // 1. We get the list of editable fields via Reflection
            var fields = ParameterReflector.GetEditableFields(config);

            foreach (var field in fields)
            {
                string paramName = field.Name;
                object finalValue = field.GetValue(config); // Default value

                // 2. Do we have an override?
                var overrideData = overrides.Find(x => x.Name == paramName);
                if (overrideData != null)
                {
                    finalValue = ParameterReflector.ParseValue(overrideData.StringValue, overrideData.TypeName);
                }

                // 3. Injection dans le Blackboard
                targetBlackboard.Set(paramName, finalValue);
            }
        }
    }
}