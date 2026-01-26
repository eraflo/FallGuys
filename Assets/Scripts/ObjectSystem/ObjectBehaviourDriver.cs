using System.Collections.Generic;
using FallGuys.StateMachine;
using ObjectSystem;
using UnityEngine;

namespace FallGuys.ObjectSystem
{
    [RequireComponent(typeof(BaseObject))]
    public class ObjectBehaviourDriver : MonoBehaviour
    {
        private BaseObject _baseObject;
        private NetworkStateMachine _stateMachine;

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

        /// <summary>
        /// Initializes the linker.
        /// </summary>
        private void InitializeLinker()
        {
            LogicIdentitySO config = _baseObject.RuntimeData.Config.LogicIdentity;
            if (config == null)
            {
                Debug.LogError("ObjectBehaviourDriver: StateConfigSO not found");
                this.gameObject.SetActive(false);
                return;
            }

            if (config is StateConfigSO stateConfigSO)
            {
                _stateMachine = gameObject.AddComponent<NetworkStateMachine>();
                _stateMachine.AddConfig(stateConfigSO);
            }
            else if (config is SimpleBehaviourSO simpleBehaviourConfigSO)
            {
                // _localBlackboard = new Blackboard();
                simpleBehaviourConfigSO.OnStart(_baseObject, _localBlackboard);
            }
        }

        /// <summary>
        /// Initializes the blackboard by iterating through the list of editable fields.
        /// </summary>
        private void InitializeBlackboard()
        {
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
                _stateMachine.Blackboard.Set(paramName, finalValue);
            }
        }
    }
}