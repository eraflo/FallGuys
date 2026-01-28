using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.Conditions
{
    /// <summary>
    /// Condition to check if the launcher is correctly aimed at its target.
    /// Returns true if the target is within the allowed angle and range.
    /// </summary>
    [CreateAssetMenu(fileName = "TargetLockedCondition", menuName = "StateMachine/Conditions/Launcher/TargetLocked")]
    public class TargetLockedConditionSO : ConditionSO
    {
        [Tooltip("The maximum allowed angle (degrees) between the launcher's forward and the target direction to consider it 'locked'.")]
        [SerializeField] private float _angleThreshold = 30f;

        public override bool IsMet(Blackboard bb)
        {
            // Check if we even have a target assigned by the Search/Orient states
            Transform target = bb.Get<Transform>("Target");
            if (target == null) return false;

            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            if (baseObj == null) return false;

            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;
            if (config == null) return false;

            Vector3 toTarget = (target.position - owner.transform.position);

            // 1. DISTANCE VALIDATION
            // Ensure target is still within shooting distance (with 10% safety buffer)
            if (toTarget.magnitude > config.DetectionRange * 1.1f) return false;

            // 2. ANGLE VALIDATION
            // Calculate how well we are pointing at the player.
            toTarget.Normalize();
            float angle = Vector3.Angle(owner.transform.forward, toTarget);

            // True if our aim is precise enough to fire
            return angle <= _angleThreshold;
        }
    }
}
