using System.Threading;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.States
{
    /// <summary>
    /// Firing state.
    /// Spawns projectiles at a fixed interval while maintaining aim.
    /// </summary>
    [CreateAssetMenu(fileName = "LauncherFireState", menuName = "StateMachine/States/Launcher/Fire")]
    public class LauncherFireStateSO : StateBaseSO
    {
        private const string LAST_FIRE_TIME_KEY = "_lastFireTime";

        public override void OnEnter(Blackboard bb, CancellationToken ct)
        {
            // Reset firing cooldown when entering the state
            bb.Set(LAST_FIRE_TIME_KEY, Time.time);
        }

        public override void OnServerUpdate(Blackboard bb)
        {
            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;

            // TRACKING: Continue updating target status while firing
            Transform target = FindBestTarget(owner, config, bb);
            bb.Set("Target", target);

            if (target == null) return;

            // AIM ASSIST: Continue pointing at target to track movement
            Vector3 toTarget = (target.position - owner.transform.position);
            toTarget.y = 0;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, Quaternion.LookRotation(toTarget), config.RotationSpeed * Time.deltaTime);
            }

            // FIRING LOGIC
            float lastFireTime = bb.Get<float>(LAST_FIRE_TIME_KEY);
            if (Time.time >= lastFireTime + (1f / config.FireRate))
            {
                Fire(owner, config);
                bb.Set(LAST_FIRE_TIME_KEY, Time.time);
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

        /// <summary>
        /// Instantiates and initializes a projectile.
        /// </summary>
        private void Fire(GameObject owner, LauncherTrapSO config)
        {
            if (config.BulletPrefab == null) return;

            // Spawn slightly in front of the launcher
            GameObject bullet = Instantiate(config.BulletPrefab, owner.transform.position + owner.transform.forward * 1.5f, owner.transform.rotation);

            // Give it initial forward momentum
            if (bullet.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(owner.transform.forward * 20f, ForceMode.Impulse);
            }

            Debug.Log("[Launcher] Server spawned projectile!");
        }
    }
}
