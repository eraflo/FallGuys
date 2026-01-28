using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.States
{
    /// <summary>
    /// Active Tracking state.
    /// Points the launcher directly at the detected target.
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherOrientState", menuName = "StateMachine/States/Launcher/Orient")]
    public class LauncherOrientStateSO : StateBaseSO
    {
        public override void OnServerUpdate(Blackboard bb)
        {
            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            if (baseObj == null) return;

            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;
            if (config == null) return;

            // CONTINUOUS SCANNING: Re-verify/Update target every frame.
            // If the target moves out of angle/range or a closer target appears, blackboard "Target" will update.
            Transform target = FindBestTarget(owner, config, bb);
            bb.Set("Target", target);

            // If target lost, transitions will handle exit to Search.
            if (target == null) return;

            // AUTHORITATIVE ROTATION: Points at the target in world space.
            Vector3 toTarget = (target.position - owner.transform.position);
            toTarget.y = 0; // Only horizontal rotation

            if (toTarget.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget);

                // Rotates gradually based on config speed
                owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, targetRot, config.RotationSpeed * Time.deltaTime);
            }
        }

        private Transform FindBestTarget(GameObject owner, LauncherTrapSO config, Blackboard bb)
        {
            // Find all colliders within detection range
            Collider[] colliders = Physics.OverlapSphere(owner.transform.position, config.DetectionRange, config.ImpactLayer);
            Transform bestTarget = null;
            float minDistance = float.MaxValue;

            if (!bb.Has("_initialRotation"))
            {
                bb.Set("_initialRotation", owner.transform.rotation);
            }
            Quaternion initialRot = bb.Get<Quaternion>("_initialRotation");
            Vector3 initialForward = initialRot * Vector3.forward;

            // Iterate through all colliders to find the best target
            foreach (var col in colliders)
            {
                Rigidbody rb = col.attachedRigidbody;
                if (rb == null) continue;

                // Calculate distance and angle to target
                Vector3 toTarget = col.transform.position - owner.transform.position;
                float dist = toTarget.magnitude;

                // Only consider targets within angle range
                toTarget.y = 0;
                float angle = Vector3.Angle(initialForward, toTarget);

                if (angle <= config.SearchAngleRange)
                {
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
