using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.States
{
    /// <summary>
    /// Default state for the Launcher.
    /// Performs a persistent scanning sweep and looks for valid player targets.
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherSearchState", menuName = "StateMachine/States/Launcher/Search")]
    public class LauncherSearchStateSO : StateBaseSO
    {
        public override void OnServerUpdate(Blackboard bb)
        {
            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            if (baseObj == null) return;

            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;
            if (config == null) return;

            // 1. SPATIAL SCANNING (Server Only)
            // Actively look for players in the 'ImpactLayer' within Range and Angle.
            Transform target = FindBestTarget(owner, config, bb);
            bb.Set("Target", target); // Store in blackboard for conditions/transitions

            // 2. VISUAL SWEEP
            // If no target is locked, perform a ping-pong rotation sweep to show the launcher is "searching".
            if (target == null)
            {
                // Ensure we have a reference rotation to sweep around
                if (!bb.Has("_initialRotation"))
                {
                    bb.Set("_initialRotation", owner.transform.rotation);
                }

                Quaternion initialRot = bb.Get<Quaternion>("_initialRotation");

                // Deterministic Sin-based sweep: SearchAngleRange degrees in each direction
                float angle = Mathf.Sin(Time.time * (config.RotationSpeed / 45f)) * config.SearchAngleRange;
                owner.transform.rotation = initialRot * Quaternion.Euler(0, angle, 0);
            }
        }

        /// <summary>
        /// Authoritative query to find players.
        /// Filters by Layer, Distance, and Scanning Arc (Angle).
        /// </summary>
        private Transform FindBestTarget(GameObject owner, LauncherTrapSO config, Blackboard bb)
        {
            // Query all possible colliders on the designated layer
            Collider[] colliders = Physics.OverlapSphere(owner.transform.position, config.DetectionRange, config.ImpactLayer);

            Transform bestTarget = null;
            float minDistance = float.MaxValue;

            if (!bb.Has("_initialRotation"))
            {
                bb.Set("_initialRotation", owner.transform.rotation);
            }
            Quaternion initialRot = bb.Get<Quaternion>("_initialRotation");
            Vector3 initialForward = initialRot * Vector3.forward;

            foreach (var col in colliders)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb == null) continue;

                Vector3 toTarget = col.transform.position - owner.transform.position;
                float dist = toTarget.magnitude;

                // Angle Check: is the player within the launcher's FOV?
                toTarget.y = 0;
                float angle = Vector3.Angle(initialForward, toTarget);

                if (angle <= config.SearchAngleRange)
                {
                    // Target the closest valid player
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestTarget = col.transform;
                    }
                }
            }

            return bestTarget;
        }
    }
}
