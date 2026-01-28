using System.Collections.Generic;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher
{
    [CreateAssetMenu(fileName = "LauncherBehaviour", menuName = "Traps/Behaviours/Launcher")]
    public class LauncherBehaviourSO : TrapBehaviourSO
    {
        private const string TARGET_LIST_KEY = "_targets";
        private const string CURRENT_TARGET_KEY = "_currentTarget";

        public override void OnStart(BaseObject owner, Blackboard blackboard)
        {
            base.OnStart(owner, blackboard);
            blackboard.Set(TARGET_LIST_KEY, new List<Transform>());
            
            // Store initial rotation for the search sweep
            blackboard.Set("_initialRotation", owner.transform.rotation);
        }

        protected override void OnTrapTriggerEnter(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!blackboard.IsServer) return;

            LauncherTrapSO config = owner.RuntimeData.Config as LauncherTrapSO;
            if (config == null) return;

            if (((1 << other.gameObject.layer) & config.ImpactLayer) != 0)
            {
                var targets = blackboard.Get<List<Transform>>(TARGET_LIST_KEY);
                if (!targets.Contains(other.transform))
                {
                    targets.Add(other.transform);
                }
            }
        }

        protected override void OnTrapTriggerExit(BaseObject owner, Blackboard blackboard, Collider other)
        {
            if (!blackboard.IsServer) return;

            var targets = blackboard.Get<List<Transform>>(TARGET_LIST_KEY);
            if (targets != null && targets.Contains(other.transform))
            {
                targets.Remove(other.transform);
                
                // If it was the current target, clear it
                if (blackboard.Get<Transform>(CURRENT_TARGET_KEY) == other.transform)
                {
                    blackboard.Set<Transform>(CURRENT_TARGET_KEY, null);
                }
            }
        }

        public override void OnUpdate(BaseObject owner, Blackboard blackboard)
        {
            if (!blackboard.IsServer) return;

            var targets = blackboard.Get<List<Transform>>(TARGET_LIST_KEY);
            if (targets != null && targets.Count > 0)
            {
                // Simple logic: target the first one that entered
                if (blackboard.Get<Transform>(CURRENT_TARGET_KEY) == null)
                {
                    blackboard.Set(CURRENT_TARGET_KEY, targets[0]);
                }
            }
        }
    }
}
