using System.Threading;
using Eraflo.Common.ObjectSystem;
using FallGuys.StateMachine;
using UnityEngine;

namespace FallGuys.Traps.Launcher.States
{
    [CreateAssetMenu(fileName = "LauncherFireState", menuName = "StateMachine/States/Launcher/Fire")]
    public class LauncherFireStateSO : StateBaseSO
    {
        private const string LAST_FIRE_TIME_KEY = "_lastFireTime";

        public override void OnEnter(Blackboard bb, CancellationToken ct)
        {
            bb.Set(LAST_FIRE_TIME_KEY, Time.time);
        }

        public override void OnServerUpdate(Blackboard bb)
        {
            Transform target = bb.Get<Transform>("_currentTarget");
            if (target == null) return;

            GameObject owner = bb.GetOwnerObject();
            BaseObject baseObj = owner.GetComponent<BaseObject>();
            LauncherTrapSO config = baseObj.RuntimeData.Config as LauncherTrapSO;
            
            // Still track target while firing (authoritative)
            Vector3 toTarget = (target.position - owner.transform.position);
            toTarget.y = 0;
            if (toTarget.sqrMagnitude > 0.01f)
            {
                owner.transform.rotation = Quaternion.RotateTowards(owner.transform.rotation, Quaternion.LookRotation(toTarget), config.RotationSpeed * Time.deltaTime);
            }

            float lastFireTime = bb.Get<float>(LAST_FIRE_TIME_KEY);
            if (Time.time >= lastFireTime + (1f / config.FireRate))
            {
                Fire(owner, config);
                bb.Set(LAST_FIRE_TIME_KEY, Time.time);
            }
        }

        private void Fire(GameObject owner, LauncherTrapSO config)
        {
            if (config.BulletPrefab == null) return;

            GameObject bullet = Instantiate(config.BulletPrefab, owner.transform.position + owner.transform.forward * 1.5f, owner.transform.rotation);
            
            if (bullet.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddForce(owner.transform.forward * 20f, ForceMode.Impulse);
            }
            
            Debug.Log("[Launcher] Fire!");
        }
    }
}
